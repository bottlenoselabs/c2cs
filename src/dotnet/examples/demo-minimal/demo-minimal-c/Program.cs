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
        var cMakeDirectoryPath = Path.Combine(rootDirectory, "src/c/examples/demo-minimal");
        if (!Directory.Exists(cMakeDirectoryPath))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        var currentApplicationBaseDirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);

        "cmake -S . -B build-temp -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release".Bash(cMakeDirectoryPath);
        "make -C ./build-temp".Bash(cMakeDirectoryPath);
        $"cp -a {cMakeDirectoryPath}/lib/* {currentApplicationBaseDirectoryPath}".Bash();
        "rm -rf ./build-temp".Bash(cMakeDirectoryPath);
    }

    private static void GenerateLibraryBindings(string rootDirectory)
    {
        var arguments = @$"
-i
{rootDirectory}/src/c/examples/demo-minimal/include/library.h
-s
{rootDirectory}/src/c/examples/demo-minimal/include
-o
{rootDirectory}/src/dotnet/examples/demo-minimal/demo-minimal-cs/demo-minimal.cs
-u
-t
-l
demo-minimal
-c
demo_minimal
";
        var argumentsArray =
            arguments.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        C2CS.Program.Main(argumentsArray);
    }
}
