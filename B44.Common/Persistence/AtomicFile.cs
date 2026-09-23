using System;
using System.IO;
using System.Text;

namespace B44.Common.Persistence;

/// <summary>
/// Durable, atomic whole-file writes with last-good recovery, for any payload:
/// bytes, UTF-8 text, or a format only the caller can parse. A write lands in a
/// sibling <c>.tmp</c> file, is flushed to disk, and only then replaces the
/// target, rotating the previous good file to <c>.bak</c>. A read falls back to
/// that backup when the main file is missing, empty, or does not parse.
/// <see cref="AtomicJsonFileStore{T}"/> is the typed JSON layer over this.
/// </summary>
/// <remarks>
/// Every storage failure surfaces as a <see cref="StoreException"/>; nothing
/// returns quietly. Engine-free: callers resolve engine virtual paths (such as
/// Godot's <c>user://</c>) to OS paths before calling.
/// </remarks>
public static class AtomicFile
{
    // Written without a byte-order mark; read with BOM detection and a UTF-8
    // default, matching File.ReadAllText.
    private static readonly UTF8Encoding WriteEncoding = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Durably replaces <paramref name="path"/> with <paramref name="contents"/>.
    /// The previous file, if any, is kept as <c>.bak</c>. On failure the previous
    /// file is untouched and no temporary file is left behind.
    /// </summary>
    /// <exception cref="StoreException">The file could not be written.</exception>
    public static void Write(string path, ReadOnlySpan<byte> contents)
    {
        RequirePath(path);
        string tempPath = TempPath(path);
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(contents);
                // Force the OS write cache to disk BEFORE the swap, so a power cut
                // can never promote a partially persisted temp file to the final path.
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(path))
            {
                // Atomic swap that also rotates the previous good file to .bak.
                File.Replace(tempPath, path, BackupPath(path), ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, path);
            }
        }
        catch (Exception ex)
        {
            DeleteAbandoned(tempPath);
            throw new StoreException("Failed to save the file.", ex);
        }
    }

    /// <summary>Durably replaces <paramref name="path"/> with UTF-8 text, written without a byte-order mark.</summary>
    /// <exception cref="StoreException">The text could not be encoded or the file could not be written.</exception>
    public static void WriteText(string path, string contents)
    {
        string text = contents ?? throw new ArgumentNullException(nameof(contents));
        byte[] bytes;
        try
        {
            bytes = WriteEncoding.GetBytes(text);
        }
        catch (EncoderFallbackException ex)
        {
            throw new StoreException("Failed to save the file.", ex);
        }

        Write(path, bytes);
    }

    /// <summary>
    /// Reads <paramref name="path"/> through <paramref name="parse"/>. When the
    /// main file is missing, empty, fails to parse, or parses to <c>null</c>, the
    /// previous good file is tried instead. A main file that parses always wins,
    /// so a document the caller later rejects is never silently replaced by an
    /// older one.
    /// </summary>
    /// <returns>The document, or <c>null</c> when neither file holds one.</returns>
    /// <exception cref="StoreException">The main file exists but cannot be read or parsed, and the backup cannot either.</exception>
    public static T? Read<T>(string path, Func<byte[], T?> parse)
        where T : class
    {
        RequirePath(path);
        Func<byte[], T?> parser = parse ?? throw new ArgumentNullException(nameof(parse));
        try
        {
            T? main = ReadDocument(path, parser);
            if (main is not null)
            {
                return main;
            }
        }
        catch (Exception ex) when (ex is not StoreException)
        {
            // The main file is corrupt; the backup is the last good copy.
            T? recovered = TryReadBackup(path, parser);
            if (recovered is not null)
            {
                return recovered;
            }

            throw new StoreException("Failed to load the save file.", ex);
        }

        // Main file cleanly absent or empty. A torn write can leave that state
        // too, so a surviving backup still counts as the document.
        return TryReadBackup(path, parser);
    }

    /// <summary>
    /// Reads <paramref name="path"/> as text through <paramref name="parse"/>,
    /// with the same backup recovery as <see cref="Read{T}"/>. Decoding detects a
    /// byte-order mark and otherwise assumes UTF-8, as File.ReadAllText does.
    /// </summary>
    /// <exception cref="StoreException">The main file exists but cannot be read or parsed, and the backup cannot either.</exception>
    public static T? ReadText<T>(string path, Func<string, T?> parse)
        where T : class
    {
        Func<string, T?> parser = parse ?? throw new ArgumentNullException(nameof(parse));
        return Read(path, bytes => parser(Decode(bytes)));
    }

    /// <summary>Deletes the file, its backup, and any abandoned temporary file.</summary>
    /// <exception cref="StoreException">A file could not be deleted.</exception>
    public static void Delete(string path)
    {
        RequirePath(path);
        try
        {
            DeleteIfExists(path);
            DeleteIfExists(BackupPath(path));
            DeleteIfExists(TempPath(path));
        }
        catch (Exception ex)
        {
            throw new StoreException("Failed to delete the save file.", ex);
        }
    }

    private static string BackupPath(string path) => path + ".bak";

    private static string TempPath(string path) => path + ".tmp";

    private static void RequirePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Save path must not be null or empty.", nameof(path));
        }
    }

    private static T? ReadDocument<T>(string path, Func<byte[], T?> parse)
        where T : class
    {
        if (!File.Exists(path))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);
        return bytes.Length == 0 ? null : parse(bytes);
    }

    private static T? TryReadBackup<T>(string path, Func<byte[], T?> parse)
        where T : class
    {
        try
        {
            return ReadDocument(BackupPath(path), parse);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Decode(byte[] bytes)
    {
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void DeleteAbandoned(string tempPath)
    {
        try
        {
            DeleteIfExists(tempPath);
        }
        catch (Exception)
        {
            // The original failure is the one worth reporting; a stale temp file
            // is overwritten by the next write.
        }
    }
}
