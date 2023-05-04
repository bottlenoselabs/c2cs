// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Linq;
using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Features.BuildCLibrary.Input.Unsanitized;
using C2CS.Foundation.Tool;

namespace C2CS.Features.BuildCLibrary.Input;

public class BuildCLibraryInputSanitizer : ToolInputSanitizer<BuildCLibraryOptions, BuildCLibraryInput>
{
    private readonly IPath _path;
    private readonly IDirectory _directory;

    public BuildCLibraryInputSanitizer(
        IFileSystem fileSystem)
    {
        _path = fileSystem.Path;
        _directory = fileSystem.Directory;
    }

    public override BuildCLibraryInput Sanitize(BuildCLibraryOptions unsanitizedInput)
    {
        var cMakeDirectoryPath = CMakeDirectoryPath(unsanitizedInput);
        var outputDirectoryPath = OutputDirectoryPath(unsanitizedInput);
        var cMakeArguments = CMakeArguments(unsanitizedInput);
        var deleteBuildFiles = DeleteBuildFiles(unsanitizedInput);

        var result = new BuildCLibraryInput
        {
            CMakeDirectoryPath = cMakeDirectoryPath,
            OutputDirectoryPath = outputDirectoryPath,
            CMakeArguments = cMakeArguments,
            DeleteBuildFiles = deleteBuildFiles
        };
        return result;
    }

    private string CMakeDirectoryPath(BuildCLibraryOptions unsanitizedInput)
    {
        var cMakeDirectoryPath = _path.GetFullPath(unsanitizedInput.CMakeDirectoryPath ?? string.Empty);
        if (!_directory.Exists(cMakeDirectoryPath))
        {
            throw new ToolInputSanitizationException(
                $"The CMake directory '{cMakeDirectoryPath}' does not exist.");
        }

        return cMakeDirectoryPath;
    }

    private string OutputDirectoryPath(BuildCLibraryOptions unsanitizedInput)
    {
        var outputDirectoryPath = _path.GetFullPath(unsanitizedInput.OutputDirectoryPath ?? Environment.CurrentDirectory);
        if (!_directory.Exists(outputDirectoryPath))
        {
            _directory.CreateDirectory(outputDirectoryPath);
        }

        return outputDirectoryPath;
    }

    private ImmutableArray<string> CMakeArguments(BuildCLibraryOptions unsanitizedInput)
    {
        return VerifyImmutableArray(unsanitizedInput.CMakeArguments);
    }

    private static bool DeleteBuildFiles(BuildCLibraryOptions unsanitizedInput)
    {
        return unsanitizedInput.DeleteBuildFiles ?? true;
    }

    private static ImmutableArray<string> VerifyImmutableArray(ImmutableArray<string?>? array)
    {
        if (array == null || array.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var result = array.Value
            .Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
        return result;
    }
}
