// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.BindgenCSharp;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class BindgenCSharpFixture
{
    public BindgenCSharpFixture(
        BindgenUseCase useCase,
        ExtractAbstractSyntaxTreeCFixture ast)
    {
        Assert.True(ast.Response.IsSuccessful);
        Assert.True(ast.Response.Diagnostics.Length == 0);

        var request = new BindgenRequest
        {
            InputFileDirectory = "my_c_library/c/ast",
            OutputFilePath = "my_c_library/c/my_c_library.cs"
        };

        var response = useCase.Execute(request);

        Assert.True(response.IsSuccessful);
        Assert.True(response.Diagnostics.Length == 0);
    }
}
