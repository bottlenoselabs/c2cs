// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO.Abstractions;
using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Features.BuildCLibrary.Input.Unsanitized;
using C2CS.Foundation.Tool;

namespace C2CS.Features.BuildCLibrary.Input;

public class BuildCLibraryInputSanitizer : ToolInputSanitizer<BuildCLibraryOptions, BuildCLibraryInput>
{
    private readonly IPath _path;
    private readonly IFile _file;
    private readonly IDirectory _directory;

    public BuildCLibraryInputSanitizer(
        IFileSystem fileSystem)
    {
        _path = fileSystem.Path;
        _file = fileSystem.File;
        _directory = fileSystem.Directory;
    }

    public override BuildCLibraryInput Sanitize(BuildCLibraryOptions unsanitizedInput)
    {
        var cMakeDirectoryPath = _path.GetFullPath(unsanitizedInput.CMakeDirectoryPath ?? string.Empty);
        if (!_directory.Exists(cMakeDirectoryPath))
        {
            throw new ToolInputSanitizationException(
                $"The CMake directory '{cMakeDirectoryPath}' does not exist.");
        }

        var outputDirectoryPath = _path.GetFullPath(unsanitizedInput.OutputDirectoryPath ?? Environment.CurrentDirectory);
        if (!_directory.Exists(outputDirectoryPath))
        {
            _directory.CreateDirectory(outputDirectoryPath);
        }

        var deleteBuildFiles = unsanitizedInput.DeleteBuildFiles ?? true;

        var result = new BuildCLibraryInput
        {
            CMakeDirectoryPath = cMakeDirectoryPath,
            OutputDirectoryPath = outputDirectoryPath,
            DeleteBuildFiles = deleteBuildFiles
        };
        return result;
    }
}
