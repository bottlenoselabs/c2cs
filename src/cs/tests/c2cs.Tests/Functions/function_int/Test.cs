// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Functions.function_int;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_int";

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/functions/{FunctionName}");

        FfiFunctionExists(ast);
    }

    private void FfiFunctionExists(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.GetFunction(FunctionName);
        _ = function.CallingConvention.Should().Be(CSharpTestCallingConvention.C);
        _ = function.ReturnType.Should().BeType("int", 4);
        _ = function.Parameters.Should().BeEmpty();
    }
}
