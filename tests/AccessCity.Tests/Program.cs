using System.Text;
using AccessCity.Core;
using AccessCity.DataProviders;
using AccessCity.Infrastructure;

var now = new DateTimeOffset(2026, 10, 4, 8, 0, 0, TimeSpan.Zero);
var clock = new FixedClock(now);
var engine = new AccessibilityEngine(clock);
var demo = DemoData.Create(now);
var hotel = demo.Places.Single(x => x.Id == "demo-hotel");
var observations = demo.Observations.Where(x => x.PlaceId == hotel.Id).ToArray();
var checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException("FAILED: " + description);
    Console.WriteLine("PASS: " + description); checks++;
}
FeatureResult Feature(AccessibilityResult result, string key) => result.Features.Single(x => x.Key == key);

var baseline = engine.Evaluate(hotel, observations, [], new(StepFree: true, MinDoorWidthCm: 90));
Check(baseline.Match.Met == 2, "Preferences compare concrete dimensions and boolean features");
Check(Feature(baseline, "wheelchairAccess").Status == "unknown", "Missing data stays unknown");
var incomplete = engine.Evaluate(demo.Places.Single(x => x.Id == "demo-museum"), demo.Observations.Where(x => x.PlaceId == "demo-museum").ToArray(), [], new(Toilet: true));
Check(incomplete.Match.Uncertain == 1 && incomplete.Match.Met == 0, "Unknown toilet does not imply availability");
var old = engine.Evaluate(demo.Places.Single(x => x.Id == "demo-library"), demo.Observations.Where(x => x.PlaceId == "demo-library").ToArray(), [], new(StepFree: true));
Check(old.Match.Uncertain == 1 && Feature(old, "stepFreeEntrance").IsStale, "Fetching an old record does not renew its observation date");
CommunityReport report = new("report-1", hotel.Id, "elevatorOperational", "false", "Lift does not start", "author", now, now.AddHours(48), 2);
var conflicting = engine.Evaluate(hotel, observations, [report], new(Elevator: true));
Check(Feature(conflicting, "elevatorOperational").HasConflict, "Confirmed reports preserve conflicting baseline evidence");
Check(Feature(conflicting, "elevatorOperational").Value is null && conflicting.Match.Uncertain == 1, "Conflicts never produce positive certainty");
Check(Feature(conflicting, "elevator").Value == "true", "Lift existence remains separate from operational state");
var expired = engine.Evaluate(hotel, observations, [report with { ExpiresAt = now }], new());
Check(!Feature(expired, "elevatorOperational").HasConflict, "Expired report stops affecting current state");
var unverified = engine.Evaluate(hotel, [], [report with { Value = "true", Confirmations = 0 }], new(Elevator: true));
Check(unverified.Match.Uncertain == 2, "Unverified positive reports are not counted as a match");
Check(Features.Validate("doorWidthCm", "-1") is not null && Features.Validate("surface", "moon") is not null, "Invalid values are rejected");
Check(Features.Normalize("doorWidthCm", "90.00") == "90", "Equivalent numeric measurements use one canonical value");

var osmJson = Encoding.UTF8.GetBytes("""
    {"elements":[{"type":"node","id":123,"lat":50.06,"lon":19.94,"timestamp":"2026-10-01T10:00:00Z",
    "tags":{"name":"Test cafe","amenity":"cafe","wheelchair":"yes","surface":"asphalt","width":"99","toilets:wheelchair":"no"}}]}
    """);
var osm = OpenStreetMapProvider.Parse(osmJson, "Kraków", now);
Check(osm.Places.Count == 1 && osm.Observations.Count == 2, "OSM imports supported tags and provenance");
Check(!osm.Observations.Any(x => x.Feature is "stepFreeEntrance" or "surface" or "doorWidthCm"), "General OSM tags do not invent entrance measurements");
Check(osm.Observations.All(x => x.UpdatedAt < x.RetrievedAt && !x.IsDemo), "Source editing and retrieval dates are separate");
var rejected = false;
try { OpenStreetMapProvider.Parse(Encoding.UTF8.GetBytes("{\"remark\":\"timeout\",\"elements\":[]}"), "Kraków", now); }
catch (InvalidDataException) { rejected = true; }
Check(rejected, "Partial provider responses fail without committing a snapshot");

var directory = Path.Combine(Path.GetTempPath(), "accesscity-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
try
{
    var repo = new SqliteRepository(Path.Combine(directory, "test.db"), clock);
    await repo.InitializeAsync(true);
    await repo.InitializeAsync(true);
    Check((await repo.SearchAsync(null, null, null, null, default)).Count == 4, "Demo seeding is idempotent");
    await repo.AddReportAsync(report with { Confirmations = 0 }, default);
    Check(await repo.ConfirmAsync(report.Id, "author", now, default) == "self-confirmation", "Author cannot confirm their own report");
    var duplicateAttempts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Task.Run(() => repo.ConfirmAsync(report.Id, "actor-2", now, default))));
    Check(duplicateAttempts.Count(x => x == "confirmed") == 1, "Concurrent duplicate confirmations count only once");
    Check(await repo.ConfirmAsync(report.Id, "actor-3", now, default) == "confirmed", "A different session can confirm");
    var publicReport = (await repo.PublicReportsAsync(hotel.Id, "actor-2", now, default)).Single();
    Check(publicReport.Confirmations == 2 && publicReport.HasConfirmed && publicReport.Status == "community-confirmed", "Public reports show aggregate and own-session state");
    Check(await repo.ConfirmAsync(report.Id, "actor-4", now.AddDays(3), default) == "expired", "Expired observations cannot receive confirmations");
    await repo.ImportAsync(osm, "OpenStreetMap", now, default);
    await repo.ImportAsync(new(osm.Places, []), "OpenStreetMap", now.AddMinutes(11), default);
    Check((await repo.ObservationsAsync(osm.Places[0].Id, default)).Count == 0, "Removed OSM tags are cleared on refresh");
    Check((await repo.ReportsAsync(hotel.Id, default)).Count == 1, "Source imports preserve independent community reports");
    var reopened = new SqliteRepository(Path.Combine(directory, "test.db"), clock);
    Check((await reopened.ReportsAsync(hotel.Id, default)).Single().Confirmations == 2, "Reports and confirmations survive a new repository instance");
}
finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); Directory.Delete(directory, true); }
Console.WriteLine($"All {checks} checks passed.");
if (args.Length > 0) await ApiChecks.RunAsync(args[0], args.Length > 1 ? args[1] : null);

sealed class FixedClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
