using System.Text.Json;

namespace CarPrices.Enrichment.Vpic;

/// <summary>Parses an NHTSA vPIC DecodeVINValues(Batch) response into <see cref="VpicResult"/>s.</summary>
public static class VpicResponseParser
{
    public static IReadOnlyList<VpicResult> Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var results = new List<VpicResult>();
        if (!doc.RootElement.TryGetProperty("Results", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return results;

        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in item.EnumerateObject())
            {
                fields[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.ToString(),
                    JsonValueKind.Null => null,
                    _ => prop.Value.ToString(),
                };
            }
            results.Add(new VpicResult(fields));
        }
        return results;
    }
}
