using System;
using System.IO;
using System.Linq;
using System.Reflection;
using B44.Common.Diagnostics;
using Xunit;

namespace B44.Common.Tests;

/// <summary>
/// The invariant behind multi-targeting: consumers on either target framework
/// get the same package, not two packages that happen to share a name.
/// </summary>
/// <remarks>
/// The <c>netstandard2.1</c> target exists so hosts whose scripting profile is
/// .NET Standard 2.1 can load this assembly at all — a <c>net8.0</c> assembly
/// references <c>System.Runtime 8.0.0.0</c> and such a host refuses the whole
/// file. That reach is only worth having while both targets stay honestly
/// identical, which is what these assert.
/// </remarks>
public class TargetFrameworkParityTests
{
    /// <summary>
    /// The one file allowed to compile differently per target: it declares a
    /// type the compiler requires for <c>init</c> setters and that .NET 5+
    /// already supplies. It is <c>internal</c> and adds no public surface.
    /// </summary>
    private const string CompilerPolyfill = "IsExternalInit.cs";

    private static readonly string RepositoryRoot =
        typeof(TargetFrameworkParityTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "B44RepositoryRoot")
            .Value!;

    [Fact]
    public void Package_TargetsTheNetStandardProfileAlongsideNet8()
    {
        string project = File.ReadAllText(
            Path.Combine(RepositoryRoot, "B44.Common", "B44.Common.csproj"));

        Assert.Contains(
            "<TargetFrameworks>netstandard2.1;net8.0</TargetFrameworks>",
            project,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Sources_ConditionNoApiOnTheTargetFramework()
    {
        // A single #if outside the polyfill is enough to make the package mean
        // two different things depending on who restored it, and nothing else in
        // the build would notice: both targets would still compile, both would
        // still pass this suite, and the difference would surface as a missing
        // member in one consumer.
        string[] conditioned = Directory
            .EnumerateFiles(Path.Combine(RepositoryRoot, "B44.Common"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !string.Equals(Path.GetFileName(file), CompilerPolyfill, StringComparison.Ordinal))
            .Where(file => File.ReadLines(file).Any(line => line.TrimStart().StartsWith("#if", StringComparison.Ordinal)))
            .Select(file => Path.GetFileName(file))
            .ToArray();

        Assert.True(
            conditioned.Length == 0,
            "Conditional compilation outside the compiler polyfill means the public API differs by " +
            $"target framework: {string.Join(", ", conditioned)}.");
    }

    [Fact]
    public void CompilerPolyfill_AddsNoPublicSurface()
    {
        Assert.DoesNotContain(
            typeof(StructuredGameLogger).Assembly.GetExportedTypes(),
            type => string.Equals(type.Name, "IsExternalInit", StringComparison.Ordinal));
    }
}
