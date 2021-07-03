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
        var cMakeDirectoryPath = Path.GetFullPath(Path.Combine(rootDirectory, "src/c/examples/helloworld"));
        var targetLibraryDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-cs");
        Terminal.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory)
    {
        var arguments = @$"
ast
-i
{rootDirectory}/src/c/examples/helloworld/include/helloworld.h
-o
{rootDirectory}/src/cs/examples/helloworld/helloworld-c/ast.json
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
{rootDirectory}/src/cs/examples/helloworld/helloworld-c/ast.json
-o
{rootDirectory}/src/cs/examples/helloworld/helloworld-cs/helloworld.cs
";
        var argumentsArray =
            arguments.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
