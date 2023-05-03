// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO;
using System.IO.Abstractions;
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
        var cMakeDirectoryPathFull = _path.GetFullPath(input.CMakeDirectoryPath);
        if (!_directory.Exists(cMakeDirectoryPathFull))
        {
            LogCMakeDirectoryDoesNotExist(input.CMakeDirectoryPath);
            return false;
        }

        var libraryOutputDirectoryPathFull = _path.GetFullPath(input.OutputDirectoryPath);
        if (!_directory.Exists(libraryOutputDirectoryPathFull))
        {
            _directory.CreateDirectory(libraryOutputDirectoryPathFull);
        }

        var outputDirectoryPath = Path.GetFullPath(_path.Combine(input.CMakeDirectoryPath, "bin"));

        var cMakeBuildDirectoryPath = _path.Combine(cMakeDirectoryPathFull, "cmake-build-release");
        if (_directory.Exists(cMakeBuildDirectoryPath))
        {
            _directory.Delete(_path.Combine(cMakeDirectoryPathFull, "cmake-build-release"), true);
        }

        if (_directory.Exists(outputDirectoryPath))
        {
            _directory.Delete(outputDirectoryPath, true);
        }

        var cMakeArguments = new[]
        {
            "-DCMAKE_BUILD_TYPE=Release",
            $"-DCMAKE_ARCHIVE_OUTPUT_DIRECTORY={outputDirectoryPath}",
            $"-DCMAKE_LIBRARY_OUTPUT_DIRECTORY={outputDirectoryPath}",
            $"-DCMAKE_RUNTIME_OUTPUT_DIRECTORY={outputDirectoryPath}"
        };
        var cMakeArgumentsString = string.Join(" ", cMakeArguments);

        var cMakeGenerateBuildFilesCommand = $"cmake -S . -B cmake-build-release {cMakeArgumentsString}";
        LogCMakeGeneratingBuildFiles(cMakeGenerateBuildFilesCommand);
        var result = cMakeGenerateBuildFilesCommand
                .ExecuteShell(cMakeDirectoryPathFull, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            LogCMakeGeneratingBuildFilesFailed(result.Output);
            return false;
        }

        LogCMakeGeneratingBuildFilesSuccess();

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

        if (input.DeleteBuildFiles)
        {
            _directory.Delete(_path.Combine(cMakeDirectoryPathFull, "cmake-build-release"), true);
        }

        return true;
    }

    [LoggerMessage(0, LogLevel.Error, "- The top level CMake directory does not exist: {DirectoryPath}")]
    private partial void LogCMakeDirectoryDoesNotExist(string directoryPath);

    [LoggerMessage(1, LogLevel.Error, "- CMake build failed. Output: \n{Output}\n")]
    private partial void LogCMakeBuildFailed(string output);

    [LoggerMessage(2, LogLevel.Information, "- CMake build success. Copied output file to: {FilePath}")]
    private partial void LogCMakeBuildSuccess(string filePath);

    [LoggerMessage(3, LogLevel.Information, "- CMake generating build files. Command: {Command}")]
    private partial void LogCMakeGeneratingBuildFiles(string command);

    [LoggerMessage(4, LogLevel.Error, "- Generating CMake build files failed. Output: \n{Output}\n")]
    private partial void LogCMakeGeneratingBuildFilesFailed(string output);

    [LoggerMessage(5, LogLevel.Information, "- Generating CMake build files success.")]
    private partial void LogCMakeGeneratingBuildFilesSuccess();
}
