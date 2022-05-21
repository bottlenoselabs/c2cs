// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json.Serialization;
using C2CS.Serialization;
using JetBrains.Annotations;

namespace C2CS;

// ReSharper disable InconsistentNaming
#pragma warning disable SA1124
#pragma warning disable SA1300
#pragma warning disable SA1307
#pragma warning disable SA1310
#pragma warning disable SA1311
#pragma warning disable SA1313
#pragma warning disable CA2211
#pragma warning disable CA1707
#pragma warning disable IDE1006

/// <summary>
///     Defines the native platforms.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(NativePlatformJsonConverter))]
public record struct TargetPlatform(string TargetName)
{
    internal string TargetName = TargetName;

    /// <summary>
    ///     The runtime operating system.
    /// </summary>
    public NativeOperatingSystem OperatingSystem = ParseTargetOperatingSystem(TargetName);

    /// <summary>
    ///     The runtime computer architecture.
    /// </summary>
    public NativeArchitecture Architecture = ParseTargetArchitecture(TargetName);

    /// <summary>
    ///     Unknown runtime platform.
    /// </summary>
    public static readonly TargetPlatform Unknown = new("unknown-unknown-unknown");

    #region Windows

    /// <summary>
    ///     X86 Windows (32-bit, Windows 7+) using Microsoft's compiler and linker.
    /// </summary>
    public static readonly TargetPlatform i686_pc_windows_msvc = new("i686-pc-windows-msvc");

    /// <summary>
    ///     X64 Windows (64-bit, Windows 7+) using Microsoft's compiler and linker.
    /// </summary>
    public static readonly TargetPlatform x86_64_pc_windows_msvc = new("x86_64-pc-windows-msvc");

    /// <summary>
    ///     ARM64 Windows (64-bit) using Microsoft's compiler and linker.
    /// </summary>
    public static readonly TargetPlatform aarch64_pc_windows_msvc = new("aarch64-pc-windows-msvc");

    /// <summary>
    ///     X86 Windows (32-bit, Windows 7+) using GNU's Compiler Collection (GCC).
    /// </summary>
    public static readonly TargetPlatform i686_pc_windows_gnu = new("i686-pc-windows-gnu");

    /// <summary>
    ///     X64 Windows (64-bit, Windows 7+) using GNU's Compiler Collection (GCC).
    /// </summary>
    public static readonly TargetPlatform x86_64_pc_windows_gnu = new("x86_64-pc-windows-gnu");

    /// <summary>
    ///     ARM64 Windows (64-bit) using GNU's Compiler Collection (GCC).
    /// </summary>
    public static readonly TargetPlatform aarch64_pc_windows_gnu = new("aarch64-pc-windows-gnu");

    #endregion

    #region Linux

    /// <summary>
    ///     X86 Linux (32-bit, kernel 2.6.32+, glibc 2.11+).
    /// </summary>
    public static readonly TargetPlatform i686_unknown_linux_gnu = new("i686-unknown-linux-gnu");

    /// <summary>
    ///     X64 Linux (64-bit, kernel 2.6.32+, glibc 2.11+).
    /// </summary>
    public static readonly TargetPlatform x86_64_unknown_linux_gnu = new("x86_64-unknown-linux-gnu");

    /// <summary>
    ///     ARM64 Linux (64-bit, kernel 4.2, glibc 2.17+).
    /// </summary>
    public static readonly TargetPlatform aarch64_unknown_linux_gnu = new("aarch64-unknown-linux-gnu");

    #endregion

    #region macOS

    /// <summary>
    ///     X86 macOS (32-bit, 10.7+, Lion+).
    /// </summary>
    public static readonly TargetPlatform i686_apple_darwin = new("i686-apple-darwin");

    /// <summary>
    ///     X64 macOS (64-bit, 10.7+, Lion+).
    /// </summary>
    public static readonly TargetPlatform x86_64_apple_darwin = new("x86_64-apple-darwin");

    /// <summary>
    ///     ARM64 macOS (64-bit, 11.0+, Big Sur+).
    /// </summary>
    public static readonly TargetPlatform aarch64_apple_darwin = new("aarch64-apple-darwin");

    #endregion

    #region iOS

