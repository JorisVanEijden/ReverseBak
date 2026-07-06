namespace System.Runtime.CompilerServices;

/// <summary>
/// Polyfill required for C# 9 `init` accessors (and `readonly record struct` positional members,
/// which the compiler lowers to `init`-only properties) on netstandard2.1, which predates this
/// BCL type. Compiler-recognized by name/namespace only; no members needed.
/// </summary>
internal static class IsExternalInit { }
