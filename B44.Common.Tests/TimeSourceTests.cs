using System;
using B44.Common.Interfaces;
using Xunit;

namespace B44.Common.Tests;

public class TimeSourceTests
{
    [Fact]
    public void FakeTimeSource_Advance_MovesTheClockForward()
    {
        var time = new FakeTimeSource { UtcNowUnixSeconds = 100 };

        time.Advance(50);

        Assert.Equal(150, time.UtcNowUnixSeconds);
    }

    [Fact]
    public void SystemTimeSource_TracksUtcNow()
    {
        var time = new SystemTimeSource();
        long expected = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        Assert.InRange(time.UtcNowUnixSeconds, expected - 2, expected + 2);
    }
}
