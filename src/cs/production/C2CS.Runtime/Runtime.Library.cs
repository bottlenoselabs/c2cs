// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS;

public static partial class Runtime
{
    private static ImmutableArray<string> _nativeSearchDirectories;

    private static void GetNativeLibrarySearchDirectories()
    {
        // For security purposes we don't allow loading a library by just any path;
        //  use NATIVE_DLL_SEARCH_DIRECTORIES instead.
        // https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing#unmanaged-native-library-probing
        if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string nativeSearchDirectoriesString)
        {
            var nativeSearchDirectoriesDelimiter = OperatingSystem == RuntimeOperatingSystem.Windows ? ';' : ':';
            var nativeSearchDirectories =
                nativeSearchDirectoriesString!.Split(nativeSearchDirectoriesDelimiter, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (!nativeSearchDirectories.Contains(AppContext.BaseDirectory))
            {
                nativeSearchDirectories.Add(AppContext.BaseDirectory);
            }

            _nativeSearchDirectories = nativeSearchDirectories.ToImmutableArray();

            var shouldPrint = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PRINT_NATIVE_DLL_SEARCH_DIRECTORIES"));
            if (!shouldPrint)
            {
                return;
            }

            Console.WriteLine("NATIVE_DLL_SEARCH_DIRECTORIES:");
            foreach (var directory in _nativeSearchDirectories)
            {
                Console.WriteLine($"\t{directory}");
            }
        }
        else
        {
            // This case should not happen
            _nativeSearchDirectories = ImmutableArray.Create(AppContext.BaseDirectory);
        }
    }

    private static string LibraryFilePath(string libraryName)
    {
        var runtimePlatform = OperatingSystem;
        var libraryFileNamePrefix = LibraryFileNamePrefix(runtimePlatform);
        var libraryFileNameExtension = LibraryFileNameExtension(runtimePlatform);
        var libraryFileName = $@"{libraryFileNamePrefix}{libraryName}{libraryFileNameExtension}";

        foreach (var searchDirectory in _nativeSearchDirectories)
        {
            var libraryFilePath = Path.Combine(searchDirectory, libraryFileName);
            if (File.Exists(libraryFilePath))
            {
                return libraryFilePath;
            }

            // TRICK: Cross compiling for Windows on Linux automatically adds the prefix "lib"
            if (OperatingSystem == RuntimeOperatingSystem.Windows)
            {
                var libraryFilePath2 = Path.Combine(searchDirectory, "lib" + libraryFileName);
                if (File.Exists(libraryFilePath2))
                {
                    return libraryFilePath2;
                }
            }
        }

        return string.Empty;
    }

    private static void PrintLibraryNotFound(string libraryName)
    {
        var runtimePlatform = OperatingSystem;
        var libraryFileNamePrefix = LibraryFileNamePrefix(runtimePlatform);
        var libraryFileNameExtension = LibraryFileNameExtension(runtimePlatform);
        var libraryFileName = $@"{libraryFileNamePrefix}{libraryName}{libraryFileNameExtension}";

        Console.WriteLine($"The library could not be found: '{libraryFileName}'. Expected to find the library in one of the following paths:");
        foreach (var searchDirectory in _nativeSearchDirectories)
        {
            if (OperatingSystem == RuntimeOperatingSystem.Windows)
            {
                var libraryFilePath = Path.Combine(searchDirectory, libraryFileName);
                var libraryFilePath2 = Path.Combine(searchDirectory, "lib" + libraryFileName);
                Console.WriteLine($"\t{libraryFilePath} OR {libraryFilePath2}");
            }
            else
            {
                var libraryFilePath = Path.Combine(searchDirectory, libraryFileName);
                Console.WriteLine($"\t{libraryFilePath}");
            }
        }
    }

    private static void PrintLibraryLoadError(string libraryFilePath)
    {
        Console.WriteLine($"The library failed to load: '{libraryFilePath}'. This is likely caused by one or more required dependencies failing to load; use a dependency walker tool to check.");
    }
}