// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace C2CS;

public static class Platform
{
    public static TargetOperatingSystem OperatingSystem
    {
        get
        {
            if (System.OperatingSystem.IsWindows())
            {
                return TargetOperatingSystem.Windows;
            }

            if (System.OperatingSystem.IsMacOS())
            {
                return TargetOperatingSystem.macOS;
            }

            if (System.OperatingSystem.IsLinux())
            {
                return TargetOperatingSystem.Linux;
            }

            if (System.OperatingSystem.IsAndroid())
            {
                return TargetOperatingSystem.Android;
            }

            if (System.OperatingSystem.IsIOS())
            {
                return TargetOperatingSystem.iOS;
            }

            if (System.OperatingSystem.IsTvOS())
            {
                return TargetOperatingSystem.tvOS;
            }

            if (System.OperatingSystem.IsBrowser())
            {
                return TargetOperatingSystem.Browser;
            }

            return TargetOperatingSystem.Unknown;
        }
    }

    public static TargetArchitecture Architecture
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => TargetArchitecture.ARM64,
                System.Runtime.InteropServices.Architecture.Arm => TargetArchitecture.ARM32,
                System.Runtime.InteropServices.Architecture.X86 => TargetArchitecture.X86,
                System.Runtime.InteropServices.Architecture.X64 => TargetArchitecture.X64,
                System.Runtime.InteropServices.Architecture.Wasm => TargetArchitecture.WASM32,
                System.Runtime.InteropServices.Architecture.S390x => TargetArchitecture.Unknown,
                _ => TargetArchitecture.Unknown
            };
        }
    }

    public static TargetPlatform Target
    {
        get
        {
            var operatingSystem = OperatingSystem;
            var architecture = Architecture;

            return operatingSystem switch
            {
                TargetOperatingSystem.Windows when architecture == TargetArchitecture.X64 => TargetPlatform
                    .x86_64_pc_windows,
                TargetOperatingSystem.Windows when architecture == TargetArchitecture.X86 => TargetPlatform
                    .i686_pc_windows,
                TargetOperatingSystem.Windows when architecture == TargetArchitecture.ARM64 => TargetPlatform
                    .aarch64_pc_windows,
                TargetOperatingSystem.macOS when architecture == TargetArchitecture.ARM64 => TargetPlatform
                    .aarch64_apple_ios,
                TargetOperatingSystem.macOS when architecture == TargetArchitecture.X64 => TargetPlatform
                    .x86_64_apple_darwin,
                TargetOperatingSystem.Linux when architecture == TargetArchitecture.X64 => TargetPlatform
                    .x86_64_unknown_linux_gnu,
                TargetOperatingSystem.Linux when architecture == TargetArchitecture.X86 => TargetPlatform
                    .i686_unknown_linux_gnu,
                TargetOperatingSystem.Linux when architecture == TargetArchitecture.ARM64 => TargetPlatform
                    .aarch64_unknown_linux_gnu,
                _ => throw new InvalidOperationException("Unknown platform host.")
            };
        }
    }

    /// <summary>
    ///     Gets the library file name extension given a <see cref="TargetOperatingSystem" />.
    /// </summary>
    /// <param name="operatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name extension for the
    ///     <paramref name="operatingSystem" />.
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="operatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNameExtension(TargetOperatingSystem operatingSystem)
    {
        return operatingSystem switch
        {
            TargetOperatingSystem.Windows => ".dll",
            TargetOperatingSystem.Xbox => ".dll",
            TargetOperatingSystem.macOS => ".dylib",
            TargetOperatingSystem.tvOS => ".dylib",
            TargetOperatingSystem.iOS => ".dylib",
            TargetOperatingSystem.Linux => ".so",
            TargetOperatingSystem.FreeBSD => ".so",
            TargetOperatingSystem.Android => ".so",
            TargetOperatingSystem.PlayStation => ".so",
            TargetOperatingSystem.Browser => throw new NotImplementedException(),
            TargetOperatingSystem.Switch => throw new NotImplementedException(),
            TargetOperatingSystem.DualScreen3D => throw new NotImplementedException(),
            TargetOperatingSystem.Unknown => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null)
        };
    }

    /// <summary>
    ///     Gets the library file name prefix for a <see cref="TargetOperatingSystem" />.
    /// </summary>
    /// <param name="targetOperatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name prefix for the
    ///     <paramref name="targetOperatingSystem" />.
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="targetOperatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetOperatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNamePrefix(TargetOperatingSystem targetOperatingSystem)
    {
        switch (targetOperatingSystem)
        {
            case TargetOperatingSystem.Windows:
            case TargetOperatingSystem.Xbox:
                return string.Empty;
            case TargetOperatingSystem.macOS:
            case TargetOperatingSystem.tvOS:
            case TargetOperatingSystem.iOS:
            case TargetOperatingSystem.Linux:
            case TargetOperatingSystem.FreeBSD:
            case TargetOperatingSystem.Android:
            case TargetOperatingSystem.PlayStation:
                return "lib";
            case TargetOperatingSystem.Browser:
            case TargetOperatingSystem.Switch:
            case TargetOperatingSystem.DualScreen3D:
            case TargetOperatingSystem.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(targetOperatingSystem), targetOperatingSystem, null);
        }
    }
}
