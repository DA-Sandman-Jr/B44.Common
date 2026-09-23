using System;
using System.IO;
using System.Text;
using B44.Common.Persistence;
using Xunit;

namespace B44.Common.Tests;

public class AtomicFileTests : IDisposable
{
    private readonly string _tempDir;

    public AtomicFileTests()
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

    private string FilePath => Path.Combine(_tempDir, "document.dat");

    private static string? AsText(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    private static string? StrictText(string text) =>
        text.StartsWith("good:", StringComparison.Ordinal) ? text : throw new FormatException("not a document");

    [Fact]
    public void Write_ThenRead_RoundTripsBytes()
    {
        byte[] contents = [0, 1, 2, 250, 255];

        AtomicFile.Write(FilePath, contents);

        Assert.Equal(contents, AtomicFile.Read(FilePath, bytes => bytes));
    }

    [Fact]
    public void WriteText_WritesUtf8WithoutByteOrderMarkAndReadsBack()
    {
        AtomicFile.WriteText(FilePath, "good: crème brûlée ✓");

        byte[] raw = File.ReadAllBytes(FilePath);
        Assert.NotEqual(0xEF, raw[0]);
        Assert.Equal("good: crème brûlée ✓", AtomicFile.ReadText(FilePath, text => text));
    }

    [Fact]
    public void Write_CreatesMissingDirectory()
    {
        string nested = Path.Combine(_tempDir, "a", "b", "document.dat");

        AtomicFile.WriteText(nested, "good: nested");

        Assert.Equal("good: nested", AtomicFile.ReadText(nested, text => text));
    }

    [Fact]
    public void Write_ReplacingRotatesThePreviousFileToBackupAndLeavesNoTempFile()
    {
        AtomicFile.WriteText(FilePath, "good: first");
        AtomicFile.WriteText(FilePath, "good: second");

        Assert.Equal("good: second", File.ReadAllText(FilePath));
        Assert.Equal("good: first", File.ReadAllText(FilePath + ".bak"));
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Write_WhenTheTempFileCannotBeCreated_ThrowsAndLeavesThePreviousFileIntact()
    {
        AtomicFile.WriteText(FilePath, "good: kept");
        // A directory standing where the temporary file belongs refuses the write.
        Directory.CreateDirectory(FilePath + ".tmp");

        Assert.Throws<StoreException>(() => AtomicFile.WriteText(FilePath, "good: lost"));

        Assert.Equal("good: kept", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Write_WhenPromotionFails_ThrowsAndRemovesTheTempFile()
    {
        // A directory standing where the file belongs refuses the final rename.
        Directory.CreateDirectory(FilePath);

        Assert.Throws<StoreException>(() => AtomicFile.WriteText(FilePath, "good: refused"));

        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Read_ReturnsNullWhenNeitherFileExists()
    {
        Assert.Null(AtomicFile.Read(FilePath, AsText));
    }

    [Fact]
    public void Read_MissingMainRecoversTheBackup()
    {
        File.WriteAllText(FilePath + ".bak", "good: backup");

        Assert.Equal("good: backup", AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void Read_EmptyMainRecoversTheBackup()
    {
        File.WriteAllBytes(FilePath, []);
        File.WriteAllText(FilePath + ".bak", "good: backup");

        Assert.Equal("good: backup", AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void Read_UnparseableMainRecoversTheBackup()
    {
        File.WriteAllText(FilePath, "torn");
        File.WriteAllText(FilePath + ".bak", "good: backup");

        Assert.Equal("good: backup", AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void Read_MainThatParsesToNullRecoversTheBackup()
    {
        File.WriteAllText(FilePath, "   ");
        File.WriteAllText(FilePath + ".bak", "good: backup");

        Assert.Equal("good: backup", AtomicFile.ReadText(FilePath, text => string.IsNullOrWhiteSpace(text) ? null : text));
    }

    [Fact]
    public void Read_AParseableMainWinsOverTheBackup()
    {
        File.WriteAllText(FilePath, "good: main");
        File.WriteAllText(FilePath + ".bak", "good: backup");

        Assert.Equal("good: main", AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void Read_UnparseableMainWithoutUsableBackup_Throws()
    {
        File.WriteAllText(FilePath, "torn");
        Assert.Throws<StoreException>(() => AtomicFile.ReadText(FilePath, StrictText));

        File.WriteAllText(FilePath + ".bak", "also torn");
        Assert.Throws<StoreException>(() => AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void ReadText_DetectsAByteOrderMark()
    {
        File.WriteAllText(FilePath, "good: marked", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Assert.Equal("good: marked", AtomicFile.ReadText(FilePath, StrictText));
    }

    [Fact]
    public void Delete_RemovesTheFileItsBackupAndAnAbandonedTempFile()
    {
        AtomicFile.WriteText(FilePath, "good: first");
        AtomicFile.WriteText(FilePath, "good: second");
        File.WriteAllText(FilePath + ".tmp", "abandoned");

        AtomicFile.Delete(FilePath);

        Assert.False(File.Exists(FilePath));
        Assert.False(File.Exists(FilePath + ".bak"));
        Assert.False(File.Exists(FilePath + ".tmp"));
        Assert.Null(AtomicFile.Read(FilePath, AsText));
    }

    [Fact]
    public void BlankPathsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => AtomicFile.Write("  ", [1]));
        Assert.Throws<ArgumentException>(() => AtomicFile.Read("  ", AsText));
        Assert.Throws<ArgumentException>(() => AtomicFile.Delete(""));
    }
}
