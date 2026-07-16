using System;
using System.Diagnostics;

namespace B44.Common;

/// <summary>
/// Centralized safe type conversion utilities with consistent warning logging
/// via <see cref="Trace.TraceWarning(string)"/>. Returns null on conversion
/// failure instead of throwing.
/// </summary>
public static class SafeConvert
{
    public static int? ToInt32(object? value, string context = "")
    {
        try
        {
            return Convert.ToInt32(value);
        }
        catch (FormatException e)
        {
            TraceWarn(value, "int", context, e);
            return null;
        }
        catch (OverflowException e)
        {
            TraceWarn(value, "int", context, e);
            return null;
        }
        catch (InvalidCastException e)
        {
            TraceWarn(value, "int", context, e);
            return null;
        }
    }

    public static ulong? ToUInt64(object? value, string context = "")
    {
        try
        {
            return Convert.ToUInt64(value);
        }
        catch (FormatException e)
        {
            TraceWarn(value, "ulong", context, e);
            return null;
        }
        catch (OverflowException e)
        {
            TraceWarn(value, "ulong", context, e);
            return null;
        }
        catch (InvalidCastException e)
        {
            TraceWarn(value, "ulong", context, e);
            return null;
        }
    }

    private static void TraceWarn(object? value, string targetType, string context, Exception e)
    {
        string prefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
        Trace.TraceWarning(
            $"{prefix}Failed to convert '{value}' ({value?.GetType().Name ?? "null"}) to {targetType}: {e.Message}");
    }
}
