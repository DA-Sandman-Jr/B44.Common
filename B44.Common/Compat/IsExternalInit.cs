#if !NET5_0_OR_GREATER

// IDE0130 is suppressed for this file alone, and cannot be repaired by moving it:
// the compiler binds init-only setters to this exact namespace and type name, so
// the folder convention has to give way rather than the declaration.
#pragma warning disable IDE0130

namespace System.Runtime.CompilerServices;

/// <summary>
/// Marker the C# compiler requires to emit <c>init</c>-only setters, and
/// therefore records. .NET 5 and later ship it in the runtime;
/// .NET Standard 2.1 does not, so the declaration has to be supplied locally.
/// </summary>
internal static class IsExternalInit
{
}

#endif
