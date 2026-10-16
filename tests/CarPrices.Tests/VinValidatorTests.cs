using CarPrices.Domain.Vin;

namespace CarPrices.Tests;

public class VinValidatorTests
{
    [Fact]
    public void Valid_vin_passes()
    {
        Assert.True(VinValidator.IsValidVin("1HGCM82633A004352"));
    }

    [Fact]
    public void Wrong_check_digit_fails()
    {
        Assert.False(VinValidator.IsValidVin("1HGCM82633A004353"));
    }

    [Theory]
    [InlineData("1HGCM82633A00435")]   // 16 chars
    [InlineData("1HGCM82633A0043I2")]  // contains I
    [InlineData("")]
    public void Malformed_vins_fail(string vin) => Assert.False(VinValidator.IsValidVin(vin));

    [Fact]
    public void Masked_pattern_matches_prefix()
    {
        Assert.True(VinValidator.MatchesMaskedPattern("4T1BF3EK2AU123456", "4T1BF3EK2AU******"));
        Assert.True(VinValidator.MatchesMaskedPattern("4T1BF3EK2AU123456", "4T1BF3EK2AU")); // 11-char prefix
        Assert.False(VinValidator.MatchesMaskedPattern("5T1BF3EK2AU123456", "4T1BF3EK2AU******"));
        Assert.True(VinValidator.MatchesMaskedPattern("ANYTHING", null)); // no mask = no constraint
    }
}
