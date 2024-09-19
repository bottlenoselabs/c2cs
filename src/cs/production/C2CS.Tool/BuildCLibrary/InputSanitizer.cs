// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Linq;
using bottlenoselabs.Common.Tools;

namespace C2CS.BuildCLibrary;

public class InputSanitizer : ToolInputSanitizer<InputUnsanitized, InputSanitized>
{
    private readonly IPath _path;
    private readonly IDirectory _directory;

    public InputSanitizer(
        IFileSystem fileSystem)
    {
        _path = fileSystem.Path;
        _directory = fileSystem.Directory;
    }

    public override InputSanitized Sanitize(InputUnsanitized unsanitizedInput)
    {
        var cMakeDirectoryPath = CMakeDirectoryPath(unsanitizedInput);
        var outputDirectoryPath = OutputDirectoryPath(unsanitizedInput);
        var cMakeArguments = CMakeArguments(unsanitizedInput);
        var isEnabledDeleteBuildFiles = unsanitizedInput.IsEnabledDeleteBuildFiles ?? true;
        var isEnabledDebugBuild = unsanitizedInput.IsEnabledDebugBuild ?? false;

        var result = new InputSanitized
        {
            CMakeDirectoryPath = cMakeDirectoryPath,
            OutputDirectoryPath = outputDirectoryPath,
            CMakeArguments = cMakeArguments,
            IsEnabledDeleteBuildFiles = isEnabledDeleteBuildFiles,
            IsEnabledDebugBuild = isEnabledDebugBuild
        };
        return result;
    }

    private string CMakeDirectoryPath(InputUnsanitized unsanitizedInput)
    {
        var cMakeDirectoryPath = _path.GetFullPath(unsanitizedInput.CMakeDirectoryPath ?? string.Empty);
        if (!_directory.Exists(cMakeDirectoryPath))
        {
            throw new ToolInputSanitizationException(
                $"The CMake directory '{cMakeDirectoryPath}' does not exist.");
        }

        return cMakeDirectoryPath;
    }

    private string OutputDirectoryPath(InputUnsanitized unsanitizedInput)
    {
        var outputDirectoryPath = _path.GetFullPath(unsanitizedInput.OutputDirectoryPath ?? Environment.CurrentDirectory);
        if (!_directory.Exists(outputDirectoryPath))
        {
            _directory.CreateDirectory(outputDirectoryPath);
        }

        return outputDirectoryPath;
    }

    private ImmutableArray<string> CMakeArguments(InputUnsanitized unsanitizedInput)
    {
        return VerifyImmutableArray(unsanitizedInput.CMakeArguments);
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
