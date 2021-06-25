// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;

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
        // var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/sdl");
        // var targetLibraryDirectoryPath = $"{rootDirectory}/src/cs/examples/sdl/sdl-cs/";
        // var isSuccess = Shell.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
        // if (!isSuccess)
        // {
        //     Environment.Exit(1);
        // }
        //
        // var oldLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, "libSDL2-2.0.dylib");
        // if (File.Exists(oldLibraryFilePath))
        // {
        //     var newLibraryFilePath = Path.Combine(targetLibraryDirectoryPath, "libSDL2.dylib");
        //     if (File.Exists(newLibraryFilePath))
        //     {
        //         File.Delete(newLibraryFilePath);
        //     }
        //
        //     File.Move(oldLibraryFilePath, newLibraryFilePath);
        // }
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory)
    {
        var arguments = @$"
ast
-i
{rootDirectory}/ext/SDL/include/SDL.h
-o
{rootDirectory}/src/cs/examples/sdl/sdl-c/ast.json
-c
{Environment.CurrentDirectory}/config_c.json
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
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
-c
{Environment.CurrentDirectory}/config_csharp.json
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
