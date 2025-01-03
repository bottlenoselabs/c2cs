// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common.Extensions;

namespace C2CS.Tests.Functions.function_int_params_int;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_int_params_int";

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/functions/{FunctionName}");

        FunctionExists(ast);
    }

    private void FunctionExists(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.GetFunction(FunctionName);
        _ = function.CallingConvention.Should().Be(CSharpTestCallingConvention.C);
        _ = function.ReturnType.Should().BeType("int", 4);

        _ = function.Parameters.Length.Should().Be(1);
        _ = function.Parameters[0].Should().BeParameterWithType("a", "int", 4);
    }
}
