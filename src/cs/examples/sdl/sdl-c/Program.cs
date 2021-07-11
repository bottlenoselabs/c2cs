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
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        GenerateAbstractSyntaxTree(rootDirectory);
        GenerateBindingsCSharp(rootDirectory);
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

        var oldLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, "libSDL2-2.0.dylib");
        if (File.Exists(oldLibraryFilePath))
        {
            var newLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, "libSDL2.dylib");
            if (File.Exists(newLibraryFilePath))
            {
                File.Delete(newLibraryFilePath);
            }

            File.Move(oldLibraryFilePath, newLibraryFilePath);
        }
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory)
    {
        var arguments = @$"
ast
-i
{rootDirectory}/ext/SDL/include/SDL.h
-o
{rootDirectory}/src/cs/examples/sdl/sdl-c/ast.json
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
            arguments.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }

    private static void GenerateBindingsCSharp(string rootDirectory)
    {
        var arguments = @$"
cs
-i
{rootDirectory}/src/cs/examples/sdl/sdl-c/ast.json
-o
{rootDirectory}/src/cs/examples/sdl/sdl-cs/SDL.cs
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
            arguments.Split(new[] { "\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
