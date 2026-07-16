namespace B44.Common.Persistence;

/// <summary>
/// Fallback repository used when a file-backed store cannot initialize
/// (e.g. sandboxed platform with no writable app-data dir). Progress lasts
/// for the session only. Also handy as a test double.
/// </summary>
public sealed class InMemoryRepository<T> : IRepository<T>
    where T : class
{
    private T? _data;

    public T? Load() => _data;

    public void Save(T data) => _data = data;

    public void Clear() => _data = null;
}
