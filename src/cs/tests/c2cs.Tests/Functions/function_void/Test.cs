// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common.Extensions;

namespace C2CS.Tests.Functions.function_void;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_void";

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
        _ = function.ReturnType.Should().BeTypeVoid();
        _ = function.Parameters.Should().BeEmpty();
    }
}
