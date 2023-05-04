// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Native;
using Microsoft.Extensions.Logging;

namespace C2CS.Features.BuildCLibrary.Domain;

public partial class CMakeLibraryBuilder
{
    private readonly ILogger<CMakeLibraryBuilder> _logger;
    private readonly IDirectory _directory;
    private readonly IPath _path;
    private readonly IFile _file;

    public CMakeLibraryBuilder(ILogger<CMakeLibraryBuilder> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _directory = fileSystem.Directory;
        _path = fileSystem.Path;
        _file = fileSystem.File;
    }

    public bool BuildLibrary(BuildCLibraryInput input)
    {
        var cMakeOutputDirectoryPath = _path.GetFullPath(_path.Combine(input.CMakeDirectoryPath, "bin"));
        if (_directory.Exists(cMakeOutputDirectoryPath))
        {
            _directory.Delete(cMakeOutputDirectoryPath, true);
        }

        _directory.CreateDirectory(cMakeOutputDirectoryPath);

        var cMakeBuildDirectoryPath = _path.Combine(input.CMakeDirectoryPath, "cmake-build-release");
        if (_directory.Exists(cMakeBuildDirectoryPath))
        {
            _directory.Delete(cMakeBuildDirectoryPath, true);
        }

        if (!GenerateCMakeBuildFiles(input.CMakeDirectoryPath, cMakeOutputDirectoryPath, input.CMakeArguments))
        {
            return false;
        }

        if (!BuildCMakeSharedLibrary(input.CMakeDirectoryPath, cMakeOutputDirectoryPath, input.OutputDirectoryPath))
        {
            return false;
        }

        if (input.DeleteBuildFiles)
        {
            _directory.Delete(cMakeBuildDirectoryPath, true);
        }

        return true;
    }

    private bool BuildCMakeSharedLibrary(
        string cMakeDirectoryPath,
        string cMakeOutputDirectoryPath,
        string outputDirectoryPath)
    {
        const string cMakeBuildCommand = "cmake --build cmake-build-release --config Release";
        LogCMakeBuildingLibrary(cMakeBuildCommand);
        var result = cMakeBuildCommand.ExecuteShell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            LogCMakeBuildingLibraryFailed(result.Output);
            return false;
        }

        var dynamicLinkLibraryFileSearchPattern = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "*.dll",
            NativeOperatingSystem.macOS => "*.dylib",
            NativeOperatingSystem.Linux => "*.so",
            _ => "*.*"
        };

        var copiedOutputFilePaths = new List<string>();
        var outputFilePaths = _directory.EnumerateFiles(
            cMakeOutputDirectoryPath,
            dynamicLinkLibraryFileSearchPattern,
            SearchOption.AllDirectories);
        foreach (var outputFilePath in outputFilePaths)
        {
            var outputFileName = _path.GetFileName(outputFilePath);
            var outputFilePathCopy = _path.Combine(outputDirectoryPath, outputFileName);
            _file.Copy(outputFilePath, outputFilePathCopy, true);
            copiedOutputFilePaths.Add(outputFilePathCopy);
        }

        var copiedOutputFilePathsString = string.Join(", ", copiedOutputFilePaths);
        LogCMakeBuildingLibrarySuccess(copiedOutputFilePathsString, result.Output);

        _directory.Delete(cMakeOutputDirectoryPath, true);
        return true;
    }

    private bool GenerateCMakeBuildFiles(
        string cMakeDirectoryPath,
        string cMakeOutputDirectoryPath,
        ImmutableArray<string> cMakeArguments)
    {
        var fullCMakeArguments = new[]
        {
            "-DCMAKE_BUILD_TYPE=Release",
            $"-DCMAKE_ARCHIVE_OUTPUT_DIRECTORY=\"{cMakeOutputDirectoryPath}\"",
            $"-DCMAKE_LIBRARY_OUTPUT_DIRECTORY=\"{cMakeOutputDirectoryPath}\"",
            $"-DCMAKE_RUNTIME_OUTPUT_DIRECTORY=\"{cMakeOutputDirectoryPath}\""
        }.Concat(cMakeArguments);
        var cMakeArgumentsString = string.Join(" ", fullCMakeArguments);

        var cMakeGenerateBuildFilesCommand = $"cmake -S . -B cmake-build-release {cMakeArgumentsString}";
        LogCMakeGeneratingBuildFiles(cMakeGenerateBuildFilesCommand);
        var result = cMakeGenerateBuildFilesCommand
            .ExecuteShell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            LogCMakeGeneratingBuildFilesFailed(result.Output);
            return false;
        }

        LogCMakeGeneratingBuildFilesSuccess(result.Output);
        return true;
    }

    [LoggerMessage(0, LogLevel.Information, "- CMake generating build files. Command: {Command}")]
    private partial void LogCMakeGeneratingBuildFiles(string command);

    [LoggerMessage(1, LogLevel.Error, "- Generating CMake build files failed. Output: \n{Output}\n")]
    private partial void LogCMakeGeneratingBuildFilesFailed(string output);

    [LoggerMessage(2, LogLevel.Information, "- Generating CMake build files success. Output: \n{Output}\n")]
    private partial void LogCMakeGeneratingBuildFilesSuccess(string output);

    [LoggerMessage(3, LogLevel.Information, "- CMake building shared library. Command: {Command}")]
    private partial void LogCMakeBuildingLibrary(string command);

    [LoggerMessage(4, LogLevel.Error, "- CMake building shared library failed. Output: \n{Output}\n")]
    private partial void LogCMakeBuildingLibraryFailed(string output);

    [LoggerMessage(5, LogLevel.Information, "- CMake building shared library success. Copied output files to: {FilePaths}. Output: \n{Output}\n")]
    private partial void LogCMakeBuildingLibrarySuccess(string filePaths, string output);
}
