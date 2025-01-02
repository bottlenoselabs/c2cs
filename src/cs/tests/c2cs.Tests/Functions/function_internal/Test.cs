// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Functions.function_internal;

public class Test : WriteCSharpCodeTest
{
    private const string FunctionName = "function_internal";

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree(
            $"src/c/tests/functions/{FunctionName}");

        Function1DoesNotExist(ast);
        Function2DoesExist(ast);
        Function3DoesNotExist(ast);
        Function4DoesNotExist(ast);
        Function5DoesExist(ast);
    }

    private void Function1DoesNotExist(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.TryGetFunction("function_internal_1");
        _ = function.Should().BeNull();
    }

    private void Function2DoesExist(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.TryGetFunction("function_internal_2");
        _ = function.Should().NotBeNull();
    }

    private void Function3DoesNotExist(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.TryGetFunction("function_internal_3");
        _ = function.Should().BeNull();
    }

    private void Function4DoesNotExist(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.TryGetFunction("function_internal_4");
        _ = function.Should().BeNull();
    }

    private void Function5DoesExist(CSharpTestAbstractSyntaxTree ast)
    {
        var function = ast.TryGetFunction("function_internal_5");
        _ = function.Should().NotBeNull();
    }
}
