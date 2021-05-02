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
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/flecs");
        if (!Directory.Exists(cMakeDirectoryPath))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        var targetLibraryDirectoryPath = $"{rootDirectory}/src/dotnet/examples/flecs/flecs-cs/";

        "cmake -S . -B cmake-build-release -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release".Bash(cMakeDirectoryPath);
        "make -C ./cmake-build-release".Bash(cMakeDirectoryPath);
        $"mkdir -p {targetLibraryDirectoryPath}".Bash();
        $"cp -a {cMakeDirectoryPath}/lib/* {targetLibraryDirectoryPath}".Bash();
        "rm -rf ./cmake-build-release".Bash(cMakeDirectoryPath);
    }

    private static void GenerateLibraryBindings(string rootDirectory)
    {
        var arguments = @$"
-i
{rootDirectory}/ext/flecs/include/flecs.h
-o
{rootDirectory}/src/dotnet/examples/flecs/flecs-cs/flecs.cs
-u
-t
-l
flecs
-c
flecs
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
