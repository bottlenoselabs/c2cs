// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.CompilerServices;
#endif

public static partial class Runtime
{
    private static ImmutableArray<string> _nativeSearchDirectories;

    public static IntPtr LibraryLoad(string libraryName)
    {
        if (string.IsNullOrEmpty(libraryName))
        {
            return IntPtr.Zero;
        }

        var libraryFilePath = LibraryFilePath(libraryName);
        if (string.IsNullOrEmpty(libraryFilePath))
        {
            PrintLibraryLoadError(libraryName);
            return IntPtr.Zero;
        }

#if NETCOREAPP
        Unsafe.SkipInit<IntPtr>(out var handle);
#else
        var handle = default(IntPtr.Zero);
#endif

#if NETCOREAPP
        if (!NativeLibrary.TryLoad(libraryFilePath, out handle))
        {
            return IntPtr.Zero;
        }
#else
        throw new NotImplementedException();
#endif

        return handle;
    }

    public static void LibraryUnload(IntPtr libraryHandle)
    {
#if NETCOREAPP
        NativeLibrary.Free(libraryHandle);
#else
        throw new NotImplementedException();
#endif
    }

    public static IntPtr LibraryGetExport(IntPtr libraryHandle, string name)
    {
        if (libraryHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

#if NETCOREAPP
        Unsafe.SkipInit<IntPtr>(out var address);
#else
        var address = default(IntPtr.Zero);
#endif

#if NETCOREAPP
        if (!NativeLibrary.TryGetExport(libraryHandle, name, out address))
        {
            return IntPtr.Zero;
        }
#else
        throw new NotImplementedException();
#endif

        return address;
    }

    private static void GetNativeLibrarySearchDirectories()
    {
        // For security purposes we don't allow loading a library by just any path;
        //  use NATIVE_DLL_SEARCH_DIRECTORIES instead.
        // https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing#unmanaged-native-library-probing
        var nativeSearchDirectoriesObject = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
        if (nativeSearchDirectoriesObject == null)
        {
            var nativeSearchDirectoriesString = (string) nativeSearchDirectoriesObject!;
            var nativeSearchDirectoryStrings = // delimiter is `;` on Windows; otherwise delimiter is `:`
                nativeSearchDirectoriesString.Split(new[] {':', ';'}, StringSplitOptions.RemoveEmptyEntries);
            _nativeSearchDirectories = nativeSearchDirectoryStrings.ToImmutableArray();
        }
        else
        {
            // This case can happen if we are not single file publishing or AOT, such as normal .NET debug/release run
            _nativeSearchDirectories = ImmutableArray.Create(
                AppContext.BaseDirectory,
                // TRICK: Since we are always finding the native library by complete file path we can get away with this
                Path.Combine(AppContext.BaseDirectory, "runtimes", "win-x64", "native"),
                Path.Combine(AppContext.BaseDirectory, "runtimes", "osx-x64", "native"),
                Path.Combine(AppContext.BaseDirectory, "runtimes", "ubuntu.16.04-x64", "native"));
        }
    }

    private static string LibraryFilePath(string libraryName)
    {
        var runtimePlatform = Platform;
        var libraryFileNamePrefix = LibraryFileNamePrefix(runtimePlatform);
        var libraryFileNameExtension = LibraryFileNameExtension(runtimePlatform);
        var libraryFileName = $@"{libraryFileNamePrefix}{libraryName}{libraryFileNameExtension}";

        foreach (var searchDirectory in _nativeSearchDirectories)
        {
            var libraryFilePath = Path.Combine(searchDirectory, libraryFileName);
            if (!File.Exists(libraryFilePath))
            {
                continue;
            }

            return libraryFilePath;
        }

        return string.Empty;
    }

    private static void PrintLibraryLoadError(string libraryName)
    {
        var runtimePlatform = Platform;
        var libraryFileNamePrefix = LibraryFileNamePrefix(runtimePlatform);
        var libraryFileNameExtension = LibraryFileNameExtension(runtimePlatform);
        var libraryFileName = $@"{libraryFileNamePrefix}{libraryName}{libraryFileNameExtension}";

        Console.WriteLine($"Failed to load '{libraryFileName}'. Expected to find the library in one of the following paths:");
        foreach (var searchDirectory in _nativeSearchDirectories)
        {
            var libraryFilePath = Path.Combine(searchDirectory, libraryFileName);
            Console.WriteLine($"\t{libraryFilePath}");
        }
    }
}
