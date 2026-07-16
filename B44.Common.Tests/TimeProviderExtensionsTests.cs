using System;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace B44.Common.Tests;

public class TimeProviderExtensionsTests
{
    [Fact]
    public void GetUtcNowUnixSeconds_ReflectsTheFakeClock()
    {
        var time = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_000_000));

        Assert.Equal(1_000_000, time.GetUtcNowUnixSeconds());
    }

    [Fact]
    public void GetUtcNowUnixSeconds_AdvancesWithTheFakeClock()
    {
        var time = new FakeTimeProvider(DateTimeOffset.FromUnixTimeSeconds(1_000_000));

        time.Advance(TimeSpan.FromSeconds(50));

        Assert.Equal(1_000_050, time.GetUtcNowUnixSeconds());
    }

    [Fact]
    public void GetUtcNowUnixSeconds_SystemProvider_TracksUtcNow()
    {
        long expected = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.InRange(TimeProvider.System.GetUtcNowUnixSeconds(), expected - 2, expected + 2);
    }
}
