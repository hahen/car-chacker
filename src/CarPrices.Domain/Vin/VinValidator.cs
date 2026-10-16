namespace CarPrices.Domain.Vin;

/// <summary>Pure VIN validation: charset, length, ISO-3779 check digit, and masked-pattern matching.</summary>
public static class VinValidator
{
    private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

    /// <summary>A full, valid 17-char VIN: correct charset (no I/O/Q) and a passing check digit.</summary>
    public static bool IsValidVin(string? vin)
    {
        if (string.IsNullOrWhiteSpace(vin)) return false;
        var v = vin.Trim().ToUpperInvariant();
        if (v.Length != 17) return false;
        foreach (var c in v)
        {
            if (c is 'I' or 'O' or 'Q') return false;
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
        }
        return CheckDigitValid(v);
    }

    public static bool CheckDigitValid(string vin)
    {
        if (vin.Length != 17) return false;
        var v = vin.ToUpperInvariant();
        var sum = 0;
        for (var i = 0; i < 17; i++)
        {
            var val = Transliterate(v[i]);
            if (val < 0) return false;
            sum += val * Weights[i];
        }
        var rem = sum % 11;
        var check = rem == 10 ? 'X' : (char)('0' + rem);
        return v[8] == check;
    }

    /// <summary>
    /// True when <paramref name="candidate"/> is consistent with a masked VIN from a listing,
    /// e.g. masked "4T1BF3EK2AU******" accepts a candidate that starts "4T1BF3EK2AU".
    /// Positions marked '*' in the mask are wildcards. An empty mask imposes no constraint.
    /// </summary>
    public static bool MatchesMaskedPattern(string candidate, string? masked)
    {
        if (string.IsNullOrWhiteSpace(masked)) return true;
        var c = candidate.Trim().ToUpperInvariant();
        var m = masked.Trim().ToUpperInvariant();
        var n = Math.Min(c.Length, m.Length);
        for (var i = 0; i < n; i++)
            if (m[i] != '*' && m[i] != c[i]) return false;
        return true;
    }

    private static int Transliterate(char c)
    {
        if (c >= '0' && c <= '9') return c - '0';
        return c switch
        {
            'A' => 1, 'B' => 2, 'C' => 3, 'D' => 4, 'E' => 5, 'F' => 6, 'G' => 7, 'H' => 8,
            'J' => 1, 'K' => 2, 'L' => 3, 'M' => 4, 'N' => 5, 'P' => 7, 'R' => 9,
            'S' => 2, 'T' => 3, 'U' => 4, 'V' => 5, 'W' => 6, 'X' => 7, 'Y' => 8, 'Z' => 9,
            _ => -1, // I, O, Q or invalid
        };
    }
}
