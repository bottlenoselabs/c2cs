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

        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        /*GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier64Bits);
        GenerateAbstractSyntaxTree(rootDirectory, runtimeIdentifier32Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier64Bits);
        GenerateBindingsCSharp(rootDirectory, runtimeIdentifier32Bits);*/
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

    private static void GenerateAbstractSyntaxTree(string rootDirectory, string runtimeIdentifier)
    {
        var bitness = runtimeIdentifier.EndsWith("64", StringComparison.InvariantCulture) ? "64" : "32";

        var arguments = @$"
ast
-i
{rootDirectory}/ext/flecs/include/flecs.h
-o
{rootDirectory}/src/cs/examples/flecs/flecs-c/ast.{runtimeIdentifier}.json
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
{rootDirectory}/src/cs/examples/flecs/flecs-c/ast.{runtimeIdentifier}.json
-o
{rootDirectory}/src/cs/examples/flecs/flecs-cs/flecs.{runtimeIdentifier}.cs
";
        var argumentsArray =
            arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
