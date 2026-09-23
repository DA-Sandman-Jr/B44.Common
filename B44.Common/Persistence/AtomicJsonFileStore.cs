using System;
using System.IO;
using System.Text.Json;

namespace B44.Common.Persistence;

/// <summary>
/// File-backed repository with durable atomic writes and JSON serialization.
/// Saves flush to disk before a write-then-rename swap, and the previous good
/// save is kept as <c>.bak</c>; <see cref="Load"/> falls back to that backup
/// when the main file is missing, torn, or corrupt. The file mechanics are
/// <see cref="AtomicFile"/>'s; this adds the typed JSON layer. Engine-free:
/// callers resolve the save path themselves — <see cref="SavePaths.ResolveAppData"/>
/// covers the common per-user app-data case.
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

    public T? Load()
    {
        // Whitespace counts as no document, so a blank main file falls through to the backup.
        return AtomicFile.ReadText(
            _savePath,
            json => string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<T>(json, _jsonOptions));
    }

    public void Save(T data)
    {
        byte[] json;
        try
        {
            json = JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions);
        }
        catch (Exception ex)
        {
            throw new StoreException("Failed to save the file.", ex);
        }

        AtomicFile.Write(_savePath, json);
    }

    public void Clear() => AtomicFile.Delete(_savePath);
}
