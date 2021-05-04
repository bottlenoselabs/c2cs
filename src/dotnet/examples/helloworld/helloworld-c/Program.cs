// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS.Tools;

internal static class Program
{
    private static void Main()
    {
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        BuildLibrary(rootDirectory);
        GenerateLibraryBindings(rootDirectory);
    }

    private static void BuildLibrary(string rootDirectory)
    {
        var cMakeDirectoryPath = Path.GetFullPath(Path.Combine(rootDirectory, "src/c/examples/helloworld"));
        var targetLibraryDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/dotnet/examples/helloworld/helloworld-cs");
        Shell.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static void GenerateLibraryBindings(string rootDirectory)
    {
        var arguments = @$"
-i
{rootDirectory}/src/c/examples/helloworld/include/library.h
-s
{rootDirectory}/src/c/examples/helloworld/include
-o
{rootDirectory}/src/dotnet/examples/helloworld/helloworld-cs/helloworld.cs
-u
-f
-t
-l
helloworld
-c
helloworld
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
