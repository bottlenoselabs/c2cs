// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

public static partial class NativeTools
{
    /// <summary>
    ///     Gets the current <see cref="NativeRuntimePlatform" />.
    /// </summary>
    public static NativeRuntimePlatform RuntimePlatform => GetRuntimePlatform();

    /// <summary>
    ///     Gets the library file name extension for the current platform.
    /// </summary>
    public static string LibraryFileNameExtension => GetLibraryFileNameExtension(GetRuntimePlatform());

    /// <summary>
    ///     Gets the library file name prefix for the current platform.
    /// </summary>
    public static string LibraryFileNamePrefix => GetLibraryFileNamePrefix(GetRuntimePlatform());

    private static NativeRuntimePlatform GetRuntimePlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return NativeRuntimePlatform.Windows;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return NativeRuntimePlatform.macOS;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return NativeRuntimePlatform.Linux;
        }

        // TODO: iOS, Android, etc

        return NativeRuntimePlatform.Unknown;
    }

    private static string GetLibraryFileNameExtension(NativeRuntimePlatform platform)
    {
        return platform switch
        {
            NativeRuntimePlatform.Windows => ".dll",
            NativeRuntimePlatform.macOS => ".dylib",
            NativeRuntimePlatform.Linux => ".so",
            NativeRuntimePlatform.Android => throw new NotImplementedException(),
            NativeRuntimePlatform.iOS => throw new NotImplementedException(),
            NativeRuntimePlatform.Web => throw new NotImplementedException(),
            NativeRuntimePlatform.Unknown => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
    }

    private static string GetLibraryFileNamePrefix(NativeRuntimePlatform platform)
    {
        return platform switch
        {
            NativeRuntimePlatform.Windows => string.Empty,
            NativeRuntimePlatform.macOS => "lib",
            NativeRuntimePlatform.Linux => "lib",
            NativeRuntimePlatform.Android => throw new NotImplementedException(),
            NativeRuntimePlatform.iOS => throw new NotImplementedException(),
            NativeRuntimePlatform.Web => throw new NotImplementedException(),
            NativeRuntimePlatform.Unknown => throw new NotSupportedException(),
            _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
        };
    }
}
