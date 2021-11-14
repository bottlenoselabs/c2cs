// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        if (!BuildLibrary(rootDirectory))
        {
            // Error building C library
            return;
        }

        GenerateAbstractSyntaxTree(rootDirectory);
        GenerateBindingsCSharp(rootDirectory);
    }

    private static bool BuildLibrary(string rootDirectory)
    {
        var cMakeDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library");
        var targetLibraryDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-cs");
        return Terminal.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory)
    {
        var arguments = @$"
ast
-i
{rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library/include/my_c_library.h
-o
{rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library/ast/ast.json
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }

    private static void GenerateBindingsCSharp(string rootDirectory)
    {
        var arguments = @$"
cs
-i
{rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library/ast/ast.json
-o
{rootDirectory}/src/cs/examples/helloworld/helloworld-cs/my_c_library.cs
-w
my_c_library_namespace
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
