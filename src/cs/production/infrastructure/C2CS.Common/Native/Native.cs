// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace C2CS;

public static class Native
{
    public static NativeOperatingSystem OperatingSystem
    {
        get
        {
            if (System.OperatingSystem.IsWindows())
            {
                return NativeOperatingSystem.Windows;
            }

            if (System.OperatingSystem.IsMacOS())
            {
                return NativeOperatingSystem.macOS;
            }

            if (System.OperatingSystem.IsLinux())
            {
                return NativeOperatingSystem.Linux;
            }

            if (System.OperatingSystem.IsAndroid())
            {
                return NativeOperatingSystem.Android;
            }

            if (System.OperatingSystem.IsIOS())
            {
                return NativeOperatingSystem.iOS;
            }

            if (System.OperatingSystem.IsTvOS())
            {
                return NativeOperatingSystem.tvOS;
            }

            if (System.OperatingSystem.IsBrowser())
            {
                return NativeOperatingSystem.Browser;
            }

            return NativeOperatingSystem.Unknown;
        }
    }

    public static NativeArchitecture Architecture
    {
        get
        {
            return RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => NativeArchitecture.ARM64,
                System.Runtime.InteropServices.Architecture.Arm => NativeArchitecture.ARM32,
                System.Runtime.InteropServices.Architecture.X86 => NativeArchitecture.X86,
                System.Runtime.InteropServices.Architecture.X64 => NativeArchitecture.X64,
                System.Runtime.InteropServices.Architecture.Wasm => NativeArchitecture.WASM32,
                System.Runtime.InteropServices.Architecture.S390x => NativeArchitecture.Unknown,
                _ => NativeArchitecture.Unknown
            };
        }
    }

    public static TargetPlatform Platform
    {
        get
        {
            var operatingSystem = OperatingSystem;
            var architecture = Architecture;

            return operatingSystem switch
            {
                NativeOperatingSystem.Windows when architecture == NativeArchitecture.X64 => TargetPlatform
                    .x86_64_pc_windows_gnu,
                NativeOperatingSystem.Windows when architecture == NativeArchitecture.X86 => TargetPlatform
                    .i686_pc_windows_gnu,
                NativeOperatingSystem.Windows when architecture == NativeArchitecture.ARM64 => TargetPlatform
                    .aarch64_pc_windows_gnu,
                NativeOperatingSystem.macOS when architecture == NativeArchitecture.ARM64 => TargetPlatform
                    .aarch64_apple_ios,
                NativeOperatingSystem.macOS when architecture == NativeArchitecture.X64 => TargetPlatform
                    .x86_64_apple_darwin,
                NativeOperatingSystem.Linux when architecture == NativeArchitecture.X64 => TargetPlatform
                    .x86_64_unknown_linux_gnu,
                NativeOperatingSystem.Linux when architecture == NativeArchitecture.X86 => TargetPlatform
                    .i686_unknown_linux_gnu,
                NativeOperatingSystem.Linux when architecture == NativeArchitecture.ARM64 => TargetPlatform
                    .aarch64_unknown_linux_gnu,
                _ => throw new InvalidOperationException("Unknown platform host.")
            };
        }
    }

    /// <summary>
    ///     Gets the library file name extension given a <see cref="NativeOperatingSystem" />.
    /// </summary>
    /// <param name="operatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name extension for the
    ///     <paramref name="operatingSystem" />.
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="operatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNameExtension(NativeOperatingSystem operatingSystem)
    {
        return operatingSystem switch
        {
            NativeOperatingSystem.Windows => ".dll",
            NativeOperatingSystem.Xbox => ".dll",
            NativeOperatingSystem.macOS => ".dylib",
            NativeOperatingSystem.tvOS => ".dylib",
            NativeOperatingSystem.iOS => ".dylib",
            NativeOperatingSystem.Linux => ".so",
            NativeOperatingSystem.FreeBSD => ".so",
            NativeOperatingSystem.Android => ".so",
            NativeOperatingSystem.PlayStation4 => ".so",
            NativeOperatingSystem.Browser => throw new NotImplementedException(),
            NativeOperatingSystem.Switch => throw new NotImplementedException(),
            NativeOperatingSystem.DualScreen3D => throw new NotImplementedException(),
            NativeOperatingSystem.Unknown => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null)
        };
    }

    /// <summary>
    ///     Gets the library file name prefix for a <see cref="NativeOperatingSystem" />.
    /// </summary>
    /// <param name="nativeOperatingSystem">The runtime platform.</param>
    /// <returns>
    ///     A <see cref="string" /> containing the library file name prefix for the
    ///     <paramref name="nativeOperatingSystem" />.
    /// </returns>
    /// <exception cref="NotImplementedException"><paramref name="nativeOperatingSystem" /> is not available yet with .NET 5.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nativeOperatingSystem" /> is not a known valid value.</exception>
    public static string LibraryFileNamePrefix(NativeOperatingSystem nativeOperatingSystem)
    {
        switch (nativeOperatingSystem)
        {
            case NativeOperatingSystem.Windows:
            case NativeOperatingSystem.Xbox:
                return string.Empty;
            case NativeOperatingSystem.macOS:
            case NativeOperatingSystem.tvOS:
            case NativeOperatingSystem.iOS:
            case NativeOperatingSystem.Linux:
            case NativeOperatingSystem.FreeBSD:
            case NativeOperatingSystem.Android:
            case NativeOperatingSystem.PlayStation4:
                return "lib";
            case NativeOperatingSystem.Browser:
            case NativeOperatingSystem.Switch:
            case NativeOperatingSystem.DualScreen3D:
            case NativeOperatingSystem.Unknown:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(nativeOperatingSystem), nativeOperatingSystem, null);
        }
    }
}
