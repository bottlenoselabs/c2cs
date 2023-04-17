// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using C2CS.Foundation.CMake;
using C2CS.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA1303

internal static class Program
{
    private static void Main()
    {
        var thisApplicationAssemblyFilePath = typeof(Program).Assembly.Location;
        var thisApplicationAssemblyMainFileDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisApplicationAssemblyFilePath)!, ".."));
        var thisApplicationName = Path.GetFileName(thisApplicationAssemblyMainFileDirectory);
        var rootDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "../../.."));
        var sourceDirectoryPath =
            Path.GetFullPath(Path.Combine(rootDirectory, "src", "cs", "examples", "helloworld", thisApplicationName));

        if (!BuildLibrary(rootDirectory, sourceDirectoryPath))
        {
            Console.WriteLine("Error building C library");
            return;
        }

        if (!GenerateBindingsCSharp(rootDirectory, sourceDirectoryPath))
        {
            Console.WriteLine("Error generating C# code");
        }
    }

    private static bool BuildLibrary(string rootDirectory, string sourceDirectoryPath)
    {
        var cMakeDirectoryPath =
            Path.GetFullPath(Path.Combine(sourceDirectoryPath, "my_c_library"));
        var targetLibraryDirectoryPath = Path.GetFullPath(Path.Combine(rootDirectory, "src", "cs", "examples", "helloworld", "helloworld-app"));

        var logger = new Logger<CMakeLibraryBuilder>(NullLoggerFactory.Instance);
        var fileSystem = new FileSystem();
        var cmakeLibraryBuilder = new CMakeLibraryBuilder(logger, fileSystem);
        return cmakeLibraryBuilder.BuildLibrary(cMakeDirectoryPath, targetLibraryDirectoryPath);
    }

    private static bool GenerateBindingsCSharp(string rootDirectory, string sourceDirectoryPath)
    {
        var bindgenConfigFileName = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "config-windows.json",
            NativeOperatingSystem.macOS => "config-macos.json",
            NativeOperatingSystem.Linux => "config-linux.json",
            _ => throw new NotImplementedException()
        };

        var bindgenConfigFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, bindgenConfigFileName));

        var extractShellOutput = $"castffi extract --config {bindgenConfigFilePath}".ExecuteShell();
        if (extractShellOutput.ExitCode != 0)
        {
            return false;
        }

        var abstractSyntaxTreeDirectoryPath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "ast"));
        var mergedAbstractSyntaxTreeFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "ast", "cross-platform.json"));
        var astShellOutput = $"castffi merge --inputDirectoryPath {abstractSyntaxTreeDirectoryPath} --outputFilePath {mergedAbstractSyntaxTreeFilePath}".ExecuteShell();
        if (astShellOutput.ExitCode != 0)
        {
            return false;
        }

        var configGenerateCSharpCodeFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "config-cs.json"));
        var parametersGenerateCSharpCode = new[] { "--config", configGenerateCSharpCodeFilePath };
        C2CS.Program.Main(parametersGenerateCSharpCode);

        return true;
    }
}
