using System.Globalization;

namespace AccessCity.Core;

// An observation is evidence, never an official assurance of accessibility.
public sealed class AccessibilityEngine(TimeProvider clock)
{
    public AccessibilityResult Evaluate(Place place, IReadOnlyList<Observation> observations,
        IReadOnlyList<CommunityReport> reports, Preferences preferences)
    {
        var now = clock.GetUtcNow();
        var features = Features.All.Select(definition => Resolve(definition, observations, reports, now)).ToArray();
        return new(place, features, Match(features, preferences), now,
            "Informacje pomagają ocenić miejsce. Zgłoszenia oraz oznaczenia OSM nie są formalnym potwierdzeniem dostępności.");
    }

    private static FeatureResult Resolve(FeatureDefinition feature, IReadOnlyList<Observation> observations,
        IReadOnlyList<CommunityReport> reports, DateTimeOffset now)
    {
        var baseline = observations.Where(x => x.Feature == feature.Key).OrderByDescending(x => x.RetrievedAt).ToArray();
        var active = reports.Where(x => x.Feature == feature.Key && x.ExpiresAt > now).OrderByDescending(x => x.CreatedAt).ToArray();
        var evidence = baseline.Select(x => new Evidence("observation", x.Source, x.SourceUrl, x.Value,
            Features.Display(feature.Key, x.Value), x.UpdatedAt, x.RetrievedAt,
            x.IsDemo ? "demo" : "source-unverified", x.IsDemo)).Concat(active.Select(x => new Evidence(
            "community", "Zgłoszenie społeczności", null, x.Value, Features.Display(feature.Key, x.Value),
            x.CreatedAt, x.CreatedAt, x.Confirmations >= 2 ? "community-confirmed" : "unverified", false,
            x.Id, x.Confirmations, x.ExpiresAt))).ToArray();
        if (evidence.Length == 0) return new(feature.Key, feature.Name, null, "Brak danych", "unknown", false, false,
            "Brak informacji nie oznacza braku bariery.", evidence);

        // A single report can create a conflict; a majority never silently erases contrary evidence.
        var values = evidence.Select(x => x.Value).Distinct().ToArray();
        var conflict = values.Length > 1;
        var stale = baseline.Length > 0 && active.Length == 0 &&
            baseline.All(x => x.UpdatedAt is null || now - x.UpdatedAt.Value > TimeSpan.FromDays(180));
        var status = conflict ? "conflict" : active.Length > 0
            ? active.Any(x => x.Confirmations >= 2) ? "community-confirmed" : "unverified"
            : stale ? "stale" : baseline.Any(x => x.IsDemo) ? "demo" : "source-unverified";
        var value = conflict ? null : values[0];
        var explanation = conflict ? "Źródła podają różne wartości. Sprawdź dowody przed wizytą."
            : active.Length > 0 ? "Zgłoszenie anonimowe; potwierdzenia liczą odrębne sesje przeglądarki, nie zweryfikowane osoby."
            : stale ? "Brak aktualnej daty lub edycja źródła starsza niż 180 dni. Data pobrania nie potwierdza stanu obiektu."
            : "Dane ze źródła; data edycji rekordu nie jest datą oględzin obiektu.";
        return new(feature.Key, feature.Name, value, conflict ? "Sprzeczne informacje" : Features.Display(feature.Key, value),
            status, stale, conflict, explanation, evidence);
    }

    private static MatchResult Match(IReadOnlyList<FeatureResult> features, Preferences p)
    {
        var results = new List<RequirementResult>();
        void Add(string key, string label, Func<string, bool> check)
        {
            var f = features.Single(x => x.Key == key);
            // "met" means the current evidence indicates a match, never a formal assurance.
            var state = f.Value is null || f.IsStale ? "uncertain" : !check(f.Value) ? "not-met"
                : f.Status == "unverified" ? "uncertain" : "met";
            results.Add(new(key, label, state, f.DisplayValue));
        }
        if (p.StepFree) Add("stepFreeEntrance", "Wejście bez schodów", x => x == "true");
        if (p.Toilet) Add("accessibleToilet", "Toaleta dostępna", x => x == "true");
        if (p.Elevator) { Add("elevator", "Winda w obiekcie", x => x == "true"); Add("elevatorOperational", "Działająca winda", x => x == "true"); }
        if (p.AvoidCobblestone) Add("surface", "Unikam bruku", x => x != "cobblestone");
        if (p.MinDoorWidthCm is not null) Add("doorWidthCm", $"Wejście ≥ {p.MinDoorWidthCm} cm", x => decimal.Parse(x, CultureInfo.InvariantCulture) >= p.MinDoorWidthCm);
        if (p.MaxThresholdCm is not null) Add("thresholdCm", $"Próg ≤ {p.MaxThresholdCm} cm", x => decimal.Parse(x, CultureInfo.InvariantCulture) <= p.MaxThresholdCm);
        return new(results.Count(x => x.Status == "met"), results.Count(x => x.Status == "not-met"), results.Count(x => x.Status == "uncertain"), results);
    }
}
