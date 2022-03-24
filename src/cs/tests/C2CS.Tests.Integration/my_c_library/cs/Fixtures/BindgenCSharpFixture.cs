// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.BindgenCSharp;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class BindgenCSharpFixture
{
    private readonly ExtractAbstractSyntaxTreeCFixture _fixture;

    public BindgenCSharpFixture(ExtractAbstractSyntaxTreeCFixture extractAbstractSyntaxTreeCFixture)
    {
        _fixture = extractAbstractSyntaxTreeCFixture;

        var request = new RequestBindgenCSharp
        {
            InputFileDirectory = "my_c_library/c/ast",
            OutputFilePath = "my_c_library/c/my_c_library.cs",
            WorkingDirectory = "../../../../src/cs/tests/C2CS.Tests.Integration"
        };

        var useCase = new UseCase();
        var response = useCase.Execute(request);

        Assert.True(response.Status == UseCaseOutputStatus.Success);
        Assert.True(response.Diagnostics.Length == 0);
    }
}
