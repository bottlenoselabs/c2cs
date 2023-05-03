// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Features.BuildCLibrary.Input.Unsanitized;
using C2CS.Foundation.Tool;

namespace C2CS.Features.BuildCLibrary.Input;

public class BuildCLibraryInputSanitizer : ToolInputSanitizer<BuildCLibraryOptions, BuildCLibraryInput>
{
    public override BuildCLibraryInput Sanitize(BuildCLibraryOptions unsanitizedInput)
    {
        var cMakeDirectoryPath = unsanitizedInput.CMakeDirectoryPath ?? string.Empty;
        var outputDirectoryPath = unsanitizedInput.OutputDirectoryPath ?? string.Empty;
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
