// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

#pragma warning disable CA1707

namespace C2CS.Tests.Enums.enum_week_day;

public class Test : WriteCSharpCodeTest
{
    private const string EnumName = "enum_week_day";

    [Fact]
    public void Enum()
    {
        var ffi = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/enums/{EnumName}");
        EnumExists(ffi);
    }

    private void EnumExists(CSharpTestAbstractSyntaxTree ast)
    {
        var @enum = ast.GetEnum(EnumName);
        _ = @enum.BaseType.Should().Be("int");
        _ = @enum.SizeOf.Should().Be(4);

        _ = @enum.Values[0].Name.Should().Be("ENUM_WEEK_DAY_UNKNOWN");
        _ = @enum.Values[0].Value.Should().Be("-1");

        _ = @enum.Values[1].Name.Should().Be("ENUM_WEEK_DAY_MONDAY");
        _ = @enum.Values[1].Value.Should().Be("1");

        _ = @enum.Values[2].Name.Should().Be("ENUM_WEEK_DAY_TUESDAY");
        _ = @enum.Values[2].Value.Should().Be("2");

        _ = @enum.Values[3].Name.Should().Be("ENUM_WEEK_DAY_WEDNESDAY");
        _ = @enum.Values[3].Value.Should().Be("3");

        _ = @enum.Values[4].Name.Should().Be("ENUM_WEEK_DAY_THURSDAY");
        _ = @enum.Values[4].Value.Should().Be("4");

        _ = @enum.Values[5].Name.Should().Be("ENUM_WEEK_DAY_FRIDAY");
        _ = @enum.Values[5].Value.Should().Be("5");

        _ = @enum.Values[6].Name.Should().Be("_ENUM_WEEK_DAY_MAX");
        _ = @enum.Values[6].Value.Should().Be("6");
    }
}
