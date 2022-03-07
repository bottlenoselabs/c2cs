// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS;

internal static class Program
{
    private static void Main()
    {
        // var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../../.."));
        // if (!BuildLibrary(rootDirectory))
        // {
        //     // Error building C library
        //     return;
        // }

        GenerateBindingsCSharp();
        // GenerateBindingsCSharp(rootDirectory);
    }

    private static bool BuildLibrary(string rootDirectory)
    {
        C2CS.Feature.BuildLibraryC.Program.Main();
        var cMakeDirectoryPath =
            Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library");
        var targetLibraryDirectoryPath = Path.GetFullPath($"{rootDirectory}/src/cs/examples/helloworld/helloworld-cs");
        return Terminal.CMake(rootDirectory, cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static void GenerateBindingsCSharp()
    {
        // C2CS.Program.Main(new[] { "ast" });
        C2CS.Program.Main(new[] { "cs" });
    }

// private static void GenerateBindingsCSharp(string rootDirectory)
//     {
//         var arguments = @$"
// cs
// {rootDirectory}/src/cs/examples/helloworld/helloworld-c/my_c_library/ast/ast.json
// -n
// my_c_library_namespace
// ";
//         var argumentsArray =
//             arguments.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
//         C2CS.Program.Main(argumentsArray);
//     }
}
