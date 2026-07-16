namespace B44.Common.Persistence;

/// <summary>
/// Minimal load/save/clear contract for a single persisted document of type
/// <typeparamref name="T"/> (a save file, campaign progress, settings...).
/// </summary>
public interface IRepository<T>
    where T : class
{
    /// <summary>Loads the saved data, or null when nothing has been saved yet.</summary>
    T? Load();

    void Save(T data);

    /// <summary>Deletes the saved data (e.g. "New Journey").</summary>
    void Clear();
}
