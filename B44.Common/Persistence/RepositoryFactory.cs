using System;

namespace B44.Common.Persistence;

/// <summary>
/// Builds the best available repository: file-backed when possible, in-memory
/// otherwise. Godot callers pass <c>GD.PushWarning</c> as the warning sink;
/// this package never references the engine.
/// </summary>
public static class RepositoryFactory
{
    public static IRepository<T> CreateWithFallback<T>(
        Func<IRepository<T>> createFileStore,
        Action<string>? onWarning = null)
        where T : class
    {
        IRepository<T> store;
        try
        {
            store = createFileStore();
        }
        catch (Exception ex)
        {
            onWarning?.Invoke(
                $"Save storage unavailable ({ex.Message}); progress will not persist this session.");
            return new InMemoryRepository<T>();
        }

        // Probe the load path now so an unreadable save surfaces here. Old or
        // corrupt formats are deleted, not migrated — the game restarts fresh
        // but stays file-backed.
        try
        {
            store.Load();
        }
        catch (StoreException ex)
        {
            onWarning?.Invoke(
                $"Save file was unreadable and has been reset ({ex.InnerException?.Message ?? ex.Message}).");
            try
            {
                store.Clear();
            }
            catch (StoreException)
            {
                onWarning?.Invoke("Save file could not be reset; progress will not persist this session.");
                return new InMemoryRepository<T>();
            }
        }

        return store;
    }
}
