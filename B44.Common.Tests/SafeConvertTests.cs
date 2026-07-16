using Xunit;

namespace B44.Common.Tests;

public class SafeConvertTests
{
    [Fact]
    public void ToInt32_ConvertsNumericAndStringValues()
    {
        Assert.Equal(42, SafeConvert.ToInt32(42));
        Assert.Equal(42, SafeConvert.ToInt32(42L));
        Assert.Equal(42, SafeConvert.ToInt32("42"));
        Assert.Equal(42, SafeConvert.ToInt32(42.0));
    }

    [Fact]
    public void ToInt32_ReturnsNullOnUnconvertibleValue()
    {
        Assert.Null(SafeConvert.ToInt32("not a number"));
        Assert.Null(SafeConvert.ToInt32(long.MaxValue));
        Assert.Null(SafeConvert.ToInt32(new object()));
    }

    [Fact]
    public void ToInt32_NullConvertsToZero()
    {
        // Convert.ToInt32(null) is 0 by BCL contract; SafeConvert preserves that.
        Assert.Equal(0, SafeConvert.ToInt32(null));
    }

    [Fact]
    public void ToUInt64_ConvertsAndRejectsNegatives()
    {
        Assert.Equal(42UL, SafeConvert.ToUInt64(42));
        Assert.Equal(42UL, SafeConvert.ToUInt64("42"));
        Assert.Null(SafeConvert.ToUInt64(-1));
        Assert.Null(SafeConvert.ToUInt64("nope"));
    }
}
