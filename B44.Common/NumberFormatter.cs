using System;
using System.Globalization;

namespace B44.Common;

/// <summary>
/// Formats large HUD amounts compactly (1.2K, 3.40M, 12.0B...) and durations
/// as timer strings. Engine-free so formatting rules stay unit-testable.
/// </summary>
public static class NumberFormatter
{
    private static readonly (double Threshold, string Suffix)[] Scales =
    [
        (1e15, "Q"),
        (1e12, "T"),
        (1e9, "B"),
        (1e6, "M"),
        (1e3, "K"),
    ];

    public static string Format(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return "0";
        }

        bool negative = value < 0;
        double magnitude = Math.Abs(value);
        string formatted = FormatMagnitude(magnitude);
        return negative ? "-" + formatted : formatted;
    }

    private static string FormatMagnitude(double magnitude)
    {
        foreach ((double threshold, string suffix) in Scales)
        {
            if (magnitude >= threshold)
            {
                double scaled = magnitude / threshold;
                string digits = scaled >= 100
                    ? scaled.ToString("F0", CultureInfo.InvariantCulture)
                    : scaled >= 10
                        ? scaled.ToString("F1", CultureInfo.InvariantCulture)
                        : scaled.ToString("F2", CultureInfo.InvariantCulture);
                return digits + suffix;
            }
        }

        return magnitude >= 100 || magnitude == Math.Floor(magnitude)
            ? magnitude.ToString("F0", CultureInfo.InvariantCulture)
            : magnitude.ToString("F1", CultureInfo.InvariantCulture);
    }

    /// <summary>Formats a duration in seconds as m:ss or h:mm:ss for timers.</summary>
    public static string FormatDuration(long totalSeconds)
    {
        if (totalSeconds < 0)
        {
            totalSeconds = 0;
        }

        long hours = totalSeconds / 3600;
        long minutes = totalSeconds % 3600 / 60;
        long seconds = totalSeconds % 60;
        return hours > 0
            ? $"{hours}:{minutes:D2}:{seconds:D2}"
            : $"{minutes}:{seconds:D2}";
    }
}
