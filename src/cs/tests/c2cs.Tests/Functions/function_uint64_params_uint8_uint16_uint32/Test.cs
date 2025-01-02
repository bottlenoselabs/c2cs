// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

#pragma warning disable CA1707

namespace C2CS.Tests.Functions.function_uint64_params_uint8_uint16_uint32;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_uint64_params_uint8_uint16_uint32";

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/functions/{FunctionName}");
        FunctionExists(ast);
    }

    private static void FunctionExists(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.GetFunction(FunctionName);
        _ = function.CallingConvention.Should().Be(CSharpTestCallingConvention.C);
        _ = function.ReturnType.Should().BeType("ulong", 8);

        _ = function.Parameters.Length.Should().Be(3);
        _ = function.Parameters[0].Should().BeParameterWithType("a", "byte", 1);
        _ = function.Parameters[1].Should().BeParameterWithType("b", "ushort", 2);
        _ = function.Parameters[2].Should().BeParameterWithType("c", "uint", 4);
    }
}
