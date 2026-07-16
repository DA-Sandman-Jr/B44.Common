using System;
using System.IO;
using System.Text.Json;

namespace B44.Common.Persistence;

/// <summary>
/// File-backed repository with durable atomic writes and JSON serialization.
/// Saves flush to disk before a write-then-rename swap, and the previous good
/// save is kept as <c>.bak</c>; <see cref="Load"/> falls back to that backup
/// when the main file is missing, torn, or corrupt. Engine-free: callers
/// resolve the save path themselves — <see cref="ResolveAppDataPath"/> covers
/// the common per-user app-data case.
/// Format policy is the caller's: pre-release, B44 games reset unreadable
/// saves via <see cref="RepositoryFactory"/>; released games layer a
/// versioned envelope + migrations on top. The store stays format-agnostic.
/// </summary>
public sealed class AtomicJsonFileStore<T> : IRepository<T>
    where T : class
{
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _savePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AtomicJsonFileStore(string savePath, JsonSerializerOptions? jsonOptions = null)
    {
        if (string.IsNullOrWhiteSpace(savePath))
        {
            throw new ArgumentException("Save path must not be null or empty.", nameof(savePath));
        }

        string? saveDirectory = Path.GetDirectoryName(savePath);
        if (!string.IsNullOrWhiteSpace(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        _savePath = savePath;
        _jsonOptions = jsonOptions ?? DefaultJsonOptions;
    }

    private string BackupPath => _savePath + ".bak";

    private string TempPath => _savePath + ".tmp";

    /// <summary>
    /// Resolves a save path under the per-user application-data directory
    /// (e.g. <c>%APPDATA%/MyGame/save.json</c>), creating the directory.
    /// </summary>
    public static string ResolveAppDataPath(string appFolderName, string fileName)
    {
        string dataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(dataDir))
        {
            throw new StoreException(
                "Unable to resolve the application data directory for the save file.",
                new InvalidOperationException("Application data path was null or empty."));
        }

        string saveDirectory = Path.Combine(dataDir, appFolderName);
        Directory.CreateDirectory(saveDirectory);
        return Path.Combine(saveDirectory, fileName);
    }

    public T? Load()
    {
        try
        {
            T? main = ReadDocument(_savePath);
            if (main is not null)
            {
                return main;
            }
        }
        catch (Exception ex) when (ex is not StoreException)
        {
            // Main file is corrupt — the backup is the last good save.
            T? recovered = TryReadBackup();
            if (recovered is not null)
            {
                return recovered;
            }

            throw new StoreException("Failed to load the save file.", ex);
        }

        // Main file cleanly absent or empty. A torn write can leave that
        // state too, so a surviving backup still counts as the save.
        return TryReadBackup();
    }

    public void Save(T data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            using (FileStream stream = new(TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new(stream))
            {
                writer.Write(json);
                writer.Flush();
                // Force the OS write cache to disk BEFORE the rename, so a
                // power cut can never promote a partially-persisted temp
                // file to the final path.
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_savePath))
            {
                // Atomic swap that also rotates the previous good save to .bak.
                File.Replace(TempPath, _savePath, BackupPath, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(TempPath, _savePath);
            }
        }
        catch (Exception ex)
        {
            throw new StoreException("Failed to save the file.", ex);
        }
    }

    public void Clear()
    {
        try
        {
            DeleteIfExists(_savePath);
            DeleteIfExists(BackupPath);
            DeleteIfExists(TempPath);
        }
        catch (Exception ex)
        {
            throw new StoreException("Failed to delete the save file.", ex);
        }
    }

    private T? ReadDocument(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        string json = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private T? TryReadBackup()
    {
        try
        {
            return ReadDocument(BackupPath);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
