// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Foundation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests.C;

[PublicAPI]
public class TestCCode : TestBase
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

    [Fact]
    public void Reads()
    {
        Assert.True(_fixture.AbstractSyntaxTrees.Length != 0, "Failed to read C code.");
    }

    [Theory]
    [MemberData(nameof(Enums))]
    public void Enum(string name)
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            var value = ast.GetEnum(name);
            AssertValue(name, value, $"{ast.TargetPlatformRequested}/Enums");
        }
    }

    private readonly TestFixtureCCode _fixture;

    public TestCCode()
        : base("C/Data/Values", true)
    {
        _fixture = TestHost.Services.GetService<TestFixtureCCode>()!;
    }
}
