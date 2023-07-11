// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using C2CS.Features.BuildCLibrary.Domain;
using C2CS.Native;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#pragma warning disable CA1303

internal static class Program
{
    private static void Main()
    {
        var applicationName = GetApplicationName();
        var rootDirectory = GetGitRepositoryPath();
        var sourceDirectoryPath = Path.GetFullPath(Path.Combine(
            rootDirectory, "src", "cs", "examples", "helloworld", applicationName));

        if (!BuildCLibrary(sourceDirectoryPath))
        {
            Console.WriteLine("Error building C library");
            return;
        }

        if (!GenerateBindingsCSharp(sourceDirectoryPath))
        {
            Console.WriteLine("Error generating C# code");
        }
    }

    private static bool BuildCLibrary(string sourceDirectoryPath)
    {
        var configBuildCLibraryFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "config-build-c-library.json"));
        var parameters = new[] { "library", "--config", configBuildCLibraryFilePath };
        return C2CS.Program.Main(parameters) == 0;
    }

    private static bool GenerateBindingsCSharp(string sourceDirectoryPath)
    {
        var bindgenConfigFileName = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "config-extract-windows.json",
            NativeOperatingSystem.macOS => "config-extract-macos.json",
            NativeOperatingSystem.Linux => "config-extract-linux.json",
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

        var configGenerateCSharpCodeFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "config-generate-cs.json"));
        var parametersGenerateCSharpCode = new[] { "generate", "--config", configGenerateCSharpCodeFilePath };
        C2CS.Program.Main(parametersGenerateCSharpCode);

        return true;
    }

    private static string GetGitRepositoryPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var directoryInfo = new DirectoryInfo(baseDirectory);
        while (true)
        {
            var files = directoryInfo.GetFiles(".gitignore", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                return directoryInfo.FullName;
            }

            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null)
            {
                return string.Empty;
            }
        }
    }

    private static string GetApplicationName()
    {
        var applicationAssemblyFilePath = typeof(Program).Assembly.Location;
        var applicationAssemblyMainFileDirectory =
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(applicationAssemblyFilePath)!, ".."));
        var applicationName = Path.GetFileName(applicationAssemblyMainFileDirectory);
        return applicationName;
    }
}
