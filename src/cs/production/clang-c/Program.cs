// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        var searchDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var directoryName = searchDirectory[(searchDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
        while (directoryName != "bin")
        {
            searchDirectory = Path.GetFullPath(Path.Combine(searchDirectory, ".."));
            directoryName = searchDirectory[(searchDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
        }

        var rootDirectory = Path.GetFullPath(Path.Combine(searchDirectory, ".."));
        GenerateCAbstractSyntaxTree(rootDirectory);
        GenerateCSharpBindings(rootDirectory);
    }

    private static void GenerateCAbstractSyntaxTree(string rootDirectory)
    {
        const string bitness = "64";
        var arguments = @$"
ast
-i
{rootDirectory}/ext/clang/include/clang-c/Index.h
-s
{rootDirectory}/ext/clang/include
-o
{rootDirectory}/src/cs/production/clang-c/ast/clang.json
-b
{bitness}
-w
{rootDirectory}/src/cs/production/clang-c/api.txt
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }

    private static void GenerateCSharpBindings(string rootDirectory)
    {
        var arguments = @$"
cs
-i
{rootDirectory}/src/cs/production/clang-c/ast/clang.json
-o
{rootDirectory}/src/cs/production/clang-cs/clang.cs
-l
libclang
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
