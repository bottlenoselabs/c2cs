// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO;
using System.IO.Abstractions;
using C2CS.Native;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.CMake;

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

    public bool BuildLibrary(string cMakeDirectoryPath, string libraryOutputDirectoryPath)
    {
        var cMakeDirectoryPathFull = _path.GetFullPath(cMakeDirectoryPath);
        if (!_directory.Exists(cMakeDirectoryPathFull))
        {
            LogCMakeDirectoryDoesNotExist(cMakeDirectoryPath);
            return false;
        }

        var libraryOutputDirectoryPathFull = _path.GetFullPath(libraryOutputDirectoryPath);
        if (!_directory.Exists(libraryOutputDirectoryPathFull))
        {
            _directory.CreateDirectory(libraryOutputDirectoryPathFull);
        }

        var outputDirectoryPath = Path.GetFullPath(_path.Combine(cMakeDirectoryPath, "bin"));

        var cMakeBuildDirectoryPath = _path.Combine(cMakeDirectoryPathFull, "cmake-build-release");
        if (_directory.Exists(cMakeBuildDirectoryPath))
        {
            _directory.Delete(_path.Combine(cMakeDirectoryPathFull, "cmake-build-release"), true);
        }

        if (_directory.Exists(outputDirectoryPath))
        {
            _directory.Delete(outputDirectoryPath, true);
        }

        var result = "cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release"
                .ExecuteShell(cMakeDirectoryPathFull, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            LogCMakeGenerationFailed(result.Output);
            return false;
        }

        result = "cmake --build cmake-build-release --config Release"
            .ExecuteShell(cMakeDirectoryPathFull, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            LogCMakeBuildFailed(result.Output);
            return false;
        }

        var dynamicLinkLibraryFileSearchPattern = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "*.dll",
            NativeOperatingSystem.macOS => "*.dylib",
            NativeOperatingSystem.Linux => "*.so",
            _ => "*.*"
        };

        var outputFilePaths = _directory.EnumerateFiles(outputDirectoryPath, dynamicLinkLibraryFileSearchPattern, SearchOption.AllDirectories);
        foreach (var outputFilePath in outputFilePaths)
        {
            var outputFileName = _path.GetFileName(outputFilePath);
            var outputFilePathCopy = _path.Combine(libraryOutputDirectoryPathFull, outputFileName);
            _file.Copy(outputFilePath, outputFilePathCopy, true);
            LogCMakeBuildSuccess(outputFilePathCopy);
        }

        return true;
    }

    [LoggerMessage(0, LogLevel.Error, "- The top level CMake directory does not exist: {DirectoryPath}")]
    private partial void LogCMakeDirectoryDoesNotExist(string directoryPath);

    [LoggerMessage(1, LogLevel.Error, "- Generating CMake build files failed: \n{Output}\n")]
    private partial void LogCMakeGenerationFailed(string output);

    [LoggerMessage(2, LogLevel.Error, "- CMake build failed: \n{Output}\n")]
    private partial void LogCMakeBuildFailed(string output);

    [LoggerMessage(3, LogLevel.Information, "- CMake build success. Copied output file to: {FilePath}")]
    private partial void LogCMakeBuildSuccess(string filePath);
}
