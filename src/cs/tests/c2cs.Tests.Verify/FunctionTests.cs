// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.GenerateCSharpCode;
using c2cs.Tests.Verify;

namespace C2CS.Tests.Verify;

public sealed class FunctionTests(Tool tool)
{
    private readonly Tool _tool = tool;

    [Fact]
    public async Task EnumUInt8()
    {
        var output = _tool.Run("configs/config-generate-cs-enum_uint8.json");

        await VerifyHelpers.VerifyOutput(output);
    }

    [Fact]
    public async Task EnumWeekDay()
    {
        var output = _tool.Run("configs/config-generate-cs-enum_week_day.json");

        await VerifyHelpers.VerifyOutput(output);
    }
}
