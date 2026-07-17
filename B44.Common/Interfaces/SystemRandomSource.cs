using System;

namespace B44.Common.Interfaces;

/// <summary>
/// Default <see cref="IRandomSource"/> backed by <see cref="System.Random"/>.
/// Production code uses the parameterless constructor; tests inject a fixed
/// seed via <see cref="SystemRandomSource(int)"/> or wrap an existing
/// <see cref="System.Random"/> via <see cref="SystemRandomSource(Random)"/>.
/// </summary>
public sealed class SystemRandomSource : IRandomSource
{
    private Random _random;

    // RS0030: constructing System.Random is banned everywhere else — this
    // class IS the sanctioned wrapper the ban points callers to.
#pragma warning disable RS0030
    public SystemRandomSource()
    {
        _random = new Random();
    }

    public SystemRandomSource(int seed)
    {
        _random = new Random(seed);
    }

    public SystemRandomSource(Random random)
    {
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public void Randomize()
    {
        _random = new Random();
    }
#pragma warning restore RS0030

    public int Randi()
    {
        return _random.Next();
    }

    public int RandiRange(int min, int max)
    {
        // Godot's randi_range is inclusive on both ends; System.Random.Next is
        // exclusive on the upper bound, so add one to match the contract.
        return _random.Next(min, max + 1);
    }

    public float Randf()
    {
        return (float)_random.NextDouble();
    }

    // Implemented directly (not via the interface defaults) so seeded
    // sequences match System.Random exactly, preserving determinism for
    // consumers that ported from a raw Random-backed implementation.
    public int NextInt(int exclusiveMax)
    {
        return _random.Next(exclusiveMax);
    }

    public double NextDouble()
    {
        return _random.NextDouble();
    }
}
