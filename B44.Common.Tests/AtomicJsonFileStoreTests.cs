using System;
using System.IO;
using B44.Common.Persistence;
using Xunit;

namespace B44.Common.Tests;

public class AtomicJsonFileStoreTests : IDisposable
{
    private readonly string _tempDir;

    public AtomicJsonFileStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "b44-tests-" + Guid.NewGuid().ToString("N"));
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

        public string? Name { get; set; }
    }

    [Fact]
    public void Load_ReturnsNullWhenNoSaveExists()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        Assert.Null(store.Load());
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        store.Save(new TestState { Score = 42, Name = "priestess" });
        TestState? loaded = store.Load();

        Assert.NotNull(loaded);
        Assert.Equal(42, loaded!.Score);
        Assert.Equal("priestess", loaded.Name);
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        store.Save(new TestState());

        Assert.True(File.Exists(SavePath));
        Assert.False(File.Exists(SavePath + ".tmp"));
    }

    [Fact]
    public void Save_OverwritesExistingSaveAtomically()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);
        store.Save(new TestState { Score = 1 });

        store.Save(new TestState { Score = 2 });

        Assert.Equal(2, store.Load()!.Score);
    }

    [Fact]
    public void Clear_DeletesTheSave()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);
        store.Save(new TestState());

        store.Clear();

        Assert.Null(store.Load());
        Assert.False(File.Exists(SavePath));
    }

    [Fact]
    public void Load_ThrowsStoreExceptionOnCorruptSave()
    {
        File.WriteAllText(SavePath, "{ not json ]");
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        Assert.Throws<StoreException>(() => store.Load());
    }

    [Fact]
    public void SecondSave_RotatesPreviousSaveToBackup()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        store.Save(new TestState { Score = 1 });
        store.Save(new TestState { Score = 2 });

        Assert.True(File.Exists(SavePath + ".bak"));
        Assert.Equal(2, store.Load()!.Score);
        Assert.Contains("1", File.ReadAllText(SavePath + ".bak"));
    }

    [Fact]
    public void CorruptMain_WithBackup_RecoversLastGoodSave()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);
        store.Save(new TestState { Score = 1 });
        store.Save(new TestState { Score = 2 });

        File.WriteAllText(SavePath, "{ torn write ]");

        Assert.Equal(1, store.Load()!.Score);
    }

    [Fact]
    public void MissingMain_WithBackup_LoadsBackup()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);
        store.Save(new TestState { Score = 1 });
        store.Save(new TestState { Score = 2 });

        File.Delete(SavePath);

        Assert.Equal(1, store.Load()!.Score);
    }

    [Fact]
    public void CorruptMain_WithCorruptBackup_Throws()
    {
        File.WriteAllText(SavePath, "{ bad ]");
        File.WriteAllText(SavePath + ".bak", "{ also bad ]");
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        Assert.Throws<StoreException>(() => store.Load());
    }

    [Fact]
    public void Clear_RemovesBackupToo()
    {
        var store = new AtomicJsonFileStore<TestState>(SavePath);
        store.Save(new TestState { Score = 1 });
        store.Save(new TestState { Score = 2 });

        store.Clear();

        Assert.Null(store.Load());
        Assert.False(File.Exists(SavePath));
        Assert.False(File.Exists(SavePath + ".bak"));
    }

    [Fact]
    public void Load_ReturnsNullOnEmptyFile()
    {
        File.WriteAllText(SavePath, "   ");
        var store = new AtomicJsonFileStore<TestState>(SavePath);

        Assert.Null(store.Load());
    }

    [Fact]
    public void Constructor_RejectsEmptyPath()
    {
        Assert.Throws<ArgumentException>(() => new AtomicJsonFileStore<TestState>("  "));
    }

    [Fact]
    public void Constructor_CreatesMissingDirectory()
    {
        string nested = Path.Combine(_tempDir, "a", "b", "save.json");

        var store = new AtomicJsonFileStore<TestState>(nested);
        store.Save(new TestState { Score = 7 });

        Assert.Equal(7, store.Load()!.Score);
    }
}
