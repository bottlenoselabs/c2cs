// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Functions.function_ignored;

public class Test : WriteCSharpCodeTest
{
    private readonly string[] _functionNamesThatShouldExist =
    [
        "function_allowed"
    ];

    private readonly string[] _functionNamesThatShouldNotExist =
    [
        "function_not_allowed",
        "function_ignored_1",
        "function_ignored_2"
    ];

    [Fact]
    public void Function()
    {
        var ast = GetCSharpAbstractSyntaxTree("src/c/tests/functions/function_ignored");

        FunctionsExist(ast, _functionNamesThatShouldExist);
        FunctionsDoNotExist(ast, _functionNamesThatShouldNotExist);
    }

    private void FunctionsExist(CSharpTestAbstractSyntaxTree ast, params string[] names)
    {
        foreach (var name in names)
        {
            var function = ast.TryGetFunction(name);
            _ = function.Should().NotBeNull();
        }
    }

    private void FunctionsDoNotExist(CSharpTestAbstractSyntaxTree ast, params string[] names)
    {
        foreach (var name in names)
        {
            var function = ast.TryGetFunction(name);
            _ = function.Should().BeNull();
        }
    }
}
