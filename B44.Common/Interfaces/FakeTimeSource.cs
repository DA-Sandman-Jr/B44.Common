namespace B44.Common.Interfaces;

/// <summary>Manually advanced clock for deterministic timer tests.</summary>
public sealed class FakeTimeSource : ITimeSource
{
    public long UtcNowUnixSeconds { get; set; } = 1_000_000;

    public void Advance(long seconds) => UtcNowUnixSeconds += seconds;
}
