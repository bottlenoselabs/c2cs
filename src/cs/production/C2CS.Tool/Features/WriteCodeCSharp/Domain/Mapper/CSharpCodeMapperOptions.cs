// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using C2CS.Features.WriteCodeCSharp.Data;

namespace C2CS.Features.WriteCodeCSharp.Domain.Mapper;

public sealed class CSharpCodeMapperOptions
{
    public CSharpCodeMapperOptions()
    {
        SystemTypeAliases = GetSystemTypeNameAliases().ToImmutableDictionary();
    }

    public ImmutableArray<CSharpTypeRename> TypeRenames { get; init; }

    public ImmutableArray<string> IgnoredNames { get; init; }

    public ImmutableDictionary<string, string> SystemTypeAliases { get; }

    public bool IsEnabledLibraryImportAttribute { get; init; }

    private static Dictionary<string, string> GetSystemTypeNameAliases()
    {
        var aliases = new Dictionary<string, string>();
        AddSystemTypes(aliases);
        return aliases;
    }

    private static void AddSystemTypes(IDictionary<string, string> renames)
    {
        AddSystemTypesWindows(renames);
        // AddSystemTypesLinux(aliases);
        AddSystemTypesDarwin(renames);
    }

    private static void AddSystemTypesDarwin(IDictionary<string, string> renames)
    {
        renames.Add("UInt8", "byte");
        renames.Add("SInt8", "sbyte");
        renames.Add("UInt16", "ushort");
        renames.Add("SInt16", "short");
        renames.Add("UInt32", "uint");
        renames.Add("SInt32", "int");
        renames.Add("UInt64", "ulong");
        renames.Add("SInt64", "long");
        renames.Add("Boolean", "CBool");
    }

    private static void AddSystemTypesWindows(IDictionary<string, string> renames)
    {
        renames.Add("BOOL", "int"); // A int boolean
        renames.Add("BOOLEAN", "CBool"); // A byte boolean
        renames.Add("BYTE", "byte"); // An unsigned char (8-bits)
        renames.Add("CCHAR", "byte"); // An 8-bit ANSI char
        renames.Add("CHAR", "byte"); // An 8-bit ANSI char

        // Unsigned integers
        renames.Add("UINT8", "byte");
        renames.Add("UINT16", "ushort");
        renames.Add("UINT32", "uint");
        renames.Add("UINT64", "ulong");
        renames.Add("DWORD", "uint");
        renames.Add("ULONG", "uint");
        renames.Add("UINT", "uint");
        renames.Add("ULONGLONG", "ulong");

        // Signed integers
        renames.Add("INT8", "sbyte");
        renames.Add("INT16", "short");
        renames.Add("INT32", "int");
        renames.Add("INT64", "long");
        renames.Add("LONG", "int");
        renames.Add("INT", "int");
        renames.Add("LONGLONG", "long");

        // 32 bits on 32-bit machine, 64-bits on 64-bit machine
        renames.Add("LONG_PTR", "nint");
        renames.Add("ULONG_PTR", "nint");
        renames.Add("UINT_PTR", "nint");
        renames.Add("INT_PTR", "nint");

        // Parameters
        renames.Add("LPARAM", "nint"); // A message parameter (LONG_PTR)
        renames.Add("WPARAM", "nint"); // A message parameter (UINT_PTR)

        // Pointers
        renames.Add("LPVOID", "nint"); // A pointer to any type
        renames.Add("LPINT", "nint"); // A pointer to an INT

        // Handles
        renames.Add("HANDLE", "nint"); // A handle to an object
        renames.Add("HINSTANCE", "nint"); // A handle to an instance
        renames.Add("HWND", "nint"); // A handle to a window
        renames.Add("SOCKET", "nint"); // A handle to a socket
    }
}
