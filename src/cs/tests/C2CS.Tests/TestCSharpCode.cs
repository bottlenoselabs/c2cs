// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Foundation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests;

[PublicAPI]
public sealed class TestCSharpCode : TestBase
{
    public static TheoryData<string> EnumNames() => new()
    {
        "EnumForceSInt8",
        "EnumForceSInt16",
        "EnumForceSInt32",
        "EnumForceSInt64",

        "EnumForceUInt8",
        "EnumForceUInt16",
        "EnumForceUInt32",
        "EnumForceUInt64"
    };

    [Theory]
    [MemberData(nameof(EnumNames))]
    public void Enum(string name)
    {
        var value = _fixture.GetEnum(name);
        AssertValue(name, value, "Enums");
    }

    public static TheoryData<string> MacroObjectNames() => new()
    {
        "MACRO_OBJECT_INT_VALUE"
    };

    [Theory]
    [MemberData(nameof(MacroObjectNames))]
    public void MacroObject(string name)
    {
        var value = _fixture.GetMacroObject(name);
        AssertValue(name, value, "MacroObjects");
    }

    [Fact]
    public void Compiles()
    {
        _fixture.AssertCSharpCodeCompiles(_fixture.Output);
    }

    private readonly TestFixtureCSharpCode _fixture;

    public TestCSharpCode()
        : base("Data/Values", regenerateDataFiles: true)
    {
        var services = TestHost.Services;
        _fixture = services.GetService<TestFixtureCSharpCode>()!;
    }
}
