using AccessCity.Core;

namespace AccessCity.Infrastructure;

public static class DemoData
{
    public static ProviderBatch Create(DateTimeOffset now)
    {
        Place[] places = [
            new("demo-hotel", "Hotel Przystań — DEMO", "hotel", "Okolice Plant (lokalizacja przykładowa)", "Kraków", 50.062, 19.933, true),
            new("demo-cafe", "Kawiarnia Pod Lipą — DEMO", "cafe", "Okolice Rynku (lokalizacja przykładowa)", "Kraków", 50.061, 19.938, true),
            new("demo-museum", "Galeria Otwarta — DEMO", "museum", "Okolice Wawelu (lokalizacja przykładowa)", "Kraków", 50.054, 19.936, true),
            new("demo-library", "Biblioteka Sąsiedzka — DEMO", "library", "Okolice Kleparza (lokalizacja przykładowa)", "Kraków", 50.068, 19.943, true)
        ];
        var data = new List<Observation>();
        void Add(string place, string key, string value, int age = 2) => data.Add(new($"{place}:{key}", place, key, value,
            "Scenariusz demonstracyjny", null, now.AddDays(-age), now, true));
        Add("demo-hotel", "stepFreeEntrance", "true"); Add("demo-hotel", "ramp", "true"); Add("demo-hotel", "doorWidthCm", "96");
        Add("demo-hotel", "thresholdCm", "2"); Add("demo-hotel", "elevator", "true"); Add("demo-hotel", "elevatorOperational", "true");
        Add("demo-hotel", "accessibleToilet", "true"); Add("demo-hotel", "surface", "paving_stones"); Add("demo-hotel", "restArea", "true");
        Add("demo-cafe", "stepFreeEntrance", "false"); Add("demo-cafe", "doorWidthCm", "78"); Add("demo-cafe", "thresholdCm", "8");
        Add("demo-cafe", "surface", "cobblestone"); Add("demo-cafe", "accessibleToilet", "false");
        Add("demo-museum", "stepFreeEntrance", "true"); Add("demo-museum", "elevator", "true"); Add("demo-museum", "restArea", "true");
        Add("demo-library", "stepFreeEntrance", "true", 300); Add("demo-library", "doorWidthCm", "90", 300);
        return new(places, data);
    }
}
