using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

static class ApiChecks
{
    public static async Task RunAsync(string baseUrl, string? unavailableSourceUrl)
    {
        var count = 0;
        void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException("API FAILED: " + label); Console.WriteLine("API PASS: " + label); count++; }
        using var a = new BrowserSession(baseUrl); using var b = new BrowserSession(baseUrl); using var c = new BrowserSession(baseUrl);
        await a.Initialize(); await b.Initialize(); await c.Initialize();
        var places = await a.Client.GetFromJsonAsync<JsonElement>("/api/places?q=HOTEL&demo=true");
        Check(places.GetArrayLength() == 1, "Case-insensitive search");
        using var noToken = await a.Client.PostAsJsonAsync("/api/places/demo-hotel/reports", new { feature = "elevatorOperational", value = "false", comment = "Test" });
        Check(noToken.StatusCode == HttpStatusCode.BadRequest, "Writes require CSRF token");
        using var invalid = await a.Post("/api/places/demo-hotel/reports", new { feature = "doorWidthCm", value = "-1", comment = "Test" });
        Check(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid dimensions are rejected at HTTP boundary");
        using var created = await a.Post("/api/places/demo-hotel/reports", new { feature = "elevatorOperational", value = "false", comment = "Automatyczny test integracyjny: winda nie działa." });
        Check(created.StatusCode == HttpStatusCode.Created, "Create a report");
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();
        using var self = await a.Post($"/api/reports/{id}/confirm");
        Check(self.StatusCode == HttpStatusCode.Conflict, "Author cannot self-confirm over HTTP");
        using var first = await b.Post($"/api/reports/{id}/confirm");
        Check(first.StatusCode == HttpStatusCode.OK, "A second browser session confirms");
        using var duplicate = await b.Post($"/api/reports/{id}/confirm");
        Check(duplicate.StatusCode == HttpStatusCode.Conflict, "A duplicate browser confirmation is rejected");
        using var second = await c.Post($"/api/reports/{id}/confirm");
        Check(second.StatusCode == HttpStatusCode.OK, "A third browser session confirms");
        var reports = await b.Client.GetFromJsonAsync<JsonElement>("/api/places/demo-hotel/reports");
        var report = reports.EnumerateArray().Single(x => x.GetProperty("id").GetString() == id);
        Check(report.GetProperty("confirmations").GetInt32() == 2 && report.GetProperty("status").GetString() == "community-confirmed", "Public report returns two confirmations and community status");
        Check(!report.TryGetProperty("authorId", out _), "Public API never discloses session identifiers");
        var accessibility = await b.Client.GetFromJsonAsync<JsonElement>("/api/places/demo-hotel/accessibility?elevator=true");
        var lift = accessibility.GetProperty("features").EnumerateArray().Single(x => x.GetProperty("key").GetString() == "elevatorOperational");
        Check(lift.GetProperty("hasConflict").GetBoolean() && lift.GetProperty("value").ValueKind == JsonValueKind.Null, "API exposes conflict rather than asserting access");
        Check((await b.Client.GetAsync("/widget.html?place=demo-hotel")).IsSuccessStatusCode, "Widget is hosted by the same application");
        if (unavailableSourceUrl is not null)
        {
            using var offline = new BrowserSession(unavailableSourceUrl); await offline.Initialize();
            var before = await offline.Client.GetFromJsonAsync<JsonElement>("/api/places");
            using var import = await offline.Post("/api/admin/import/osm");
            Check(import.StatusCode == HttpStatusCode.ServiceUnavailable, "Unavailable OSM source returns 503");
            var after = await offline.Client.GetFromJsonAsync<JsonElement>("/api/places");
            Check(before.GetArrayLength() == after.GetArrayLength(), "Failed import preserves cached places");
        }
        Console.WriteLine($"All {count} HTTP integration checks passed.");
    }
    private sealed class BrowserSession : IDisposable
    {
        public HttpClient Client { get; }
        private string? token;
        public BrowserSession(string baseUrl) => Client = new(new HttpClientHandler { CookieContainer = new(), UseCookies = true }) { BaseAddress = new Uri(baseUrl) };
        public async Task Initialize() => token = (await Client.GetFromJsonAsync<JsonElement>("/api/session")).GetProperty("csrfToken").GetString();
        public async Task<HttpResponseMessage> Post(string url, object? body = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("X-CSRF-TOKEN", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await Client.SendAsync(request);
        }
        public void Dispose() => Client.Dispose();
    }
}
