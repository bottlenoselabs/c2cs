// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.Reflection;
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
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/demo-exhaustive");
        if (!Directory.Exists(cMakeDirectoryPath))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        var currentApplicationBaseDirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);

        "cmake -S . -B cmake-build-release -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release".Bash(cMakeDirectoryPath);
        "make -C ./cmake-build-release".Bash(cMakeDirectoryPath);
        $"cp -a {cMakeDirectoryPath}/lib/* {currentApplicationBaseDirectoryPath}".Bash();
        "rm -rf ./cmake-build-release".Bash(cMakeDirectoryPath);
    }

    private static void GenerateLibraryBindings(string rootDirectory)
    {
        var arguments = @$"
-i
{rootDirectory}/src/c/examples/demo-exhaustive/include/library.h
-s
{rootDirectory}/src/c/examples/demo-exhaustive/include
-o
{rootDirectory}/src/dotnet/examples/demo-exhaustive/demo-exhaustive-cs/demo-exhaustive.cs
-u
-t
-l
demo-exhaustive
-c
demo_exhaustive
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
