// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ExtractAbstractSyntaxTreeCFixture
{
    public readonly ImmutableDictionary<string, CFunction> FunctionsByName;
    public readonly ImmutableDictionary<string, CEnum> EnumsByName;

    public ExtractAbstractSyntaxTreeResponse Response { get; }

    public ExtractAbstractSyntaxTreeCFixture(ExtractAbstractSyntaxTreeUseCase useCase, CJsonSerializer cJsonSerializer)
    {
        var request = new ExtractAbstractSyntaxTreeRequest
        {
            IsEnabledFindSdk = true,
            InputFilePath = "my_c_library/c/include/my_c_library.h",
            OutputFileDirectory = "my_c_library/c/ast"
        };

        Response = useCase.Execute(request);

        Assert.True(Response.IsSuccessful);
        Assert.True(Response.Diagnostics.Length == 0);

        Assert.True(Response.FilePath != null);
        var ast = cJsonSerializer.Read(Response.FilePath!);
        Assert.True(ast != null);

        FunctionsByName = ast!.Functions.ToImmutableDictionary(x => x.Name, x => x);
        EnumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

        Assert.True(FunctionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "char*");
        Assert.True(function.Parameters.IsDefaultOrEmpty);
    }
}
