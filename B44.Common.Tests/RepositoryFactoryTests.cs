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

    [Theory]
    [InlineData(UnreadableSavePolicy.Preserve)]
    [InlineData(UnreadableSavePolicy.Reset)]
    public void HealthyFileStore_IsReturnedAsIs_UnderEitherPolicy(UnreadableSavePolicy policy)
    {
        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath),
            policy);

        Assert.IsType<AtomicJsonFileStore<TestState>>(repository);
    }

    [Theory]
    [InlineData(UnreadableSavePolicy.Preserve)]
    [InlineData(UnreadableSavePolicy.Reset)]
    public void CreateThrows_FallsBackToInMemoryWithWarning(UnreadableSavePolicy policy)
    {
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback<TestState>(
            () => throw new InvalidOperationException("no writable dir"),
            policy,
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("will not persist"));

        // The fallback still round-trips for the session.
        repository.Save(new TestState { Score = 5 });
        Assert.Equal(5, repository.Load()!.Score);
    }

    [Fact]
    public void CorruptSave_UnderReset_IsDeletedAndStaysFileBacked()
    {
        File.WriteAllText(SavePath, "{ corrupt ]");
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath),
            UnreadableSavePolicy.Reset,
            warnings.Add);

        Assert.IsType<AtomicJsonFileStore<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("has been reset"));
        Assert.False(File.Exists(SavePath));
        Assert.Null(repository.Load());
    }

    [Fact]
    public void CorruptSave_UnderPreserve_IsLeftOnDiskAndSessionRunsInMemory()
    {
        File.WriteAllText(SavePath, "{ corrupt ]");
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath),
            UnreadableSavePolicy.Preserve,
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("left untouched"));

        // The unreadable bytes survive verbatim — this is the whole point of
        // the policy, so a later migration still has something to migrate.
        Assert.True(File.Exists(SavePath));
        Assert.Equal("{ corrupt ]", File.ReadAllText(SavePath));
    }

    [Fact]
    public void CorruptSave_UnderPreserve_DoesNotOverwriteTheFileWhenTheSessionSaves()
    {
        File.WriteAllText(SavePath, "{ corrupt ]");

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback(
            () => new AtomicJsonFileStore<TestState>(SavePath),
            UnreadableSavePolicy.Preserve);

        repository.Save(new TestState { Score = 7 });

        Assert.Equal("{ corrupt ]", File.ReadAllText(SavePath));
        Assert.Equal(7, repository.Load()!.Score);
    }

    [Fact]
    public void UnresettableSave_UnderReset_FallsBackToInMemory()
    {
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback<TestState>(
            () => new UnresettableStore(),
            UnreadableSavePolicy.Reset,
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.Contains(warnings, w => w.Contains("could not be reset"));
    }

    [Fact]
    public void UnresettableSave_UnderPreserve_NeverAttemptsAClear()
    {
        var store = new UnresettableStore();
        var warnings = new List<string>();

        IRepository<TestState> repository = RepositoryFactory.CreateWithFallback<TestState>(
            () => store,
            UnreadableSavePolicy.Preserve,
            warnings.Add);

        Assert.IsType<InMemoryRepository<TestState>>(repository);
        Assert.False(store.ClearAttempted);
        Assert.DoesNotContain(warnings, w => w.Contains("reset"));
    }

    private sealed class UnresettableStore : IRepository<TestState>
    {
        public bool ClearAttempted { get; private set; }

        public TestState? Load() =>
            throw new StoreException("corrupt", new IOException("bad bytes"));

        public void Save(TestState data)
        {
        }

        public void Clear()
        {
            ClearAttempted = true;
            throw new StoreException("locked", new IOException("cannot delete"));
        }
    }
}
