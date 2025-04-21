// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.GenerateCSharpCode;
using C2CS.Tests.Verify.Helpers;

namespace C2CS.Tests.Verify;

public class SettingsTests(FileSystemHelper fileSystemHelper, Tool tool) : VerifyHelpers(fileSystemHelper, tool)
{
    [Fact]
    private async Task FileScopedNamespacesDisabled()
    {
        var output = RunTool("config-generate-cs-file_scoped_namespaces.json");

        await VerifyOutput(output);
    }

    [Fact]
    private async Task DifferentNames()
    {
        var output = RunTool("config-generate-cs-different_names.json");

        await VerifyOutput(output);
    }
}
