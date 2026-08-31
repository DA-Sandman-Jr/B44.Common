using System;

namespace B44.Common.Presentation;

/// <summary>
/// Calculates viewport-derived readability scaling for presentation surfaces.
/// </summary>
public static class ViewportScale
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float MinimumScale = 0.85f;
    private const float MaximumScale = 2.5f;

    /// <summary>
    /// Computes a clamped scale from a viewport measured against the standard
    /// 1920 by 1080 reference resolution.
    /// </summary>
    /// <param name="viewportWidth">The viewport width in presentation pixels.</param>
    /// <param name="viewportHeight">The viewport height in presentation pixels.</param>
    /// <param name="readabilityBoost">The caller's readability multiplier.</param>
    /// <returns>A scale clamped to the supported readability range.</returns>
    public static float Compute(float viewportWidth, float viewportHeight, float readabilityBoost)
    {
        float widthScale = viewportWidth / ReferenceWidth;
        float heightScale = viewportHeight / ReferenceHeight;
        float scale = MathF.Sqrt(widthScale * heightScale) * readabilityBoost;
        return MathF.Min(MaximumScale, MathF.Max(MinimumScale, scale));
    }

    /// <summary>
    /// Scales and rounds an integer presentation value.
    /// </summary>
    public static int Scale(int baseSize, float scale)
    {
        return (int)MathF.Round(baseSize * scale, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Scales and rounds an integer presentation value without reducing it
    /// below its base size.
    /// </summary>
    public static int ScaleAtLeastBase(int baseSize, float scale)
    {
        return Math.Max(baseSize, Scale(baseSize, scale));
    }
}
