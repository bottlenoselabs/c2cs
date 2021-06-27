// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace C2CS
{
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

            Unsafe.SkipInit<IntPtr>(out var handle);

            if (!NativeLibrary.TryLoad(libraryFilePath, out handle))
            {
                return IntPtr.Zero;
            }

            return handle;
        }

        public static void LibraryUnload(IntPtr libraryHandle)
        {
            NativeLibrary.Free(libraryHandle);
        }

        public static IntPtr LibraryGetExport(IntPtr libraryHandle, string name)
        {
            if (libraryHandle == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            Unsafe.SkipInit<IntPtr>(out var address);

            if (!NativeLibrary.TryGetExport(libraryHandle, name, out address))
            {
                return IntPtr.Zero;
            }

            return address;
        }

        private static void GetNativeLibrarySearchDirectories()
        {
            // For security purposes we don't allow loading a library by just any path;
            //  use NATIVE_DLL_SEARCH_DIRECTORIES instead.
            // https://docs.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing#unmanaged-native-library-probing
            if (AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") is string nativeSearchDirectoriesString)
            {
                var nativeSearchDirectoryStrings = // delimiter is `;` on Windows; otherwise delimiter is `:`
                    nativeSearchDirectoriesString!.Split(new[] {':', ';'}, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (!nativeSearchDirectoryStrings.Contains(AppContext.BaseDirectory))
                {
                    nativeSearchDirectoryStrings.Add(AppContext.BaseDirectory);
                }

                _nativeSearchDirectories = nativeSearchDirectoryStrings.ToImmutableArray();

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
}
