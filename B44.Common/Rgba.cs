using System;

namespace B44.Common;

/// <summary>
/// Engine-free RGBA color with float components in [0, 1]. Each game bridges
/// to <c>Godot.Color</c> at the Godot boundary; mirrors the
/// <c>System.Numerics.Vector2</c> ↔ <c>Godot.Vector2</c> convention.
/// </summary>
public readonly struct Rgba : IEquatable<Rgba>
{
    public Rgba(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    public float R { get; }

    public float G { get; }

    public float B { get; }

    public float A { get; }

    /// <summary>
    /// Parses <c>#rrggbb</c> or <c>#rrggbbaa</c> (leading <c>#</c> optional).
    /// </summary>
    public static Rgba FromHex(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);
        string digits = hex.StartsWith('#') ? hex[1..] : hex;

        if (digits.Length is not (6 or 8))
        {
            throw new FormatException($"Expected #rrggbb or #rrggbbaa, got '{hex}'.");
        }

        byte r = Convert.ToByte(digits[..2], 16);
        byte g = Convert.ToByte(digits[2..4], 16);
        byte b = Convert.ToByte(digits[4..6], 16);
        byte a = digits.Length == 8 ? Convert.ToByte(digits[6..8], 16) : (byte)255;

        return new Rgba(r / 255f, g / 255f, b / 255f, a / 255f);
    }

    /// <summary>Componentwise linear interpolation (matches <c>Godot.Color.Lerp</c>).</summary>
    public Rgba Lerp(Rgba to, float weight)
    {
        return new Rgba(
            R + ((to.R - R) * weight),
            G + ((to.G - G) * weight),
            B + ((to.B - B) * weight),
            A + ((to.A - A) * weight));
    }

    public bool Equals(Rgba other) => R == other.R && G == other.G && B == other.B && A == other.A;

    public override bool Equals(object? obj) => obj is Rgba other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Rgba left, Rgba right) => left.Equals(right);

    public static bool operator !=(Rgba left, Rgba right) => !left.Equals(right);

    public override string ToString() => $"Rgba({R:0.###}, {G:0.###}, {B:0.###}, {A:0.###})";
}
