// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ExtractAbstractSyntaxTreeCFixture
{
    public readonly ImmutableDictionary<string, CFunction> FunctionsByName;
    public readonly ImmutableDictionary<string, CEnum> EnumsByName;

    public ExtractAbstractSyntaxTreeCFixture()
    {
        var request = new RequestExtractAbstractSyntaxTreeC
        {
            IsEnabledFindSdk = true,
            InputFilePath = "my_c_library/c/include/my_c_library.h",
            OutputFileDirectory = "my_c_library/c/ast",
            WorkingDirectory = "../../../../src/cs/tests/C2CS.Tests.Integration"
        };

        var useCase = new UseCase();
        var response = useCase.Execute(request);

        Assert.True(response.Status == UseCaseOutputStatus.Success);
        Assert.True(response.Diagnostics.Length == 0);

        var ast = response.AbstractSyntaxTree;
        Assert.True(ast != null);

        FunctionsByName = ast!.Functions.ToImmutableDictionary(x => x.Name, x => x);
        EnumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

        Assert.True(FunctionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "char*");
        Assert.True(function.Parameters.IsDefaultOrEmpty);
    }
}
