using B44.Common.Presentation;
using Xunit;

namespace B44.Common.Tests;

public class ViewportScaleTests
{
    [Fact]
    public void Compute_ReferenceViewport_AppliesReadabilityBoost()
    {
        Assert.Equal(1.1f, ViewportScale.Compute(1920f, 1080f, 1.1f));
    }

    [Fact]
    public void Compute_LargerViewport_UsesGeometricMean()
    {
        Assert.Equal(2.2f, ViewportScale.Compute(3840f, 2160f, 1.1f));
    }

    [Fact]
    public void Compute_SmallerViewport_ClampsToMinimum()
    {
        Assert.Equal(0.85f, ViewportScale.Compute(960f, 540f, 1.1f));
    }

    [Fact]
    public void Compute_VerySmallViewport_ClampsToMinimum()
    {
        Assert.Equal(0.85f, ViewportScale.Compute(100f, 100f, 1.2f));
    }

    [Fact]
    public void Compute_VeryLargeViewport_ClampsToMaximum()
    {
        Assert.Equal(2.5f, ViewportScale.Compute(7680f, 4320f, 1.2f));
    }

    [Fact]
    public void Compute_NowhereToNestBehavior_UsesItsReadabilityBoost()
    {
        Assert.Equal(1.1f, ViewportScale.Compute(1920f, 1080f, 1.1f));
    }

    [Fact]
    public void Compute_WhispersOfTheEarthBehavior_UsesItsReadabilityBoost()
    {
        Assert.Equal(1.2f, ViewportScale.Compute(1920f, 1080f, 1.2f));
    }

    [Fact]
    public void Scale_RoundsMidpointsAwayFromZero()
    {
        Assert.Equal(13, ViewportScale.Scale(10, 1.25f));
    }

    [Fact]
    public void ScaleAtLeastBase_DoesNotShrinkBelowBase()
    {
        Assert.Equal(10, ViewportScale.ScaleAtLeastBase(10, 0.85f));
    }
}
