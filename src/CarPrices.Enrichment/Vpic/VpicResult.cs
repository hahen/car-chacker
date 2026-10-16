namespace CarPrices.Enrichment.Vpic;

/// <summary>One decoded VIN from NHTSA vPIC: the flat field map plus typed accessors.</summary>
public class VpicResult(IReadOnlyDictionary<string, string?> fields)
{
    public IReadOnlyDictionary<string, string?> Fields { get; } = fields;

    /// <summary>Field value, trimmed; blank becomes null.</summary>
    public string? Get(string key) =>
        Fields.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v.Trim() : null;

    /// <summary>True when a (usually safety) field is present/affirmative rather than blank or "No".</summary>
    public bool IsPresent(string key)
    {
        var v = Get(key);
        if (string.IsNullOrWhiteSpace(v)) return false;
        var u = v.ToUpperInvariant();
        return u is not ("NO" or "NOT APPLICABLE" or "NOT AVAILABLE" or "NONE" or "0");
    }

    public string? Vin => Get("VIN");
    public string? Make => Get("Make");
    public string? Model => Get("Model");
    public string? Trim => Get("Trim") ?? Get("Series");
    public int? ModelYear => int.TryParse(Get("ModelYear"), out var y) ? y : null;
    public string? BodyClass => Get("BodyClass");
    public string? DriveType => Get("DriveType");
    public string? FuelType => Get("FuelTypePrimary");
    public string? EngineModel => Get("EngineModel");
    public string? EngineCylindersRaw => Get("EngineCylinders");
    public int? EngineCylinders => int.TryParse(Get("EngineCylinders"), out var c) ? c : null;
    public string? DisplacementL => Get("DisplacementL");
    public string? PlantCountry => Get("PlantCountry");
    public string? ErrorCode => Get("ErrorCode");
    public bool DecodedOk => ErrorCode is null || ErrorCode.StartsWith('0');
}
