// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.GenerateCSharpCode;
using C2CS.Tests.Verify.Helpers;

namespace C2CS.Tests.Verify;

public sealed class EnumTests(FileSystemHelper fileSystemHelper, Tool tool) : VerifyHelpers(fileSystemHelper, tool)
{
    [Fact]
    public async Task EnumUInt8()
    {
        var output = RunTool("enums/enum_uint8", "config-generate-cs-enum_uint8.json");

        await VerifyOutput(output);
    }

    [Fact]
    public async Task EnumWeekDay()
    {
        var output = RunTool("enums/enum_week_day", "config-generate-cs-enum_week_day.json");

        await VerifyOutput(output);
    }
}
