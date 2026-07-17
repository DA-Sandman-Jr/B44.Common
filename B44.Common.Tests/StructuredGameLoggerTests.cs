using System;
using System.Collections.Generic;
using B44.Common.Diagnostics;
using Xunit;

namespace B44.Common.Tests;

public class StructuredGameLoggerTests
{
    // The package ships no categories — tests declare their own the way a
    // game's Core project would.
    private static class Categories
    {
        public static readonly LogCategory Flow = new("Flow");
        public static readonly LogCategory Save = new("Save");
        public static readonly LogCategory Turn = new("Turn");
        public static readonly LogCategory Campaign = new("Campaign");
    }

    // ── LogCategory ──────────────────────────────────────────────────────────

    [Fact]
    public void LogCategory_EqualByName()
    {
        Assert.Equal(new LogCategory("Save"), new LogCategory("Save"));
        Assert.NotEqual(new LogCategory("Save"), new LogCategory("Flow"));
    }

    [Fact]
    public void LogCategory_ToString_IsTheName()
    {
        Assert.Equal("Save", Categories.Save.ToString());
    }

    // ── Format ──────────────────────────────────────────────────────────────

    [Fact]
    public void Format_ContainsCategoryAndSeverityAndEventName()
    {
        var evt = new StructuredLogEvent(
            Categories.Campaign,
            LogSeverity.Info,
            "test_event",
            "corr-123",
            new Dictionary<string, object?>(StringComparer.Ordinal));

        string result = StructuredGameLogger.Format(evt);

        Assert.Contains("[Campaign]", result);
        Assert.Contains("Info", result);
        Assert.Contains("test_event", result);
        Assert.Contains("corr-123", result);
    }

    [Fact]
    public void Format_WithFields_IncludesKeyValuePairs()
    {
        var fields = new Dictionary<string, object?>(StringComparer.Ordinal) { ["foo"] = "bar", ["count"] = 3 };
        var evt = new StructuredLogEvent(Categories.Save, LogSeverity.Warning, "some_event", "x", fields);

        string result = StructuredGameLogger.Format(evt);

        Assert.Contains("foo=bar", result);
        Assert.Contains("count=3", result);
    }

    [Fact]
    public void Format_NoFields_DoesNotContainSeparator()
    {
        var evt = new StructuredLogEvent(Categories.Flow, LogSeverity.Debug, "no_fields", "x",
            new Dictionary<string, object?>(StringComparer.Ordinal));

        string result = StructuredGameLogger.Format(evt);

        Assert.DoesNotContain("::", result);
    }

    // ── Sink dispatch ────────────────────────────────────────────────────────

    [Fact]
    public void Log_InvokesSinkWithEvent()
    {
        StructuredLogEvent? captured = null;
        var logger = new StructuredGameLogger((evt, _) => captured = evt);

        logger.Log(Categories.Turn, LogSeverity.Info, "turn_event");

        Assert.NotNull(captured);
        Assert.Equal("turn_event", captured!.Value.EventName);
        Assert.Equal(Categories.Turn, captured.Value.Category);
    }

    [Fact]
    public void Log_NullSink_DoesNotThrow()
    {
        var logger = new StructuredGameLogger(sink: null);

        logger.Log(Categories.Flow, LogSeverity.Info, "no_sink_event");
    }

    [Fact]
    public void Log_SuppressedBySeverityFilter_DoesNotInvokeSink()
    {
        int callCount = 0;
        var logger = new StructuredGameLogger((_, _) => callCount++);
        logger.Verbosity.SetMinimum(Categories.Turn, LogSeverity.Warning);

        logger.Log(Categories.Turn, LogSeverity.Info, "below_threshold");

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void Log_PassesSeverityFilter_InvokesSink()
    {
        int callCount = 0;
        var logger = new StructuredGameLogger((_, _) => callCount++);
        logger.Verbosity.SetMinimum(Categories.Turn, LogSeverity.Warning);

        logger.Log(Categories.Turn, LogSeverity.Warning, "at_threshold");

        Assert.Equal(1, callCount);
    }

    // ── BeginOperation / EnsureCorrelation ──────────────────────────────────

    [Fact]
    public void BeginOperation_SetsCorrelationForScope_ThenRestores()
    {
        string? duringScope = null;
        string? afterScope = null;

        // Run on a fresh thread so the AsyncLocal starts clean and is not
        // polluted by other tests' EnsureCorrelation calls in the same context.
        var thread = new System.Threading.Thread(() =>
        {
            var logger = new StructuredGameLogger((evt, _) =>
            {
                if (duringScope == null)
                {
                    duringScope = evt.CorrelationId;
                }
                else
                {
                    afterScope = evt.CorrelationId;
                }
            });

            using (StructuredGameLogger.BeginOperation("test-op"))
            {
                logger.Log(Categories.Flow, LogSeverity.Info, "inside");
            }

            logger.Log(Categories.Flow, LogSeverity.Info, "outside");
        });
        thread.Start();
        thread.Join();

        Assert.NotNull(duringScope);
        Assert.StartsWith("test-op-", duringScope);
        // After the scope is restored the correlation resets; outside log gets a fresh id
        Assert.NotNull(afterScope);
        Assert.NotEqual(duringScope, afterScope);
    }

    [Fact]
    public void EnsureCorrelation_ReusesExistingScope()
    {
        using (StructuredGameLogger.BeginOperation("my-op"))
        {
            string first = StructuredGameLogger.EnsureCorrelation("other");
            string second = StructuredGameLogger.EnsureCorrelation("another");

            Assert.Equal(first, second);
            Assert.StartsWith("my-op-", first);
        }
    }

    [Fact]
    public void EnsureCorrelation_WithNoScope_CreatesNewId()
    {
        // Run in a fresh thread to avoid any ambient scope from other tests
        string? result = null;
        var thread = new System.Threading.Thread(() =>
        {
            result = StructuredGameLogger.EnsureCorrelation("fresh");
        });
        thread.Start();
        thread.Join();

        Assert.NotNull(result);
        Assert.StartsWith("fresh-", result);
    }

    // ── LogVerbosityConfig ───────────────────────────────────────────────────

    [Fact]
    public void LogVerbosityConfig_DefaultMinimum_IsInfo()
    {
        var config = new LogVerbosityConfig();

        Assert.True(config.IsEnabled(Categories.Campaign, LogSeverity.Info));
        Assert.False(config.IsEnabled(Categories.Campaign, LogSeverity.Debug));
    }

    [Fact]
    public void LogVerbosityConfig_PerCategoryOverride_TakesPrecedence()
    {
        var config = new LogVerbosityConfig(LogSeverity.Info);
        config.SetMinimum(Categories.Save, LogSeverity.Error);

        Assert.False(config.IsEnabled(Categories.Save, LogSeverity.Warning));
        Assert.True(config.IsEnabled(Categories.Save, LogSeverity.Error));
        // Other categories still use default
        Assert.True(config.IsEnabled(Categories.Campaign, LogSeverity.Info));
    }

    [Fact]
    public void LogVerbosityConfig_CustomDefault_AppliesToUnsetCategories()
    {
        var config = new LogVerbosityConfig(LogSeverity.Warning);

        Assert.False(config.IsEnabled(Categories.Flow, LogSeverity.Info));
        Assert.True(config.IsEnabled(Categories.Flow, LogSeverity.Warning));
    }
}
