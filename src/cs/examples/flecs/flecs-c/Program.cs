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
        GenerateAbstractSyntaxTree(rootDirectory);
        GenerateBindingsCSharp(rootDirectory);
        BuildLibrary(rootDirectory);
    }

    private static void BuildLibrary(string rootDirectory)
    {
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/flecs");
        var targetLibraryDirectoryPath = $"{rootDirectory}/src/cs/examples/flecs/flecs-cs/";
        var isSuccess = Terminal.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
        if (!isSuccess)
        {
            Environment.Exit(1);
        }
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory)
    {
        var arguments = @$"
ast
-i
{rootDirectory}/ext/flecs/include/flecs.h
-o
{rootDirectory}/src/cs/examples/flecs/flecs-c/ast.json
";
        var argumentsArray =
            arguments.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }

    private static void GenerateBindingsCSharp(string rootDirectory)
    {
        var arguments = @$"
cs
-i
{rootDirectory}/src/cs/examples/flecs/flecs-c/ast.json
-o
{rootDirectory}/src/cs/examples/flecs/flecs-cs/flecs.cs
";
        var argumentsArray =
            arguments.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
