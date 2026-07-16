using Xunit;

namespace B44.Common.Tests;

public class Vec2Tests
{
    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Vec2(1f, 2f);
        var b = new Vec2(1f, 2f);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Vec2(1f, 2f);
        var b = new Vec2(3f, 4f);

        Assert.NotEqual(a, b);
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHash()
    {
        var a = new Vec2(5f, 10f);
        var b = new Vec2(5f, 10f);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Addition_ReturnsComponentWiseSum()
    {
        var a = new Vec2(1f, 2f);
        var b = new Vec2(3f, 4f);

        Vec2 result = a + b;

        Assert.Equal(new Vec2(4f, 6f), result);
    }

    [Fact]
    public void Subtraction_ReturnsComponentWiseDifference()
    {
        var a = new Vec2(5f, 7f);
        var b = new Vec2(2f, 3f);

        Vec2 result = a - b;

        Assert.Equal(new Vec2(3f, 4f), result);
    }

    [Fact]
    public void Zero_HasBothComponentsZero()
    {
        Assert.Equal(new Vec2(0f, 0f), Vec2.Zero);
    }

    [Fact]
    public void Length_ComputesEuclideanLength()
    {
        var v = new Vec2(3f, 4f);

        Assert.Equal(5f, v.Length());
        Assert.Equal(25f, v.LengthSquared());
    }

    [Fact]
    public void DistanceTo_ComputesEuclideanDistance()
    {
        var a = new Vec2(1f, 1f);
        var b = new Vec2(4f, 5f);

        Assert.Equal(5f, a.DistanceTo(b));
        Assert.Equal(25f, a.DistanceSquaredTo(b));
    }
}
