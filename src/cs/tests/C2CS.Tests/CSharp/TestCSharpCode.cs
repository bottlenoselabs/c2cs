// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Foundation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests.CSharp;

[PublicAPI]
public sealed class TestCSharpCode : TestBase
{
    private readonly TestFixtureCSharpCode _fixture;

    public static TheoryData<string> Enums() => new()
    {
        "EnumForceSInt8",
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
        _fixture.AssertCompiles();
    }

    public TestCSharpCode()
        : base("CSharp/Data/Values", true)
    {
        var services = TestHost.Services;
        _fixture = services.GetService<TestFixtureCSharpCode>()!;
    }
}
