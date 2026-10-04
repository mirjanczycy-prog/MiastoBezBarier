using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AccessCity.Core;
using AccessCity.DataProviders;
using AccessCity.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<AccessibilityEngine>();
var dataDirectory = Path.GetFullPath(builder.Configuration["Storage:Directory"] ?? "App_Data", builder.Environment.ContentRootPath);
Directory.CreateDirectory(dataDirectory);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys"))).SetApplicationName("AccessCity");
builder.Services.AddSingleton<IAccessibilityRepository>(sp => new SqliteRepository(Path.Combine(dataDirectory, "accesscity.db"), sp.GetRequiredService<TimeProvider>()));
builder.Services.AddSingleton<ImportLock>();
builder.Services.AddAntiforgery(o => { o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "accesscity.csrf"; });
builder.Services.AddHttpClient<OpenStreetMapProvider>(http => {
    http.BaseAddress = new Uri(builder.Configuration["OpenStreetMap:Endpoint"] ?? "https://overpass-api.de/api/interpreter");
    http.Timeout = TimeSpan.FromSeconds(40);
    http.DefaultRequestHeaders.UserAgent.ParseAdd("AccessCity-Hackathon/1.0");
});
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("writes", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
    o.AddPolicy("imports", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions {
            PermitLimit = 2, Window = TimeSpan.FromMinutes(10), QueueLimit = 0
        }));
});
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16_384);
var app = builder.Build();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.Use(async (context, next) => {
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    var frameAncestors = context.Request.Path == "/widget.html" ? "*" : "'self'";
    context.Response.Headers["Content-Security-Policy"] = $"default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https://tile.openstreetmap.org; connect-src 'self'; frame-ancestors {frameAncestors}; object-src 'none'; base-uri 'self'; form-action 'self'";
    if (context.Request.Path.StartsWithSegments("/api")) {
        context.Response.Headers.CacheControl = "no-store";
        var protector = context.RequestServices.GetRequiredService<IDataProtectionProvider>().CreateProtector("anonymous-session.v1").ToTimeLimitedDataProtector();
        string? actor = null;
        if (context.Request.Cookies.TryGetValue("accesscity.session", out var cookie)) {
            try { actor = protector.Unprotect(cookie, out _); } catch (CryptographicException) { /* Invalid/expired cookies are rotated. */ }
        }
        if (actor is null) {
            actor = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            context.Response.Cookies.Append("accesscity.session", protector.Protect(actor, TimeSpan.FromDays(30)), new CookieOptions {
                HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(30), Path = "/"
            });
        }
        context.Items["actor"] = actor;
        if (HttpMethods.IsPost(context.Request.Method)) {
            try { await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context); }
            catch (AntiforgeryValidationException) {
                await Results.Problem("Odśwież stronę: token zabezpieczający wygasł lub go brakuje.", statusCode: 400).ExecuteAsync(context); return;
            }
        }
    }
    await next(context);
});
app.UseDefaultFiles(); app.UseStaticFiles(); app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/session", (HttpContext context, IAntiforgery antiforgery) => Results.Ok(new {
    csrfToken = antiforgery.GetAndStoreTokens(context).RequestToken,
    confirmationPolicy = "Dwie odrębne sesje poza autorem; anonimowe sesje nie potwierdzają tożsamości.",
    canImport = app.Environment.IsDevelopment()
}));
app.MapGet("/api/features", () => Features.All);
app.MapGet("/api/places", async (string? q, string? category, string? city, bool? demo, IAccessibilityRepository repo, CancellationToken ct) => {
    if (q?.Length > 200 || category?.Length > 60 || city?.Length > 100) return Results.BadRequest(new { detail = "Zbyt długi filtr." });
    return Results.Ok(await repo.SearchAsync(q, category, city, demo, ct));
});
app.MapGet("/api/places/{id}", async (string id, IAccessibilityRepository repo, CancellationToken ct) =>
    await repo.FindAsync(id, ct) is { } p ? Results.Ok(p) : Results.NotFound());