    /// <summary>
    ///     ARM64 iOS (64-bit).
    /// </summary>
    public static readonly TargetPlatform aarch64_apple_ios = new("aarch64-apple-ios");

    /// <summary>
    ///     ARM64 iOS simulator (64-bit).
    /// </summary>
    public static readonly TargetPlatform aarch64_apple_ios_sim = new("aarch64-apple-ios-sim");

    /// <summary>
    ///     X64 iOS (64-bit).
    /// </summary>
    public static readonly TargetPlatform x86_64_apple_ios = new("x86_64-apple-ios");

    #endregion

    #region Android

    /// <summary>
    ///     ARM64 Android (64-bit).
    /// </summary>
    public static readonly TargetPlatform aarch64_linux_android = new("aarch64-linux-android");

    /// <summary>
    ///     ARM32 (ARMv7) Android (32-bit).
    /// </summary>
    public static readonly TargetPlatform arm_linux_androideabi = new("arm-linux-androideabi");

    /// <summary>
    ///     X86 Android (32-bit).
    /// </summary>
    public static readonly TargetPlatform i686_linux_android = new("i686-linux-android");

    /// <summary>
    ///     X64 Android (64-bit).
    /// </summary>
    public static readonly TargetPlatform x86_64_linux_android = new("x86_64-linux-android");

    #endregion

    #region Browser

    /// <summary>
    ///     WebAssembly (32-bit).
    /// </summary>
    public static readonly TargetPlatform wasm32_unknown_unknown = new("wasm32-unknown-unknown");

    /// <summary>
    ///     WebAssembly via Emscripten (32-bit).
    /// </summary>
    public static readonly TargetPlatform wasm32_unknown_emscripten = new("wasm32-unknown-emscripten");

    #endregion

    #region Sony

    /// <summary>
    ///     PlayStation 4 (64-bit).
    /// </summary>
    public static readonly TargetPlatform x86_64_scei_ps4 = new("x86_64-scei-ps4");

    #endregion

    #region Nintendo

    /// <summary>
    ///     Nintendo 3DS (32-bit).
    /// </summary>
    public static readonly TargetPlatform armv6k_nintendo_3ds = new("armv6k-nintendo-3ds");

	#endregion

    private static NativeArchitecture ParseTargetArchitecture(string targetTriple)
    {
        if (targetTriple.StartsWith("aarch64-", StringComparison.InvariantCultureIgnoreCase) ||
            targetTriple.StartsWith("arm64-", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.ARM64;
        }

        if (targetTriple.StartsWith("x86_64-", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.X64;
        }

        if (targetTriple.StartsWith("i686-", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.X86;
        }

        if (targetTriple.StartsWith("arm-", StringComparison.InvariantCultureIgnoreCase) ||
            targetTriple.StartsWith("armv7-", StringComparison.InvariantCultureIgnoreCase) ||
            targetTriple.StartsWith("armv6k-", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.ARM32;
        }

        if (targetTriple.StartsWith("wasm64", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.WASM64;
        }

        if (targetTriple.StartsWith("wasm32", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeArchitecture.WASM32;
        }

        return NativeArchitecture.Unknown;
    }

    private static NativeOperatingSystem ParseTargetOperatingSystem(string targetTriple)
    {
        if (targetTriple.Contains("-pc-windows", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.Windows;
        }

        if (targetTriple.Contains("-unknown-linux", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.Linux;
        }

        if (targetTriple.Contains("-apple-darwin", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.macOS;
        }

        if (targetTriple.Contains("-apple-ios", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.iOS;
        }

        if (targetTriple.Contains("-apple-tvos", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.tvOS;
        }

        if (targetTriple.Contains("-unknown-freebsd", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.FreeBSD;
        }

        if (targetTriple.Contains("-linux-android", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.Android;
        }

        if (targetTriple.Contains("-scei-ps4", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.PlayStation4;
        }

        if (targetTriple.Contains("-nintendo_3ds", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.DualScreen3D;
        }

        if (targetTriple.StartsWith("wasm", StringComparison.InvariantCultureIgnoreCase))
        {
            return NativeOperatingSystem.Browser;
        }

        return NativeOperatingSystem.Unknown;
    }

    public override string ToString()
    {
        return TargetName;
    }
}
