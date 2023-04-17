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
    public static TheoryData<string> Enums() => new()
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
    [MemberData(nameof(Enums))]
    public void Enum(string name)
    {
        var value = _fixture.GetEnum(name);
        AssertValue(name, value, "Enums");
    }

    [Fact]
    public void Compiles()
    {
        _fixture.AssertCSharpCodeCompiles();
        _fixture.AssertCSharpCodePreJustInTimeCompilesAtRuntime();
    }

    private readonly TestFixtureCSharpCode _fixture;

    public TestCSharpCode()
        : base("CSharp/Data/Values", true)
    {
        var services = TestHost.Services;
        _fixture = services.GetService<TestFixtureCSharpCode>()!;
    }
}
