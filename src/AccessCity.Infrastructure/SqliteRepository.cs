using AccessCity.Core;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace AccessCity.Infrastructure;

public sealed class SqliteRepository(string databasePath, TimeProvider clock) : IAccessibilityRepository
{
    private async Task<SqliteConnection> Open(CancellationToken ct)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = databasePath, ForeignKeys = true, DefaultTimeout = 10
        }.ToString());
        await connection.OpenAsync(ct);
        return connection;
    }
    private static SqliteCommand Command(SqliteConnection c, string sql, SqliteTransaction? tx = null,
        params (string Name, object? Value)[] parameters)
    {
        var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.Transaction = tx;
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }
    private static string Stamp(DateTimeOffset date) => date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    private static DateTimeOffset Date(string value) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    private static string? Text(SqliteDataReader r, int index) => r.IsDBNull(index) ? null : r.GetString(index);
    private static Place ReadPlace(SqliteDataReader r) => new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3),
        r.GetString(4), r.GetDouble(5), r.GetDouble(6), r.GetInt32(7) == 1, Text(r, 8));

    public async Task InitializeAsync(bool seedDemo, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(databasePath))!);
        await using var c = await Open(ct);
        using var cmd = Command(c, """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Places (
              Id TEXT PRIMARY KEY, Name TEXT NOT NULL, Category TEXT NOT NULL, Address TEXT NOT NULL, City TEXT NOT NULL,
              Latitude REAL NOT NULL, Longitude REAL NOT NULL, IsDemo INTEGER NOT NULL, ExternalUrl TEXT);
            CREATE TABLE IF NOT EXISTS Observations (
              Id TEXT PRIMARY KEY, PlaceId TEXT NOT NULL REFERENCES Places(Id), Feature TEXT NOT NULL, Value TEXT NOT NULL,
              Source TEXT NOT NULL, SourceUrl TEXT, UpdatedAt TEXT, RetrievedAt TEXT NOT NULL, IsDemo INTEGER NOT NULL,
              RawKey TEXT, RawValue TEXT);
            CREATE TABLE IF NOT EXISTS Reports (
              Id TEXT PRIMARY KEY, PlaceId TEXT NOT NULL REFERENCES Places(Id), Feature TEXT NOT NULL, Value TEXT NOT NULL,
              Comment TEXT NOT NULL, AuthorId TEXT NOT NULL, CreatedAt TEXT NOT NULL, ExpiresAt TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Confirmations (
              ReportId TEXT NOT NULL REFERENCES Reports(Id), ActorId TEXT NOT NULL, CreatedAt TEXT NOT NULL,
              PRIMARY KEY(ReportId, ActorId));
            CREATE TABLE IF NOT EXISTS Imports (Provider TEXT PRIMARY KEY, ImportedAt TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS IX_Observations_Place ON Observations(PlaceId);
            CREATE INDEX IF NOT EXISTS IX_Reports_Place ON Reports(PlaceId);
            PRAGMA user_version=1;
            """);
        await cmd.ExecuteNonQueryAsync(ct);
        if (seedDemo)
        {
            using var count = Command(c, "SELECT COUNT(*) FROM Places WHERE IsDemo=1");
            if (Convert.ToInt32(await count.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture) == 0)
                await ImportAsync(DemoData.Create(clock.GetUtcNow()), "DEMO", clock.GetUtcNow(), ct);
        }
    }
    public async Task<IReadOnlyList<Place>> SearchAsync(string? search, string? category, string? city, bool? demo, CancellationToken ct)
    {
        await using var c = await Open(ct);
        // Search names in .NET for Polish case-insensitive matching; SQLite NOCASE is ASCII-only.
        using var cmd = Command(c, """
            SELECT * FROM Places WHERE ($category IS NULL OR Category=$category)
            AND ($city IS NULL OR City=$city) AND ($demo IS NULL OR IsDemo=$demo) ORDER BY Name
            """, null, ("$category", category), ("$city", city), ("$demo", demo is null ? null : demo.Value ? 1 : 0));
        var result = new List<Place>(); await using var r = await cmd.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct))
        {
            var p = ReadPlace(r);
            if (string.IsNullOrWhiteSpace(search) || (p.Name + " " + p.Address).Contains(search, StringComparison.OrdinalIgnoreCase)) result.Add(p);
            if (result.Count >= 300) break;
        }
        return result;
    }
    public async Task<Place?> FindAsync(string id, CancellationToken ct)
    {
        await using var c = await Open(ct); using var cmd = Command(c, "SELECT * FROM Places WHERE Id=$id", null, ("$id", id));
        await using var r = await cmd.ExecuteReaderAsync(ct); return await r.ReadAsync(ct) ? ReadPlace(r) : null;
    }
    public async Task<IReadOnlyList<Observation>> ObservationsAsync(string placeId, CancellationToken ct)
    {
        await using var c = await Open(ct); using var cmd = Command(c, "SELECT * FROM Observations WHERE PlaceId=$id", null, ("$id", placeId));
        await using var r = await cmd.ExecuteReaderAsync(ct); var result = new List<Observation>();
        while (await r.ReadAsync(ct)) result.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            Text(r, 5), Text(r, 6) is { } date ? Date(date) : null, Date(r.GetString(7)), r.GetInt32(8) == 1, Text(r, 9), Text(r, 10)));
        return result;
    }
    public async Task<IReadOnlyList<CommunityReport>> ReportsAsync(string placeId, CancellationToken ct)
    {
        await using var c = await Open(ct); using var cmd = Command(c, """
            SELECT r.*, (SELECT COUNT(*) FROM Confirmations c WHERE c.ReportId=r.Id) FROM Reports r
            WHERE PlaceId=$id ORDER BY CreatedAt DESC
            """, null, ("$id", placeId));
        await using var reader = await cmd.ExecuteReaderAsync(ct); var result = new List<CommunityReport>();
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3),
            reader.GetString(4), reader.GetString(5), Date(reader.GetString(6)), Date(reader.GetString(7)), reader.GetInt32(8)));
        return result;
    }
    public async Task<IReadOnlyList<PublicReport>> PublicReportsAsync(string placeId, string authorId, DateTimeOffset now, CancellationToken ct)
    {
        var reports = await ReportsAsync(placeId, ct);
        await using var c = await Open(ct); using var cmd = Command(c, "SELECT ReportId FROM Confirmations WHERE ActorId=$actor", null, ("$actor", authorId));
        await using var r = await cmd.ExecuteReaderAsync(ct); var confirmed = new HashSet<string>();
        while (await r.ReadAsync(ct)) confirmed.Add(r.GetString(0));
        return reports.Select(x => new PublicReport(x.Id, x.Feature, Features.All.Single(f => f.Key == x.Feature).Name,
            x.Value, Features.Display(x.Feature, x.Value), x.Comment, x.CreatedAt, x.ExpiresAt, x.Confirmations,
            x.ExpiresAt <= now ? "expired" : x.Confirmations >= 2 ? "community-confirmed" : "unverified",
            x.ExpiresAt <= now, x.AuthorId == authorId, confirmed.Contains(x.Id))).ToArray();
    }
    public async Task AddReportAsync(CommunityReport report, CancellationToken ct)
    {
        await using var c = await Open(ct); using var cmd = Command(c, """
            INSERT INTO Reports VALUES ($id,$place,$feature,$value,$comment,$author,$created,$expires)
            """, null, ("$id", report.Id), ("$place", report.PlaceId), ("$feature", report.Feature), ("$value", report.Value),
            ("$comment", report.Comment), ("$author", report.AuthorId), ("$created", Stamp(report.CreatedAt)), ("$expires", Stamp(report.ExpiresAt)));
        await cmd.ExecuteNonQueryAsync(ct);
    }
    public async Task<string> ConfirmAsync(string reportId, string actorId, DateTimeOffset now, CancellationToken ct)
    {
        await using var c = await Open(ct); using var tx = c.BeginTransaction();
        using var find = Command(c, "SELECT AuthorId,ExpiresAt FROM Reports WHERE Id=$id", tx, ("$id", reportId));
        string author; DateTimeOffset expires;
        await using (var r = await find.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return "not-found";
            author = r.GetString(0); expires = Date(r.GetString(1));
        }
        if (author == actorId) return "self-confirmation";
        if (expires <= now) return "expired";
        using var cmd = Command(c, "INSERT OR IGNORE INTO Confirmations VALUES ($id,$actor,$created)", tx,
            ("$id", reportId), ("$actor", actorId), ("$created", Stamp(now)));
        var changed = await cmd.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
        return changed == 1 ? "confirmed" : "already-confirmed";
    }
    public async Task ImportAsync(ProviderBatch batch, string provider, DateTimeOffset importedAt, CancellationToken ct)
    {
        await using var c = await Open(ct); using var tx = c.BeginTransaction();
        foreach (var p in batch.Places)
        {
            using var cmd = Command(c, """
                INSERT INTO Places VALUES ($id,$name,$category,$address,$city,$lat,$lon,$demo,$url)
                ON CONFLICT(Id) DO UPDATE SET Name=excluded.Name,Category=excluded.Category,Address=excluded.Address,
                City=excluded.City,Latitude=excluded.Latitude,Longitude=excluded.Longitude,ExternalUrl=excluded.ExternalUrl
                """, tx, ("$id", p.Id), ("$name", p.Name), ("$category", p.Category), ("$address", p.Address), ("$city", p.City),
                ("$lat", p.Latitude), ("$lon", p.Longitude), ("$demo", p.IsDemo ? 1 : 0), ("$url", p.ExternalUrl));
            await cmd.ExecuteNonQueryAsync(ct);
            // A successful refresh replaces the source snapshot, but preserves independent reports.
            using var clear = Command(c, "DELETE FROM Observations WHERE PlaceId=$id AND Source=$source", tx,
                ("$id", p.Id), ("$source", provider == "DEMO" ? "Scenariusz demonstracyjny" : provider));
            await clear.ExecuteNonQueryAsync(ct);
        }
        foreach (var o in batch.Observations)
        {
            using var cmd = Command(c, "INSERT INTO Observations VALUES ($id,$place,$feature,$value,$source,$url,$updated,$retrieved,$demo,$key,$raw)", tx,
                ("$id", o.Id), ("$place", o.PlaceId), ("$feature", o.Feature), ("$value", o.Value), ("$source", o.Source),
                ("$url", o.SourceUrl), ("$updated", o.UpdatedAt is null ? null : Stamp(o.UpdatedAt.Value)),
                ("$retrieved", Stamp(o.RetrievedAt)), ("$demo", o.IsDemo ? 1 : 0), ("$key", o.RawKey), ("$raw", o.RawValue));
            await cmd.ExecuteNonQueryAsync(ct);
        }
        using var import = Command(c, "INSERT INTO Imports VALUES ($provider,$at) ON CONFLICT(Provider) DO UPDATE SET ImportedAt=excluded.ImportedAt", tx,
            ("$provider", provider), ("$at", Stamp(importedAt)));
        await import.ExecuteNonQueryAsync(ct); await tx.CommitAsync(ct);
    }
    public async Task<DateTimeOffset?> LastImportAsync(string provider, CancellationToken ct)
    {
        await using var c = await Open(ct); using var cmd = Command(c, "SELECT ImportedAt FROM Imports WHERE Provider=$provider", null, ("$provider", provider));
        return await cmd.ExecuteScalarAsync(ct) is string value ? Date(value) : null;
    }
}
