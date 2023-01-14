// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;

namespace C2CS.Tests.Foundation.CMake;

public class CMakeLibraryBuilder
{
    private readonly IDirectory _directory;
    private readonly IPath _path;
    private readonly IFile _file;

    public CMakeLibraryBuilder(IFileSystem fileSystem)
    {
        _directory = fileSystem.Directory;
        _path = fileSystem.Path;
        _file = fileSystem.File;
    }

    public CCodeBuildResult BuildLibrary(string cMakeDirectoryPath, string libraryOutputDirectoryPath)
    {
        var cMakeDirectoryPathFull = _path.GetFullPath(cMakeDirectoryPath);
        if (!_directory.Exists(cMakeDirectoryPathFull))
        {
            return CCodeBuildResult.Failure;
        }

        var libraryOutputDirectoryPathFull = _path.GetFullPath(libraryOutputDirectoryPath);
        if (!_directory.Exists(libraryOutputDirectoryPathFull))
        {
            _directory.CreateDirectory(libraryOutputDirectoryPathFull);
        }

        var outputDirectoryPath = _path.Combine(cMakeDirectoryPath, "bin");

        var cMakeBuildDirectoryPath = _path.Combine(cMakeDirectoryPathFull, "cmake-build-release");
        if (_directory.Exists(cMakeBuildDirectoryPath))
        {
            _directory.Delete(_path.Combine(cMakeDirectoryPathFull, "cmake-build-release"), true);
        }

        if (_directory.Exists(outputDirectoryPath))
        {
            _directory.Delete(outputDirectoryPath, true);
        }

        var result =
            $"cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release"
                .ExecuteShell(cMakeDirectoryPathFull, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            Console.Write(result.Output);
            return CCodeBuildResult.Failure;
        }

        result = "cmake --build cmake-build-release --config Release"
            .ExecuteShell(cMakeDirectoryPathFull, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            Console.Write(result.Output);
            return CCodeBuildResult.Failure;
        }

        var outputFilePaths = _directory.EnumerateFiles(outputDirectoryPath, "*.*");
        foreach (var outputFilePath in outputFilePaths)
        {
            var outputFileName = _path.GetFileName(outputFilePath);
            var outputFilePathCopy = _path.Combine(libraryOutputDirectoryPathFull, outputFileName);
            _file.Copy(outputFilePath, outputFilePathCopy, true);
        }

        var buildResult = new CCodeBuildResult(true);
        return buildResult;
    }
}
