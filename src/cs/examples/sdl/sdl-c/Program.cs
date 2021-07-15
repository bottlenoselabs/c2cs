// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        var runtimeIdentifierOperatingSystem = string.Empty;
        if (OperatingSystem.IsWindows())
        {
            runtimeIdentifierOperatingSystem = "win";
        }
        else if (OperatingSystem.IsMacOS())
        {
            runtimeIdentifierOperatingSystem = "osx";
        }
        else if (OperatingSystem.IsLinux())
        {
            runtimeIdentifierOperatingSystem = "linux";
        }

        var runtimeIdentifier32Bits = runtimeIdentifierOperatingSystem + "32";
        var runtimeIdentifier64Bits = runtimeIdentifierOperatingSystem + "64";

        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier64Bits);
        GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier32Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier64Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier32Bits);
        // BuildLibrary(rootDirectory);
    }

    private static void BuildLibrary(string rootDirectory)
    {
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/sdl");
        var targetLibraryDirectoryPath = $"{rootDirectory}/src/cs/examples/sdl/sdl-cs/";
        var isSuccess = Terminal.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
        if (!isSuccess)
        {
            Environment.Exit(1);
        }

        var libraryFileNameExtension = Runtime.LibraryFileNameExtension(Runtime.OperatingSystem);
        var oldLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, $"libSDL2-2.0{libraryFileNameExtension}");
        if (File.Exists(oldLibraryFilePath))
        {
            var newLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, $"libSDL2{libraryFileNameExtension}");
            if (File.Exists(newLibraryFilePath))
            {
                File.Delete(newLibraryFilePath);
            }

            File.Move(oldLibraryFilePath, newLibraryFilePath);
        }
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory, string runtimeIdentifier)
    {
        var bitness = runtimeIdentifier.EndsWith("64", StringComparison.InvariantCulture) ? "64" : "32";

        var arguments = @$"
ast
-i
{rootDirectory}/ext/SDL/include/SDL.h
-o
{rootDirectory}/src/cs/examples/sdl/sdl-c/ast.{runtimeIdentifier}.json
-b
{bitness}
-d
SDL_DISABLE_MM3DNOW_H
SDL_DISABLE_IMMINTRIN_H
SDL_DISABLE_MMINTRIN_H
SDL_DISABLE_XMMINTRIN_H
SDL_DISABLE_EMMINTRIN_H
SDL_DISABLE_PMMINTRIN_H
-g
SDL_main.h
SDL_assert.h
SDL_system.h
SDL_stdinc.h
SDL_thread.h
SDL_endian.h
-p
SDL_RWops
SDL_AudioCVT
SDL_Thread
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }

    private static void GenerateBindingsCSharp(string rootDirectory, string runtimeIdentifier)
    {
        var arguments = @$"
cs
-i
{rootDirectory}/src/cs/examples/sdl/sdl-c/ast.{runtimeIdentifier}.json
-o
{rootDirectory}/src/cs/examples/sdl/sdl-cs/SDL.{runtimeIdentifier}.cs
-l
SDL2
-a
SDL_bool -> CBool
Uint8 -> byte
Uint16 -> ushort
Uint32 -> uint
Uint64 -> ulong
Sint8 -> sbyte
Sint16 -> short
Sint32 -> int
Sint64 -> long
-g
SDL_bool
".Trim();
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
