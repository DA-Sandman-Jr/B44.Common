using Xunit;

namespace B44.Common.Tests;

public class NumberFormatterTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(7, "7")]
    [InlineData(42.6, "42.6")]
    [InlineData(999, "999")]
    [InlineData(1_000, "1.00K")]
    [InlineData(1_500, "1.50K")]
    [InlineData(15_400, "15.4K")]
    [InlineData(999_000, "999K")]
    [InlineData(1_200_000, "1.20M")]
    [InlineData(3_450_000_000, "3.45B")]
    [InlineData(7_100_000_000_000, "7.10T")]
    [InlineData(2_000_000_000_000_000, "2.00Q")]
    [InlineData(-1_500, "-1.50K")]
    [InlineData(double.NaN, "0")]
    [InlineData(double.PositiveInfinity, "0")]
    public void Format_ProducesCompactHudStrings(double value, string expected)
    {
        Assert.Equal(expected, NumberFormatter.Format(value));
    }

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(59, "0:59")]
    [InlineData(90, "1:30")]
    [InlineData(3_600, "1:00:00")]
    [InlineData(5_025, "1:23:45")]
    [InlineData(-5, "0:00")]
    public void FormatDuration_ProducesTimerStrings(long seconds, string expected)
    {
        Assert.Equal(expected, NumberFormatter.FormatDuration(seconds));
    }
}
