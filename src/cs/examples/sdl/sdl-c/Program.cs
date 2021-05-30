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
        GenerateLibraryBindings();
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

    private static void GenerateLibraryBindings()
    {
        var arguments = @$"
-c
{Environment.CurrentDirectory}/config.json
";

        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
