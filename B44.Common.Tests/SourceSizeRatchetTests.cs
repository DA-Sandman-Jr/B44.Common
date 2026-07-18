using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using B44.Common.Quality;
using Xunit;

namespace B44.Common.Tests;

public class SourceSizeRatchetTests : IDisposable
{
    private readonly string _tempDir;

    public SourceSizeRatchetTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "b44-ratchet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private string BaselinePath => Path.Combine(_tempDir, "baseline.txt");

    private void WriteSource(string relativePath, int lines)
    {
        string path = Path.Combine(_tempDir, "src", relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, Enumerable.Repeat("// line", lines));
    }

    private string SourceRoot => Path.Combine(_tempDir, "src");

    [Fact]
    public void SmallFiles_NoBaseline_NoViolations()
    {
        WriteSource("A.cs", 100);
        WriteSource("Sub/B.cs", 499);

        Assert.Empty(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
    }

    [Fact]
    public void NewFileOverHardMax_IsViolation()
    {
        WriteSource("Big.cs", 501);

        RatchetViolation violation = Assert.Single(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
        Assert.Equal("Big.cs", violation.RelativePath);
        Assert.Equal(SourceSizeRatchet.NewFileMaxLines, violation.Limit);
    }

    [Fact]
    public void BaselinedFile_MayNotGrow_ButMayShrink()
    {
        WriteSource("Old.cs", 620);
        File.WriteAllLines(BaselinePath, new[] { "Old.cs = 610 # cohesive catalog" });

        RatchetViolation violation = Assert.Single(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
        Assert.Equal(610, violation.Limit);

        WriteSource("Old.cs", 590);
        Assert.Empty(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
    }

    [Fact]
    public void BaselineEntry_AllowsDeliberateExceptionAboveHardMax()
    {
        WriteSource("Catalog.cs", 700);
        File.WriteAllLines(BaselinePath, new[] { "Catalog.cs = 750 # declarative monster catalog" });

        Assert.Empty(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
    }

    [Fact]
    public void BinAndObj_AreIgnored()
    {
        WriteSource("bin/Generated.cs", 9_000);
        WriteSource("obj/Generated.cs", 9_000);

        Assert.Empty(SourceSizeRatchet.Check(SourceRoot, BaselinePath));
    }

    [Fact]
    public void ExcludeDirs_SkipsTopLevelDirectoriesOnly()
    {
        WriteSource("Game.Tests/HugeTests.cs", 9_000);
        WriteSource(".godot/mono/Generated.cs", 9_000);
        WriteSource("Core/Game.Tests/NestedSameName.cs", 600);

        IReadOnlyList<RatchetViolation> violations = SourceSizeRatchet.Check(
            SourceRoot, BaselinePath, new[] { "Game.Tests", ".godot" });

        // Top-level exclusions apply; the same name nested deeper does not.
        RatchetViolation violation = Assert.Single(violations);
        Assert.Equal("Core/Game.Tests/NestedSameName.cs", violation.RelativePath);
    }

    [Fact]
    public void MalformedBaseline_Throws()
    {
        WriteSource("A.cs", 10);
        File.WriteAllLines(BaselinePath, new[] { "not a valid entry" });

        Assert.Throws<FormatException>(() => SourceSizeRatchet.Check(SourceRoot, BaselinePath));
    }

    [Fact]
    public void WriteBaseline_RecordsOnlyFilesOverTrackingThreshold()
    {
        WriteSource("Small.cs", 200);
        WriteSource("Tracked.cs", 400);

        SourceSizeRatchet.WriteBaseline(SourceRoot, BaselinePath);

        string content = File.ReadAllText(BaselinePath);
        Assert.Contains("Tracked.cs = 400", content);
        Assert.DoesNotContain("Small.cs", content);
    }

    // ── The self-test: this repo's own package source obeys the ratchet ────

    [Fact]
    public void B44CommonPackageSource_ObeysTheRatchet()
    {
        string repoRoot = FindRepoRoot();
        IReadOnlyList<RatchetViolation> violations = SourceSizeRatchet.Check(
            Path.Combine(repoRoot, "B44.Common"),
            Path.Combine(repoRoot, "ratchet-baseline.txt"));

        Assert.True(
            violations.Count == 0,
            string.Join(Environment.NewLine, violations.Select(v => $"{v.RelativePath} ({v.Lines} lines): {v.Reason}")));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "B44.Common.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found from test base directory.");
    }
}
