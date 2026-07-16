using System;

namespace B44.Common;

/// <summary>
/// B44 sugar over the BCL <see cref="TimeProvider"/> abstraction — the
/// standard replacement for the old per-game <c>ITimeSource</c>. Inject
/// <see cref="TimeProvider"/> into domain code; use
/// <see cref="TimeProvider.System"/> in production and
/// <c>FakeTimeProvider</c> (Microsoft.Extensions.TimeProvider.Testing)
/// in tests.
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>Current UTC time expressed as Unix seconds.</summary>
    public static long GetUtcNowUnixSeconds(this TimeProvider timeProvider)
        => timeProvider.GetUtcNow().ToUnixTimeSeconds();
}
