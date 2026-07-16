using System;
using System.Collections.Generic;
using System.IO;
using B44.Common.Persistence;
using Xunit;

namespace B44.Common.Tests;

public class RepositoryFactoryTests : IDisposable
{
    private readonly string _tempDir;

    public RepositoryFactoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "b44-factory-tests-" + Guid.NewGuid().ToString("N"));
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

    private string SavePath => Path.Combine(_tempDir, "save.json");

    public sealed class TestState
    {
        public int Score { get; set; }
    }

    [Fact]
    public void HealthyFileStore_IsReturnedAsIs()
    {
        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath));

        Assert.IsType<AtomicJsonFileStore<TestState>>(repository);
    }

    [Fact]
    public void CreateThrows_FallsBackToInMemoryWithWarning()
    {
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback<TestState>(
            () => throw new InvalidOperationException("no writable dir"),
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("will not persist"));

        // The fallback still round-trips for the session.
        repository.Save(new TestState { Score = 5 });
        Assert.Equal(5, repository.Load()!.Score);
    }

    [Fact]
    public void CorruptSave_IsResetAndStaysFileBacked()
    {
        File.WriteAllText(SavePath, "{ corrupt ]");
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath),
            warnings.Add);

        Assert.IsType<AtomicJsonFileStore<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("has been reset"));
        Assert.False(File.Exists(SavePath));
        Assert.Null(repository.Load());
    }

    [Fact]
    public void UnresettableSave_FallsBackToInMemory()
    {
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback<TestState>(
            () => new UnresettableStore(),
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("could not be reset"));
    }

    private sealed class UnresettableStore : IRepository<TestState>
    {
        public TestState? Load() =>
            throw new StoreException("corrupt", new IOException("bad bytes"));

        public void Save(TestState data)
        {
        }

        public void Clear() =>
            throw new StoreException("locked", new IOException("cannot delete"));
    }
}
