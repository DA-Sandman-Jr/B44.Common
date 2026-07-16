using System;
using System.IO;
using System.Text.Json;

namespace B44.Common.Persistence;

/// <summary>
/// File-backed repository with atomic writes (write-then-rename) and JSON
/// serialization. Engine-free: callers resolve the save path themselves —
/// <see cref="ResolveAppDataPath"/> covers the common per-user app-data case.
/// No save backwards-compatibility by B44 doctrine: unreadable saves throw
/// <see cref="StoreException"/> and <see cref="RepositoryFactory"/> resets
/// them rather than migrating.
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
            if (!File.Exists(_savePath))
            {
                return null;
            }

            string json = File.ReadAllText(_savePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex) when (ex is not StoreException)
        {
            throw new StoreException("Failed to load the save file.", ex);
        }
    }

    public void Save(T data)
    {
        try
        {
            string json = JsonSerializer.Serialize(data, _jsonOptions);
            string tempPath = _savePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, _savePath, overwrite: true);
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
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
        }
        catch (Exception ex)
        {
            throw new StoreException("Failed to delete the save file.", ex);
        }
    }
}
