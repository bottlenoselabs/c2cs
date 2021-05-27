// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        GenerateLibraryBindings(rootDirectory);
        BuildLibrary(rootDirectory);
    }

    private static void BuildLibrary(string rootDirectory)
    {
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/sdl");
        var targetLibraryDirectoryPath = $"{rootDirectory}/src/cs/examples/sdl/sdl-cs/";
        var isSuccess = Shell.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
        if (!isSuccess)
        {
            Environment.Exit(1);
        }
    }

    private static void GenerateLibraryBindings(string rootDirectory)
    {
        var arguments = @$"
-i
{rootDirectory}/ext/SDL/include/SDL.h
-s
{rootDirectory}/ext/SDL/include
-o
{rootDirectory}/src/cs/examples/sdl/sdl-cs/sdl.cs
-u
-f
-t
-l
SDL2
-c
SDL
";

        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
