using System;
using Xunit;

namespace B44.Common.Tests;

public class RgbaTests
{
    [Fact]
    public void FromHex_SixDigits_ParsesOpaqueColor()
    {
        var color = Rgba.FromHex("#ff8000");

        Assert.Equal(1f, color.R);
        Assert.Equal(128f / 255f, color.G);
        Assert.Equal(0f, color.B);
        Assert.Equal(1f, color.A);
    }

    [Fact]
    public void FromHex_EightDigits_ParsesAlpha()
    {
        var color = Rgba.FromHex("#00000080");

        Assert.Equal(128f / 255f, color.A);
    }

    [Fact]
    public void FromHex_LeadingHashOptional()
    {
        Assert.Equal(Rgba.FromHex("#336699"), Rgba.FromHex("336699"));
    }

    [Theory]
    [InlineData("#fff")]
    [InlineData("#12345")]
    [InlineData("#123456789")]
    public void FromHex_WrongLength_Throws(string hex)
    {
        Assert.Throws<FormatException>(() => Rgba.FromHex(hex));
    }

    [Fact]
    public void Lerp_AtZero_ReturnsStart_AtOne_ReturnsEnd()
    {
        var from = new Rgba(0f, 0f, 0f);
        var to = new Rgba(1f, 1f, 1f);

        Assert.Equal(from, from.Lerp(to, 0f));
        Assert.Equal(to, from.Lerp(to, 1f));
    }

    [Fact]
    public void Lerp_Midpoint_InterpolatesComponentwise()
    {
        var from = new Rgba(0f, 0.2f, 1f, 0f);
        var to = new Rgba(1f, 0.4f, 0f, 1f);

        Rgba mid = from.Lerp(to, 0.5f);

        Assert.Equal(0.5f, mid.R);
        Assert.Equal(0.3f, mid.G, precision: 5);
        Assert.Equal(0.5f, mid.B);
        Assert.Equal(0.5f, mid.A);
    }

    [Fact]
    public void Equality_ComparesAllComponents()
    {
        var a = new Rgba(0.1f, 0.2f, 0.3f, 0.4f);
        var b = new Rgba(0.1f, 0.2f, 0.3f, 0.4f);
        var c = new Rgba(0.1f, 0.2f, 0.3f, 0.5f);

        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void DefaultAlpha_IsOpaque()
    {
        Assert.Equal(1f, new Rgba(0f, 0f, 0f).A);
    }
}
