// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BindgenCSharp.Data;

namespace C2CS.Feature.BindgenCSharp.Domain.Logic.Mapper;

public sealed class CSharpMapperParameters
{
    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

    public ImmutableArray<string> IgnoredTypeNames { get; }

    public DiagnosticsSink DiagnosticsSink { get; }

    public ImmutableDictionary<string, string> SystemTypeNameAliases { get; }

    public CSharpMapperParameters(
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        DiagnosticsSink diagnostics)
    {
        TypeAliases = typeAliases;
        IgnoredTypeNames = ignoredTypeNames;
        DiagnosticsSink = diagnostics;
        SystemTypeNameAliases = GetSystemTypeNameAliases().ToImmutableDictionary();
    }

    private static Dictionary<string, string> GetSystemTypeNameAliases()
    {
        var aliases = new Dictionary<string, string>();
        AddSystemTypes(aliases);
        return aliases;
    }

    private static void AddSystemTypes(IDictionary<string, string> aliases)
    {
        aliases.Add("wchar_t", string.Empty); // remove

        AddSystemTypesWindows(aliases);
        AddSystemTypesLinux(aliases);
        AddSystemTypesDarwin(aliases);
    }

    private static void AddSystemTypesDarwin(IDictionary<string, string> aliases)
    {
        aliases.Add("__uint32_t", "uint");
        aliases.Add("__uint16_t", "ushort");
        aliases.Add("__uint8_t", "byte");
        aliases.Add("__int32_t", "int");

        aliases.Add("__darwin_pthread_t", "nint");
        aliases.Add("__darwin_uid_t", "uint");
        aliases.Add("__darwin_pid_t", "int");
        aliases.Add("__darwin_gid_t", "uint");
        aliases.Add("__darwin_socklen_t", "uint");
        aliases.Add("_opaque_pthread_t", string.Empty); // remove
        aliases.Add("__darwin_pthread_handler_rec", string.Empty); // remove
        aliases.Add("__darwin_wchar_t", string.Empty); // remove
        aliases.Add("__darwin_time_t", "nint");
    }

    private static void AddSystemTypesLinux(IDictionary<string, string> aliases)
    {
        aliases.Add("__gid_t", "uint");
        aliases.Add("__uid_t", "uint");
        aliases.Add("__pid_t", "int");
        aliases.Add("__socklen_t", "uint");
        aliases.Add("__time_t", "nint");
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
        aliases.Add("HINSTANCE__", string.Empty); // remove
        aliases.Add("HWND__", string.Empty); // remove
    }
}
