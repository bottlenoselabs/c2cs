// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace C2CS;

/// <summary>
///     The collection of utilities for platform interoperability with native libraries in C#.
/// </summary>
public static class Platform
{
    /// <summary>
    ///     Gets the current <see cref="RuntimeOperatingSystem" />.
    /// </summary>
    public static RuntimeOperatingSystem HostOperatingSystem => GetRuntimeOperatingSystem();

    /// <summary>
    ///     Gets the current <see cref="RuntimeArchitecture" />.
    /// </summary>
    public static RuntimeArchitecture HostArchitecture => GetRuntimeArchitecture();

    /// <summary>
    ///     Gets the library file name extension given a <see cref="RuntimeOperatingSystem" />.
    /// </summary>
    /// <param name="operatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name extension for the <paramref name="operatingSystem" />
    ///     .
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="operatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNameExtension(RuntimeOperatingSystem operatingSystem)
    {
        return operatingSystem switch
        {
            RuntimeOperatingSystem.Windows => ".dll",
            RuntimeOperatingSystem.Xbox => ".dll",
            RuntimeOperatingSystem.macOS => ".dylib",
            RuntimeOperatingSystem.tvOS => ".dylib",
            RuntimeOperatingSystem.iOS => ".dylib",
            RuntimeOperatingSystem.Linux => ".so",
            RuntimeOperatingSystem.FreeBSD => ".so",
            RuntimeOperatingSystem.Android => ".so",
            RuntimeOperatingSystem.PlayStation => ".so",
            RuntimeOperatingSystem.Browser => throw new NotImplementedException(),
            RuntimeOperatingSystem.Switch => throw new NotImplementedException(),
            RuntimeOperatingSystem.Unknown => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null)
        };
    }

    /// <summary>
    ///     Gets the library file name prefix for a <see cref="RuntimeOperatingSystem" />.
    /// </summary>
    /// <param name="targetOperatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name prefix for the
    ///     <paramref name="targetOperatingSystem" />.
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="targetOperatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetOperatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNamePrefix(RuntimeOperatingSystem targetOperatingSystem)
    {
        switch (targetOperatingSystem)
        {
            case RuntimeOperatingSystem.Windows:
            case RuntimeOperatingSystem.Xbox:
                return string.Empty;
            case RuntimeOperatingSystem.macOS:
            case RuntimeOperatingSystem.tvOS:
            case RuntimeOperatingSystem.iOS:
            case RuntimeOperatingSystem.Linux:
            case RuntimeOperatingSystem.FreeBSD:
            case RuntimeOperatingSystem.Android:
            case RuntimeOperatingSystem.PlayStation:
                return "lib";
            case RuntimeOperatingSystem.Browser:
            case RuntimeOperatingSystem.Switch:
            case RuntimeOperatingSystem.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(targetOperatingSystem), targetOperatingSystem, null);
        }
    }

    private static RuntimeArchitecture GetRuntimeArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.Arm64 => RuntimeArchitecture.ARM64,
            Architecture.Arm => RuntimeArchitecture.ARM32,
            Architecture.X86 => RuntimeArchitecture.X86,
            Architecture.X64 => RuntimeArchitecture.X64,
            Architecture.Wasm => RuntimeArchitecture.Unknown,
            Architecture.S390x => RuntimeArchitecture.Unknown,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private static RuntimeOperatingSystem GetRuntimeOperatingSystem()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeOperatingSystem.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeOperatingSystem.macOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return RuntimeOperatingSystem.Linux;
        }

        if (OperatingSystem.IsAndroid())
        {
            return RuntimeOperatingSystem.Android;
        }

        if (OperatingSystem.IsIOS())
        {
            return RuntimeOperatingSystem.iOS;
        }

        if (OperatingSystem.IsTvOS())
        {
            return RuntimeOperatingSystem.tvOS;
        }

        if (OperatingSystem.IsBrowser())
        {
            return RuntimeOperatingSystem.Browser;
        }

        return RuntimeOperatingSystem.Unknown;
    }
}
