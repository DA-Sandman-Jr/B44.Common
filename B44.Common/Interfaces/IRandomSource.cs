namespace B44.Common.Interfaces;

/// <summary>
/// Engine-independent random number source. Inject into any code path that
/// needs randomness so tests can supply a deterministic seeded implementation
/// instead of relying on global mutable state.
/// Convention: a nullable <c>IRandomSource?</c> parameter means "null =
/// deterministic" — the callee falls back to fixed behavior, never to a
/// hidden global RNG.
/// </summary>
public interface IRandomSource
{
    void Randomize();

    int Randi();

    /// <summary>Random integer, inclusive on BOTH ends (the Godot <c>randi_range</c> contract).</summary>
    int RandiRange(int min, int max);

    float Randf();

    /// <summary>Non-negative random integer less than <paramref name="exclusiveMax"/>.</summary>
    int NextInt(int exclusiveMax) => RandiRange(0, exclusiveMax - 1);

    /// <summary>Random double in [0, 1). The default implementation widens <see cref="Randf"/>.</summary>
    double NextDouble() => Randf();
}
