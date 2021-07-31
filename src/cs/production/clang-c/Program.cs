// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
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

        var searchDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
        var directoryName = searchDirectory[(searchDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
        while (directoryName != "bin")
        {
            searchDirectory = Path.GetFullPath(Path.Combine(searchDirectory, ".."));
            directoryName = searchDirectory[(searchDirectory.LastIndexOf(Path.DirectorySeparatorChar) + 1)..];
        }

        var rootDirectory = Path.GetFullPath(Path.Combine(searchDirectory, ".."));
        GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier64Bits);
        GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier32Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier64Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier32Bits);
    }

    private static void GenerateAbstractSyntaxTree(string rootDirectory, string runtimeIdentifier)
    {
        var bitness = runtimeIdentifier.EndsWith("64", StringComparison.InvariantCulture) ? "64" : "32";

        var arguments = @$"
ast
-i
{rootDirectory}/ext/clang/include/clang-c/Index.h
-s
{rootDirectory}/ext/clang/include
-o
{rootDirectory}/src/cs/production/clang-c/ast.{runtimeIdentifier}.json
-b
{bitness}
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
{rootDirectory}/src/cs/production/clang-c/ast.{runtimeIdentifier}.json
-o
{rootDirectory}/src/cs/production/clang-cs/clang.{runtimeIdentifier}.cs
-l
libclang
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
