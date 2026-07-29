using System;

namespace B44.Common.Persistence;

/// <summary>
/// What <see cref="RepositoryFactory.CreateWithFallback"/> does when a store is
/// reachable but its contents cannot be read (old format, corruption, a partial
/// write no <c>.bak</c> could recover).
/// </summary>
/// <remarks>
/// There is deliberately no default. Discarding a player's save is a decision
/// each game makes for itself, and a shared factory must not make it silently on
/// the game's behalf — the call site has to say which one it wants.
/// </remarks>
public enum UnreadableSavePolicy
{
    /// <summary>
    /// Leave the unreadable data exactly where it is and run this session on an
    /// in-memory store. Nothing is deleted, so the file survives for inspection
    /// or a later migration, but progress made this session will not persist.
    /// </summary>
    Preserve,

    /// <summary>
    /// Delete the unreadable data and continue file-backed, so the game starts
    /// fresh but still saves. Appropriate while a game is pre-release and its
    /// saves are not yet a compatibility surface; revisit at 1.0, when a
    /// released save becomes player data that a version bump must not destroy.
    /// </summary>
    Reset,
}

/// <summary>
/// Builds the best available repository: file-backed when possible, in-memory
/// otherwise. Godot callers pass <c>GD.PushWarning</c> as the warning sink;
/// this package never references the engine.
/// </summary>
public static class RepositoryFactory
{
    /// <summary>
    /// Creates the file store, probes its load path so unreadable data surfaces
    /// here rather than mid-session, and applies
    /// <paramref name="unreadableSavePolicy"/> if it does. Falls back to an
    /// in-memory store when the file store cannot be created at all, or when a
    /// requested reset fails.
    /// </summary>
    public static IRepository<T> CreateWithFallback<T>(
        Func<IRepository<T>> createFileStore,
        UnreadableSavePolicy unreadableSavePolicy,
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

        // Probe the load path now so an unreadable save surfaces here rather
        // than at the first mid-session load.
        try
        {
            store.Load();
        }
        catch (StoreException ex)
        {
            string cause = ex.InnerException?.Message ?? ex.Message;
            if (unreadableSavePolicy == UnreadableSavePolicy.Preserve)
            {
                onWarning?.Invoke(
                    $"Save file was unreadable and has been left untouched ({cause}); progress will not persist this session.");
                return new InMemoryRepository<T>();
            }

            onWarning?.Invoke($"Save file was unreadable and has been reset ({cause}).");
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
