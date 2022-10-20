﻿// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../.."));
        if (!BuildLibrary(rootDirectory))
        {
#pragma warning disable CA1303
            Console.WriteLine("Error building C library");
#pragma warning restore CA1303
            return;
        }

        GenerateBindingsCSharp();
    }

    private static bool BuildLibrary(string rootDirectory)
    {
        var thisApplicationAssemblyFilePath = typeof(Program).Assembly.Location;
        var thisApplicationAssemblyMainFileDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisApplicationAssemblyFilePath)!, ".."));
        var thisApplicationName = Path.GetFileName(thisApplicationAssemblyMainFileDirectory);
        var cMakeDirectoryPath =
            Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/{thisApplicationName}/my_c_library");
        var targetLibraryDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-app");
        return CMake.Build(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static void GenerateBindingsCSharp()
    {
        var parameters = Array.Empty<string>();
        C2CS.Program.Main(parameters);
    }
}
