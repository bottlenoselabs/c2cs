// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.Mapper;

public sealed class CSharpCodeMapperOptions
{
    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; init; }

    public ImmutableArray<string> IgnoredNames { get; init; }

    public ImmutableDictionary<string, string> SystemTypeAliases { get; }

    public CSharpCodeMapperOptions()
    {
        SystemTypeAliases = GetSystemTypeNameAliases().ToImmutableDictionary();
    }

    private static Dictionary<string, string> GetSystemTypeNameAliases()
    {
        var aliases = new Dictionary<string, string>();
        AddSystemTypes(aliases);
        return aliases;
    }

    private static void AddSystemTypes(IDictionary<string, string> aliases)
    {
        AddSystemTypesWindows(aliases);
        // AddSystemTypesLinux(aliases);
        AddSystemTypesDarwin(aliases);
    }

    private static void AddSystemTypesDarwin(IDictionary<string, string> aliases)
    {
        aliases.Add("UInt8", "byte");
        aliases.Add("SInt8", "sbyte");
        aliases.Add("UInt16", "ushort");
        aliases.Add("SInt16", "short");
        aliases.Add("UInt32", "uint");
        aliases.Add("SInt32", "int");
        aliases.Add("UInt64", "ulong");
        aliases.Add("SInt64", "long");
        aliases.Add("Boolean", "CBool");
    }

    private static void AddSystemTypesWindows(IDictionary<string, string> aliases)
    {
        aliases.Add("BOOL", "int"); // A int boolean
        aliases.Add("BOOLEAN", "CBool"); // A byte boolean
        aliases.Add("BYTE", "byte"); // An unsigned char (8-bits)
        aliases.Add("CCHAR", "byte"); // An 8-bit ANSI char
        aliases.Add("CHAR", "byte"); // An 8-bit ANSI char

        // Unsigned integers
        aliases.Add("UINT8", "byte");
        aliases.Add("UINT16", "ushort");
        aliases.Add("UINT32", "uint");
        aliases.Add("UINT64", "ulong");
        aliases.Add("DWORD", "uint");
        aliases.Add("ULONG", "uint");
        aliases.Add("UINT", "uint");
        aliases.Add("ULONGLONG", "ulong");

        // Signed integers
        aliases.Add("INT8", "sbyte");
        aliases.Add("INT16", "short");
        aliases.Add("INT32", "int");
        aliases.Add("INT64", "long");
        aliases.Add("LONG", "int");
        aliases.Add("INT", "int");
        aliases.Add("LONGLONG", "long");

        // 32 bits on 32-bit machine, 64-bits on 64-bit machine
        aliases.Add("LONG_PTR", "nint");
        aliases.Add("ULONG_PTR", "nint");
        aliases.Add("UINT_PTR", "nint");
        aliases.Add("INT_PTR", "nint");

        // Parameters
        aliases.Add("LPARAM", "nint"); // A message parameter (LONG_PTR)
        aliases.Add("WPARAM", "nint"); // A message parameter (UINT_PTR)

        // Pointers
        aliases.Add("LPVOID", "nint"); // A pointer to any type
        aliases.Add("LPINT", "nint"); // A pointer to an INT

        // Handles
        aliases.Add("HANDLE", "nint"); // A handle to an object
        aliases.Add("HINSTANCE", "nint"); // A handle to an instance
        aliases.Add("HWND", "nint"); // A handle to a window
        aliases.Add("SOCKET", "nint"); // A handle to a socket
    }
}
