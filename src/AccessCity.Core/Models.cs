using System.Globalization;

namespace AccessCity.Core;

public sealed record FeatureDefinition(string Key, string Name, string Kind, string Unit = "");

public static class Features
{
    public static readonly FeatureDefinition[] All = [
        new("stepFreeEntrance", "Wejście bez schodów", "boolean"),
        new("ramp", "Podjazd", "boolean"),
        new("doorWidthCm", "Szerokość wejścia", "number", "cm"),
        new("thresholdCm", "Wysokość progu", "number", "cm"),
        new("elevator", "Winda w obiekcie", "boolean"),
        new("elevatorOperational", "Działająca winda", "boolean"),
        new("accessibleToilet", "Toaleta dostępna", "boolean"),
        new("surface", "Nawierzchnia przy wejściu", "surface"),
        new("restArea", "Miejsce odpoczynku", "boolean"),
        new("wheelchairAccess", "Ogólne oznaczenie OSM: wheelchair", "wheelchair")
    ];
    public static readonly string[] Surfaces = ["asphalt", "paving_stones", "cobblestone", "gravel", "ground", "concrete"];
    public static string? Validate(string? key, string? value)
    {
        var feature = All.FirstOrDefault(x => x.Key == key);
        if (feature is null) return "Nieznana cecha dostępności.";
        if (string.IsNullOrWhiteSpace(value)) return "Podaj wartość cechy.";
        if (feature.Kind == "boolean" && value is not ("true" or "false")) return "Wartość musi być true albo false.";
        if (feature.Kind == "surface" && !Surfaces.Contains(value)) return "Nieznana nawierzchnia.";
        if (feature.Kind == "wheelchair" && value is not ("yes" or "no" or "limited")) return "Wartość musi być yes, no albo limited.";
        if (feature.Kind == "number" && (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) || n < 0 || n > 500))
            return "Pomiar musi być liczbą od 0 do 500 cm (kropka jako separator).";
        return null;
    }
    public static string Normalize(string key, string value) => All.Single(x => x.Key == key).Kind == "number"
        ? decimal.Parse(value, CultureInfo.InvariantCulture).ToString("0.##", CultureInfo.InvariantCulture) : value;
    public static string Display(string key, string? value)
    {
        if (value is null) return "Brak danych";
        if (value == "true") return "Tak";
        if (value == "false") return "Nie";
        var feature = All.Single(x => x.Key == key);
        if (feature.Kind == "number") return value + " " + feature.Unit;
        return value switch {
            "asphalt" => "Asfalt", "paving_stones" => "Płyty / kostka betonowa", "cobblestone" => "Bruk kamienny",
            "gravel" => "Żwir", "ground" => "Grunt", "concrete" => "Beton", "yes" => "Tak (oznaczenie ogólne)",
            "no" => "Nie (oznaczenie ogólne)", "limited" => "Ograniczona dostępność", _ => value
        };
    }
}

public sealed record Place(string Id, string Name, string Category, string Address, string City,
    double Latitude, double Longitude, bool IsDemo, string? ExternalUrl = null);
public sealed record Observation(string Id, string PlaceId, string Feature, string Value, string Source,
    string? SourceUrl, DateTimeOffset? UpdatedAt, DateTimeOffset RetrievedAt, bool IsDemo,
    string? RawKey = null, string? RawValue = null);
public sealed record CommunityReport(string Id, string PlaceId, string Feature, string Value, string Comment,
    string AuthorId, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int Confirmations);
public sealed record PublicReport(string Id, string Feature, string FeatureName, string Value, string DisplayValue,
    string Comment, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt, int Confirmations,
    string Status, bool IsExpired, bool IsMine, bool HasConfirmed);
public sealed record Evidence(string Kind, string Source, string? Url, string Value, string DisplayValue,
    DateTimeOffset? UpdatedAt, DateTimeOffset RetrievedAt, string Status, bool IsDemo, string? ReportId = null,
    int Confirmations = 0, DateTimeOffset? ExpiresAt = null);
public sealed record FeatureResult(string Key, string Name, string? Value, string DisplayValue, string Status,
    bool IsStale, bool HasConflict, string Explanation, IReadOnlyList<Evidence> Evidence);
public sealed record Preferences(bool StepFree = false, bool Toilet = false, bool Elevator = false,
    bool AvoidCobblestone = false, decimal? MinDoorWidthCm = null, decimal? MaxThresholdCm = null);
public sealed record RequirementResult(string Feature, string Label, string Status, string Detail);
public sealed record MatchResult(int Met, int NotMet, int Uncertain, IReadOnlyList<RequirementResult> Requirements);
public sealed record AccessibilityResult(Place Place, IReadOnlyList<FeatureResult> Features, MatchResult Match,
    DateTimeOffset GeneratedAt, string Disclaimer);
public sealed record ImportResult(string Provider, int Places, int Observations, DateTimeOffset ImportedAt, string Message);
public sealed record ProviderBatch(IReadOnlyList<Place> Places, IReadOnlyList<Observation> Observations);

public sealed class CityArea
{
    public string Name { get; set; } = "Kraków";
    public double South { get; set; } = 50.04;
    public double West { get; set; } = 19.91;
    public double North { get; set; } = 50.08;
    public double East { get; set; } = 19.97;
}
public interface IAccessibilityDataProvider
{
    string Name { get; }
    Task<ProviderBatch> FetchAsync(CityArea area, CancellationToken ct);
}

public interface IAccessibilityRepository
{
    Task InitializeAsync(bool seedDemo, CancellationToken ct = default);
    Task<IReadOnlyList<Place>> SearchAsync(string? search, string? category, string? city, bool? demo, CancellationToken ct);
    Task<Place?> FindAsync(string id, CancellationToken ct);
    Task<IReadOnlyList<Observation>> ObservationsAsync(string placeId, CancellationToken ct);
    Task<IReadOnlyList<CommunityReport>> ReportsAsync(string placeId, CancellationToken ct);
    Task<IReadOnlyList<PublicReport>> PublicReportsAsync(string placeId, string authorId, DateTimeOffset now, CancellationToken ct);
    Task AddReportAsync(CommunityReport report, CancellationToken ct);
    Task<string> ConfirmAsync(string reportId, string actorId, DateTimeOffset now, CancellationToken ct);
    Task ImportAsync(ProviderBatch batch, string provider, DateTimeOffset importedAt, CancellationToken ct);
    Task<DateTimeOffset?> LastImportAsync(string provider, CancellationToken ct);
}
