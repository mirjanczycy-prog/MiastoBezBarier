using System.Globalization;
using System.Text.Json;
using AccessCity.Core;

namespace AccessCity.DataProviders;

public sealed class OpenStreetMapProvider(HttpClient http, TimeProvider clock) : IAccessibilityDataProvider
{
    public string Name => "OpenStreetMap";
    public async Task<ProviderBatch> FetchAsync(CityArea area, CancellationToken ct)
    {
        var box = string.Join(",", new[] { area.South, area.West, area.North, area.East }.Select(x => x.ToString(CultureInfo.InvariantCulture)));
        var query = $"""
            [out:json][timeout:25];
            (nwr["name"]["amenity"~"^(restaurant|cafe|library|cinema|theatre|community_centre)$"]({box});
             nwr["name"]["tourism"~"^(museum|hotel|gallery)$"]({box}););
            out meta center;
            """;
        using var request = new HttpRequestMessage(HttpMethod.Post, "") {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["data"] = query })
        };
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        // Public provider responses are bounded before parsing to protect the demo host.
        if (response.Content.Headers.ContentLength > 8_000_000) throw new InvalidDataException("Zbyt duża odpowiedź OSM.");
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var memory = new MemoryStream(); var buffer = new byte[16_384]; int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            if (memory.Length + read > 8_000_000) throw new InvalidDataException("Zbyt duża odpowiedź OSM.");
            memory.Write(buffer, 0, read);
        }
        return Parse(memory.ToArray(), area.Name, clock.GetUtcNow());
    }

    public static ProviderBatch Parse(byte[] json, string city, DateTimeOffset retrievedAt)
    {
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("remark", out _)) throw new InvalidDataException("OSM zwróciło ostrzeżenie lub niekompletny wynik. Import przerwany.");
        var places = new List<Place>(); var observations = new List<Observation>();
        foreach (var e in doc.RootElement.GetProperty("elements").EnumerateArray().Take(300))
        {
            if (!e.TryGetProperty("tags", out var tags) || !tags.TryGetProperty("name", out var name)) continue;
            var point = e.TryGetProperty("center", out var center) ? center : e;
            if (!point.TryGetProperty("lat", out var lat) || !point.TryGetProperty("lon", out var lon)) continue;
            var type = e.GetProperty("type").GetString(); var osmId = e.GetProperty("id").GetInt64();
            var id = $"osm-{type}-{osmId}"; var url = $"https://www.openstreetmap.org/{type}/{osmId}";
            string? Tag(string key) => tags.TryGetProperty(key, out var v) ? v.GetString() : null;
            var category = Tag("tourism") ?? Tag("amenity") ?? "other";
            var address = string.Join(" ", new[] { Tag("addr:street"), Tag("addr:housenumber") }.Where(x => !string.IsNullOrWhiteSpace(x)));
            places.Add(new(id, name.GetString()!, category, address.Length == 0 ? "Brak adresu w źródle" : address, city,
                lat.GetDouble(), lon.GetDouble(), false, url));
            DateTimeOffset? updated = e.TryGetProperty("timestamp", out var stamp) && DateTimeOffset.TryParse(stamp.GetString(), out var d) ? d : null;
            void Add(string feature, string key, string value) => observations.Add(new($"{id}:{feature}", id, feature,
                Features.Normalize(feature, value), "OpenStreetMap", url, updated, retrievedAt, false, key, Tag(key)));
            void Boolean(string feature, string key)
            {
                if (Tag(key) is "yes") Add(feature, key, "true");
                if (Tag(key) is "no") Add(feature, key, "false");
            }
            Boolean("ramp", "ramp:wheelchair"); Boolean("elevator", "elevator"); Boolean("accessibleToilet", "toilets:wheelchair");
            if (Tag("wheelchair") is "yes" or "no" or "limited") Add("wheelchairAccess", "wheelchair", Tag("wheelchair")!);
            // wheelchair=yes does NOT prove absence of stairs, doorway width, or that a lift works.
            // width/surface of a building are NOT assumed to describe its entrance.
            if (Tag("entrance:step_count") is { } steps && int.TryParse(steps, out var count) && count >= 0)
                Add("stepFreeEntrance", "entrance:step_count", count == 0 ? "true" : "false");
            if (Tag("entrance:width") is { } width && decimal.TryParse(width, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var meters)
                && meters > 0 && meters <= 5)
                Add("doorWidthCm", "entrance:width", (meters * 100).ToString(CultureInfo.InvariantCulture));
            if (Tag("entrance:surface") is { } surface && Features.Surfaces.Contains(surface)) Add("surface", "entrance:surface", surface);
        }
        return new(places, observations);
    }
}