app.MapGet("/api/search", async (string? q, string? category, bool? demo, bool? stepFree, bool? toilet,
    bool? elevator, bool? avoidCobblestone, decimal? minDoorWidthCm, decimal? maxThresholdCm, bool? hideKnownBarriers,
    IAccessibilityRepository repo, AccessibilityEngine engine, CancellationToken ct) => {
    if (q?.Length > 200 || minDoorWidthCm is < 0 or > 500 || maxThresholdCm is < 0 or > 500)
        return Results.BadRequest(new { detail = "Niepoprawne kryteria wyszukiwania." });
    var results = new List<object>();
    foreach (var place in await repo.SearchAsync(q, category, null, demo, ct)) {
        var result = engine.Evaluate(place, await repo.ObservationsAsync(place.Id, ct), await repo.ReportsAsync(place.Id, ct),
            new(stepFree ?? false, toilet ?? false, elevator ?? false, avoidCobblestone ?? false, minDoorWidthCm, maxThresholdCm));
        if (hideKnownBarriers == true && result.Match.NotMet > 0) continue;
        results.Add(new { place, result.Match, knownFeatures = result.Features.Count(x => x.Evidence.Count > 0),
            conflicts = result.Features.Count(x => x.HasConflict), staleFeatures = result.Features.Count(x => x.IsStale) });
    }
    return Results.Ok(results);
});
app.MapGet("/api/places/{id}/accessibility", async (string id, bool? stepFree, bool? toilet, bool? elevator,
    bool? avoidCobblestone, decimal? minDoorWidthCm, decimal? maxThresholdCm,
    IAccessibilityRepository repo, AccessibilityEngine engine, CancellationToken ct) => {
    if (minDoorWidthCm is < 0 or > 500 || maxThresholdCm is < 0 or > 500) return Results.BadRequest(new { detail = "Pomiar musi być od 0 do 500 cm." });
    var place = await repo.FindAsync(id, ct); if (place is null) return Results.NotFound();
    return Results.Ok(engine.Evaluate(place, await repo.ObservationsAsync(id, ct), await repo.ReportsAsync(id, ct),
        new(stepFree ?? false, toilet ?? false, elevator ?? false, avoidCobblestone ?? false, minDoorWidthCm, maxThresholdCm)));
});
app.MapGet("/api/places/{id}/reports", async (string id, HttpContext context, IAccessibilityRepository repo, TimeProvider clock, CancellationToken ct) => {
    if (await repo.FindAsync(id, ct) is null) return Results.NotFound();
    return Results.Ok(await repo.PublicReportsAsync(id, (string)context.Items["actor"]!, clock.GetUtcNow(), ct));
});
app.MapPost("/api/places/{id}/reports", async (string id, ReportRequest request, HttpContext context,
    IAccessibilityRepository repo, TimeProvider clock, CancellationToken ct) => {
    if (Features.Validate(request.Feature, request.Value) is { } error) return Results.BadRequest(new { detail = error });
    if (string.IsNullOrWhiteSpace(request.Comment) || request.Comment.Length > 500) return Results.BadRequest(new { detail = "Opis powinien mieć od 1 do 500 znaków." });
    if (await repo.FindAsync(id, ct) is null) return Results.NotFound();
    var now = clock.GetUtcNow(); var ttl = request.Feature == "elevatorOperational" ? TimeSpan.FromHours(48) : TimeSpan.FromDays(30);
    var report = new CommunityReport(Guid.NewGuid().ToString("N"), id, request.Feature!, Features.Normalize(request.Feature!, request.Value!),
        request.Comment.Trim(), (string)context.Items["actor"]!, now, now + ttl, 0);
    await repo.AddReportAsync(report, ct);
    return Results.Created($"/api/places/{id}/reports", new { report.Id, report.CreatedAt, report.ExpiresAt, status = "unverified" });
}).RequireRateLimiting("writes");
app.MapPost("/api/reports/{id}/confirm", async (string id, HttpContext context, IAccessibilityRepository repo, TimeProvider clock, CancellationToken ct) => {
    var status = await repo.ConfirmAsync(id, (string)context.Items["actor"]!, clock.GetUtcNow(), ct);
    return status switch {
        "confirmed" => Results.Ok(new { status }), "not-found" => Results.NotFound(),
        "self-confirmation" => Results.Conflict(new { detail = "Autor nie może potwierdzić własnego zgłoszenia." }),
        "expired" => Results.Conflict(new { detail = "Zgłoszenie wygasło; dodaj nową obserwację." }),
        _ => Results.Conflict(new { detail = "Ta sesja już potwierdziła zgłoszenie." })
    };
}).RequireRateLimiting("writes");
app.MapGet("/api/providers/status", async (IAccessibilityRepository repo, CancellationToken ct) => Results.Ok(new {
    provider = "OpenStreetMap", lastSuccessfulImport = await repo.LastImportAsync("OpenStreetMap", ct),
    refreshPolicy = "Import ręczny; minimum 10 minut odstępu. Brak źródła nie usuwa zapisanych danych.",
    license = "ODbL 1.0", attributionUrl = "https://www.openstreetmap.org/copyright",
    area = builder.Configuration.GetSection("City").Get<CityArea>() ?? new()
}));
app.MapPost("/api/admin/import/osm", async (HttpContext context, OpenStreetMapProvider provider,
    IAccessibilityRepository repo, ImportLock gate, TimeProvider clock, ILogger<Program> logger, CancellationToken ct) => {
    if (!app.Environment.IsDevelopment()) {
        var expected = builder.Configuration["Admin:ApiKey"];
        var supplied = context.Request.Headers["X-Admin-Key"].ToString();
        if (string.IsNullOrEmpty(expected) || supplied.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(supplied), System.Text.Encoding.UTF8.GetBytes(expected)))
            return Results.Unauthorized();
    }
    if (!await gate.Value.WaitAsync(0, ct)) return Results.Conflict(new { detail = "Import już trwa." });
    try {
        var now = clock.GetUtcNow(); var last = await repo.LastImportAsync(provider.Name, ct);
        if (last is not null && now - last.Value < TimeSpan.FromMinutes(10))
            return Results.Conflict(new { detail = "Poczekaj 10 minut między importami." });
        var area = builder.Configuration.GetSection("City").Get<CityArea>() ?? new();
        if (!(area.South < area.North && area.West < area.East) || area.North - area.South > .15 || area.East - area.West > .2)
            return Results.BadRequest(new { detail = "Ustaw poprawny mały obszar miasta w konfiguracji." });
        var batch = await provider.FetchAsync(area, ct);
        if (batch.Places.Count == 0) return Results.Problem("Brak miejsc w odpowiedzi źródła. Dane zapisane wcześniej zachowano.", statusCode: 503);
        await repo.ImportAsync(batch, provider.Name, now, ct);
        return Results.Ok(new ImportResult(provider.Name, batch.Places.Count, batch.Observations.Count, now,
            "Zaimportowano aktualny wycinek (maksymalnie 300 miejsc). Brak tagu oznacza brak danych, nie brak bariery."));
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidDataException) {
        logger.LogWarning(ex, "Import OSM nie powiódł się.");
        return Results.Problem("Źródło OSM jest niedostępne albo zwróciło niepełną odpowiedź. Zapisane dane pozostają dostępne.", statusCode: 503);
    }
    finally { gate.Value.Release(); }
}).RequireRateLimiting("imports");

await app.Services.GetRequiredService<IAccessibilityRepository>().InitializeAsync(builder.Configuration.GetValue("Demo:Seed", true));
app.Run();

public sealed record ReportRequest(string? Feature, string? Value, string? Comment);
public sealed class ImportLock { public SemaphoreSlim Value { get; } = new(1, 1); }
