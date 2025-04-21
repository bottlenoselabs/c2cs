// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.GenerateCSharpCode;
using c2cs.Tests.Verify;

namespace C2CS.Tests.Verify;

public class SettingsTests(Tool tool)
{
    private readonly Tool _tool = tool;

    [Fact]
    private async Task FileScopedNamespacesDisabled()
    {
        var output = _tool.Run("configs/config-generate-cs-file_scoped_namespaces.json");

        await VerifyHelpers.VerifyOutput(output);
    }

    [Fact]
    private async Task DifferentNames()
    {
        var output = _tool.Run("configs/config-generate-cs-different_names.json");

        await VerifyHelpers.VerifyOutput(output);
    }
}
