using System;

namespace B44.Common.Interfaces;

/// <summary>
/// Engine-free clock abstraction. All wall-clock reads in a domain layer
/// (timers, expiry, offline earnings) go through this so tests can advance
/// time deterministically via <see cref="FakeTimeSource"/>.
/// </summary>
public interface ITimeSource
{
    /// <summary>Current UTC time expressed as Unix seconds.</summary>
    long UtcNowUnixSeconds { get; }
}

public sealed class SystemTimeSource : ITimeSource
{
    public long UtcNowUnixSeconds => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}
