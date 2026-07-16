using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace B44.Common.Diagnostics;

/// <summary>
/// A named log channel. Games declare their own category constants in their
/// Core project (e.g. <c>public static class LogCategories { public static
/// readonly LogCategory Save = new("Save"); }</c>) — the package deliberately
/// ships no game-specific categories. Keep each game's list short and add a
/// category only when a real consumer needs to filter on it.
/// </summary>
public readonly record struct LogCategory(string Name)
{
    public override string ToString() => Name;
}

public enum LogSeverity
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
}

/// <summary>
/// Per-category minimum severity. Defaults to <c>Info</c> across the board;
/// override per category as needed (e.g. raise a chatty category to
/// <c>Warning</c> to silence routine play-by-play in production builds).
/// </summary>
public sealed class LogVerbosityConfig
{
    private readonly Dictionary<LogCategory, LogSeverity> _minimumSeverity = new();

    public LogVerbosityConfig(LogSeverity defaultMinimum = LogSeverity.Info)
    {
        DefaultMinimum = defaultMinimum;
    }

    public LogSeverity DefaultMinimum { get; set; }

    public void SetMinimum(LogCategory category, LogSeverity minimum)
    {
        _minimumSeverity[category] = minimum;
    }

    public LogSeverity GetMinimum(LogCategory category)
    {
        return _minimumSeverity.TryGetValue(category, out LogSeverity minimum) ? minimum : DefaultMinimum;
    }

    public bool IsEnabled(LogCategory category, LogSeverity severity)
    {
        return severity >= GetMinimum(category);
    }
}

/// <summary>
/// One structured log event. The <c>Fields</c> dictionary should hold
/// primitives or strings — formatters call <c>ToString()</c> on each value.
/// </summary>
public readonly record struct StructuredLogEvent(
    LogCategory Category,
    LogSeverity Severity,
    string EventName,
    string CorrelationId,
    IReadOnlyDictionary<string, object?> Fields);

/// <summary>
/// Engine-free structured logger. Consumers construct one with a sink delegate
/// that routes formatted events to the host (Godot via <c>GD.Print</c>/etc.,
/// tests into a captured list). Each game keeps its own Godot-side sink
/// factory (<c>GodotLoggerFactory</c>) and its own <c>LogContext</c> factory
/// class for repeated context-dictionary shapes.
/// </summary>
public sealed class StructuredGameLogger(Action<StructuredLogEvent, string>? sink = null)
{
    private static readonly AsyncLocal<string?> CorrelationScope = new();

    public LogVerbosityConfig Verbosity { get; } = new();

    public static IDisposable BeginOperation(string operationName)
    {
        string? previous = CorrelationScope.Value;
        CorrelationScope.Value = $"{operationName}-{Guid.NewGuid():N}";
        return new CorrelationScopeRestore(previous);
    }

    public static string EnsureCorrelation(string operationName)
    {
        if (!string.IsNullOrWhiteSpace(CorrelationScope.Value))
        {
            return CorrelationScope.Value!;
        }

        CorrelationScope.Value = $"{operationName}-{Guid.NewGuid():N}";
        return CorrelationScope.Value!;
    }

    public void Log(LogCategory category, LogSeverity severity, string eventName, IReadOnlyDictionary<string, object?>? fields = null)
    {
        if (!Verbosity.IsEnabled(category, severity))
        {
            return;
        }

        string correlationId = EnsureCorrelation(category.Name.ToLowerInvariant());
        IReadOnlyDictionary<string, object?> normalizedFields = fields ?? new Dictionary<string, object?>();
        var evt = new StructuredLogEvent(category, severity, eventName, correlationId, normalizedFields);
        sink?.Invoke(evt, Format(evt));
    }

    public static string Format(StructuredLogEvent evt)
    {
        StringBuilder builder = new();
        builder.Append('[').Append(evt.Category).Append("] ")
            .Append(evt.Severity).Append(' ')
            .Append(evt.EventName)
            .Append(" correlationId=").Append(evt.CorrelationId);

        if (evt.Fields.Count > 0)
        {
            IEnumerable<string> pairs = evt.Fields.Select(p => $"{p.Key}={p.Value}");
            builder.Append(" :: ").Append(string.Join(' ', pairs));
        }

        return builder.ToString();
    }

    private sealed class CorrelationScopeRestore(string? previous) : IDisposable
    {
        public void Dispose()
        {
            CorrelationScope.Value = previous;
        }
    }
}
