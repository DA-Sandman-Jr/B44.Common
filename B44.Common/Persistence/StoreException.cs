using System;

namespace B44.Common.Persistence;

/// <summary>Wraps any I/O or serialization failure raised by a file-backed store.</summary>
public sealed class StoreException : Exception
{
    public StoreException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
