using System;

namespace B44.Common;

/// <summary>
/// Immutable 2D float vector used by Core systems without an engine dependency.
/// Each game bridges to <c>Godot.Vector2</c> via its own extension methods at
/// the Godot boundary (e.g. <c>Utils/Vec2Extensions.cs</c>); never reference
/// engine types from code that consumes this.
/// </summary>
public readonly struct Vec2(float x, float y) : IEquatable<Vec2>
{
    public float X { get; } = x;

    public float Y { get; } = y;

    public static readonly Vec2 Zero = new(0f, 0f);

    public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);

    public override bool Equals(object? obj) => obj is Vec2 other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Y);

    public static bool operator ==(Vec2 left, Vec2 right) => left.Equals(right);

    public static bool operator !=(Vec2 left, Vec2 right) => !left.Equals(right);

    public static Vec2 operator +(Vec2 left, Vec2 right) => new(left.X + right.X, left.Y + right.Y);

    public static Vec2 operator -(Vec2 left, Vec2 right) => new(left.X - right.X, left.Y - right.Y);

    public float LengthSquared() => (X * X) + (Y * Y);

    public float Length() => MathF.Sqrt(LengthSquared());

    public float DistanceSquaredTo(Vec2 other) => (this - other).LengthSquared();

    public float DistanceTo(Vec2 other) => (this - other).Length();

    public override string ToString() => $"({X}, {Y})";
}
