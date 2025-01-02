// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

#pragma warning disable CA1707

namespace C2CS.Tests.Functions.function_implicit_enum;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_implicit_enum";

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/functions/{FunctionName}");

        FunctionExists(ast);
        EnumExists(ast);
    }

    private void FunctionExists(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.GetFunction(FunctionName);
        _ = function.CallingConvention.Should().Be(CSharpTestCallingConvention.C);
        _ = function.ReturnType.Should().BeType("int", 4);

        _ = function.Parameters.Should().HaveCount(1);
        _ = function.Parameters[0].Should().BeParameterWithType("value", "int", 4);

        var @enum = ast.GetEnum("enum_implicit");
        _ = @enum.Values.Should().HaveCount(2);
        _ = @enum.BaseType.Should().Be("int");
    }

    private void EnumExists(CSharpTestAbstractSyntaxTree ast)
    {
        var @enum = ast.GetEnum("enum_implicit");
        _ = @enum.Values.Should().HaveCount(2);
        _ = @enum.BaseType.Should().Be("int");
    }
}
