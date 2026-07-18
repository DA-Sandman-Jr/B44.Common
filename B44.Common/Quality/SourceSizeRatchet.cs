using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace B44.Common.Quality;

/// <summary>One file-size ratchet violation. <c>Limit</c> is the bound that was exceeded.</summary>
public sealed record RatchetViolation(string RelativePath, int Lines, int Limit, string Reason);

/// <summary>
/// Enforces the B44 Architecture Ratchet's mechanical half from a repo's test
/// suite: new production files stay at or below <see cref="NewFileMaxLines"/>,
/// and files already over <see cref="TrackThresholdLines"/> are pinned to a
/// checked-in baseline they may shrink under but never grow past. No analyzer
/// implements relative-to-baseline no-growth, which is the actual policy —
/// that is why this exists (the judgment half of the ratchet stays in review).
///
/// Baseline format, one entry per line, '#' starts a comment:
///   Path/To/File.cs = 612          # optional reason, e.g. cohesive catalog
/// Regenerate after a sanctioned extraction with <see cref="WriteBaseline"/>.
/// </summary>
public static class SourceSizeRatchet
{
    public const int NewFileMaxLines = 500;
    public const int TrackThresholdLines = 350;

    /// <summary>
    /// Scans production sources under <paramref name="productionRoot"/>
    /// (recursively, excluding bin/obj) against the baseline at
    /// <paramref name="baselinePath"/>. A missing baseline file means an
    /// empty baseline. Returns violations; empty means the ratchet holds.
    /// <paramref name="excludeDirs"/> names TOP-LEVEL directories under the
    /// root to skip entirely (e.g. the test project and ".godot" when
    /// scanning a game repo's root — tests may exceed the ratchet by
    /// doctrine, and engine folders hold generated sources).
    /// </summary>
    public static IReadOnlyList<RatchetViolation> Check(
        string productionRoot,
        string baselinePath,
        IEnumerable<string>? excludeDirs = null)
    {
        Dictionary<string, int> baseline = ReadBaseline(baselinePath);
        var violations = new List<RatchetViolation>();

        foreach ((string relativePath, int lines) in EnumerateSources(productionRoot, excludeDirs))
        {
            if (baseline.TryGetValue(relativePath, out int allowed))
            {
                if (lines > allowed)
                {
                    violations.Add(new RatchetViolation(
                        relativePath,
                        lines,
                        allowed,
                        $"grew past its baseline of {allowed} lines — extract a cohesive owner, or regenerate the baseline in the same change that performs an extraction"));
                }
            }
            else if (lines > NewFileMaxLines)
            {
                violations.Add(new RatchetViolation(
                    relativePath,
                    lines,
                    NewFileMaxLines,
                    $"new production file over {NewFileMaxLines} lines — split it, or add a baseline entry with a written cohesion reason"));
            }
        }

        return violations;
    }

    /// <summary>
    /// Writes a fresh baseline recording every production file currently over
    /// <see cref="TrackThresholdLines"/>. Existing entry comments are lost —
    /// re-add reasons for deliberate exceptions.
    /// </summary>
    public static void WriteBaseline(string productionRoot, string baselinePath, IEnumerable<string>? excludeDirs = null)
    {
        IEnumerable<string> entries = EnumerateSources(productionRoot, excludeDirs)
            .Where(s => s.Lines > TrackThresholdLines)
            .OrderBy(s => s.RelativePath, StringComparer.Ordinal)
            .Select(s => $"{s.RelativePath} = {s.Lines}");

        File.WriteAllLines(baselinePath, new[]
        {
            "# B44 source-size ratchet baseline. Files listed here exceed the",
            $"# {TrackThresholdLines}-line tracking threshold and may not grow past their recorded size.",
            "# Regenerated via SourceSizeRatchet.WriteBaseline after sanctioned extractions.",
        }.Concat(entries));
    }

    private static IEnumerable<(string RelativePath, int Lines)> EnumerateSources(
        string productionRoot,
        IEnumerable<string>? excludeDirs)
    {
        string root = Path.GetFullPath(productionRoot);
        var excluded = new HashSet<string>(excludeDirs ?? Array.Empty<string>(), StringComparer.Ordinal);
        foreach (string file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            string[] parts = relative.Split('/');
            if (parts.Any(part => part is "bin" or "obj") || excluded.Contains(parts[0]))
            {
                continue;
            }

            yield return (relative, File.ReadLines(file).Count());
        }
    }

    private static Dictionary<string, int> ReadBaseline(string baselinePath)
    {
        var baseline = new Dictionary<string, int>(StringComparer.Ordinal);
        if (!File.Exists(baselinePath))
        {
            return baseline;
        }

        foreach (string rawLine in File.ReadLines(baselinePath))
        {
            string line = rawLine.Split('#')[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            string[] parts = line.Split('=', 2);
            if (parts.Length != 2 || !int.TryParse(parts[1].Trim(), out int allowed))
            {
                throw new FormatException($"Unreadable ratchet baseline entry: '{rawLine}'");
            }

            baseline[parts[0].Trim().Replace('\\', '/')] = allowed;
        }

        return baseline;
    }
}
