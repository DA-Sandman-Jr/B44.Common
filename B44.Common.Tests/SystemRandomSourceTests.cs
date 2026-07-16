using System;
using B44.Common.Interfaces;
using Xunit;

namespace B44.Common.Tests;

public class SystemRandomSourceTests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new SystemRandomSource(42);
        var b = new SystemRandomSource(42);

        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(a.Randi(), b.Randi());
        }
    }

    [Fact]
    public void RandiRange_IsInclusiveOnBothEnds()
    {
        var rng = new SystemRandomSource(7);
        bool sawMin = false;
        bool sawMax = false;

        for (int i = 0; i < 1_000; i++)
        {
            int value = rng.RandiRange(1, 3);
            Assert.InRange(value, 1, 3);
            sawMin |= value == 1;
            sawMax |= value == 3;
        }

        Assert.True(sawMin, "RandiRange never produced the inclusive minimum.");
        Assert.True(sawMax, "RandiRange never produced the inclusive maximum.");
    }

    [Fact]
    public void RandiRange_SingleValueRange_ReturnsThatValue()
    {
        var rng = new SystemRandomSource(1);

        Assert.Equal(5, rng.RandiRange(5, 5));
    }

    [Fact]
    public void Randf_IsInUnitInterval()
    {
        var rng = new SystemRandomSource(11);

        for (int i = 0; i < 100; i++)
        {
            float value = rng.Randf();
            Assert.InRange(value, 0f, 1f);
            Assert.True(value < 1f);
        }
    }

    [Fact]
    public void NextInt_MatchesRawSystemRandomSequence()
    {
        // Consumers ported from a raw Random-backed source rely on seeded
        // sequences being identical to System.Random.
        var rng = new SystemRandomSource(42);
        var raw = new Random(42);

        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(raw.Next(10), rng.NextInt(10));
        }
    }

    [Fact]
    public void NextDouble_MatchesRawSystemRandomSequence()
    {
        var rng = new SystemRandomSource(42);
        var raw = new Random(42);

        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(raw.NextDouble(), rng.NextDouble());
        }
    }

    [Fact]
    public void Randomize_ReplacesTheSeededSequence()
    {
        var seeded = new SystemRandomSource(42);
        var reference = new SystemRandomSource(42);

        seeded.Randomize();

        // After Randomize the sequences should diverge (probability of 20
        // consecutive matches by chance is negligible).
        bool anyDifferent = false;
        for (int i = 0; i < 20; i++)
        {
            anyDifferent |= seeded.Randi() != reference.Randi();
        }

        Assert.True(anyDifferent);
    }

    [Fact]
    public void WrappedRandom_IsUsedDirectly()
    {
        var rng = new SystemRandomSource(new Random(99));
        var raw = new Random(99);

        Assert.Equal(raw.Next(), rng.Randi());
    }

    [Fact]
    public void InterfaceDefaults_DeriveFromTheFourCoreMembers()
    {
        // A minimal fake implementing only the four core members still gets
        // NextInt/NextDouble via the interface defaults — existing game test
        // fakes must not need new members after migration.
        IRandomSource fake = new FourMemberFake();

        Assert.Equal(0, fake.NextInt(1));
        Assert.Equal(0.25, fake.NextDouble(), precision: 5);
    }

    private sealed class FourMemberFake : IRandomSource
    {
        public void Randomize()
        {
        }

        public int Randi() => 0;

        public int RandiRange(int min, int max) => min;

        public float Randf() => 0.25f;
    }
}
