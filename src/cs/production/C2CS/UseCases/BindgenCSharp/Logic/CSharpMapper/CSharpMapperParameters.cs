// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace C2CS.UseCases.BindgenCSharp;

public sealed class CSharpMapperParameters
{
    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

    public ImmutableArray<string> IgnoredTypeNames { get; }

    public int Bitness { get; }

    public DiagnosticsSink DiagnosticsSink { get; }

    public ImmutableDictionary<string, string> SystemTypeNameAliases { get; }

    public CSharpMapperParameters(
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        int bitness,
        DiagnosticsSink diagnostics)
    {
        TypeAliases = typeAliases;
        IgnoredTypeNames = ignoredTypeNames;
        Bitness = bitness is 32 or 64 ? bitness : throw new NotImplementedException($"{bitness}-bit is not implemented.");
        DiagnosticsSink = diagnostics;
        SystemTypeNameAliases = GetSystemTypeNameAliases(bitness).ToImmutableDictionary();
    }

    private static Dictionary<string, string> GetSystemTypeNameAliases(int bitness)
    {
        var aliases = new Dictionary<string, string>();
        var operatingSystem = Platform.OperatingSystem;
        AddSystemTypes(bitness, operatingSystem, aliases);
        return aliases;
    }

    private static void AddSystemTypes(
        int bitness,
        RuntimeOperatingSystem operatingSystem,
        Dictionary<string, string> aliases)
    {
        aliases.Add("wchar_t", string.Empty); // remove

        switch (operatingSystem)
        {
            case RuntimeOperatingSystem.Windows:
                AddSystemTypesWindows(aliases);
                break;
            case RuntimeOperatingSystem.Linux:
                AddSystemTypesLinux(bitness, aliases);
                break;
            case RuntimeOperatingSystem.macOS:
            case RuntimeOperatingSystem.iOS:
            case RuntimeOperatingSystem.tvOS:
                AddSystemTypesDarwin(bitness, aliases);
                break;
            case RuntimeOperatingSystem.Unknown:
                throw new PlatformNotSupportedException();
            case RuntimeOperatingSystem.FreeBSD:
            case RuntimeOperatingSystem.Android:
            case RuntimeOperatingSystem.Browser:
            case RuntimeOperatingSystem.PlayStation:
            case RuntimeOperatingSystem.Xbox:
            case RuntimeOperatingSystem.Switch:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null);
        }
    }

    private static void AddSystemTypesDarwin(int bitness, Dictionary<string, string> aliases)
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

        switch (bitness)
        {
            case 32:
                aliases.Add("__darwin_time_t", "int");
                break;
            case 64:
                aliases.Add("__darwin_time_t", "long");
                break;
        }
    }

    private static void AddSystemTypesLinux(int bitness, Dictionary<string, string> aliases)
    {
        aliases.Add("__gid_t", "uint");
        aliases.Add("__uid_t", "uint");
        aliases.Add("__pid_t", "int");
        aliases.Add("__socklen_t", "uint");

        switch (bitness)
        {
            case 32:
                aliases.Add("__time_t", "int");
                break;
            case 64:
                aliases.Add("__time_t", "long");
                break;
        }
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
