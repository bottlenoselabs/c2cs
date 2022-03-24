// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace C2CS;

public partial record struct RuntimePlatform
{
    /// <summary>
    ///     Gets the current <see cref="RuntimePlatform" />.
    /// </summary>
    public static RuntimePlatform Host => new(HostRuntimeOperatingSystem(), HostRuntimeArchitecture());

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

    private static RuntimeOperatingSystem HostRuntimeOperatingSystem()
    {
        if (System.OperatingSystem.IsWindows())
        {
            return RuntimeOperatingSystem.Windows;
        }

        if (System.OperatingSystem.IsMacOS())
        {
            return RuntimeOperatingSystem.macOS;
        }

        if (System.OperatingSystem.IsLinux())
        {
            return RuntimeOperatingSystem.Linux;
        }

        if (System.OperatingSystem.IsAndroid())
        {
            return RuntimeOperatingSystem.Android;
        }

        if (System.OperatingSystem.IsIOS())
        {
            return RuntimeOperatingSystem.iOS;
        }

        if (System.OperatingSystem.IsTvOS())
        {
            return RuntimeOperatingSystem.tvOS;
        }

        if (System.OperatingSystem.IsBrowser())
        {
            return RuntimeOperatingSystem.Browser;
        }

        return RuntimeOperatingSystem.Unknown;
    }

    private static RuntimeArchitecture HostRuntimeArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => RuntimeArchitecture.ARM64,
            System.Runtime.InteropServices.Architecture.Arm => RuntimeArchitecture.ARM32,
            System.Runtime.InteropServices.Architecture.X86 => RuntimeArchitecture.X86,
            System.Runtime.InteropServices.Architecture.X64 => RuntimeArchitecture.X64,
            System.Runtime.InteropServices.Architecture.Wasm => RuntimeArchitecture.Unknown,
            System.Runtime.InteropServices.Architecture.S390x => RuntimeArchitecture.Unknown,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static RuntimePlatform FromString(string value)
    {
        return value switch
        {
            "win-x64" => win_x64,
            "osx-x64" => osx_x64,
            "osx-arm64" => osx_arm64,
            "linux-x64" => linux_x64,
            "unknown" => Unknown,
            _ => Unknown
        };
    }

    public static string ToString(RuntimePlatform value)
    {
        if (value == win_x64)
        {
            return "win-x64";
        }

        if (value == osx_x64)
        {
            return "osx-x64";
        }

        if (value == osx_arm64)
        {
            return "osx-arm64";
        }

        if (value == linux_x64)
        {
            return "linux-x64";
        }

        return "unknown";
    }
}
