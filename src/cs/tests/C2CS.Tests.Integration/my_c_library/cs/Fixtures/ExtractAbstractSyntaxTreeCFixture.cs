// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ExtractAbstractSyntaxTreeCFixture
{
    public readonly ImmutableDictionary<string, CFunction> FunctionsByName;
    public readonly ImmutableDictionary<string, CEnum> EnumsByName;

    public ExtractAbstractSyntaxTreeOutput Output { get; }

    public ExtractAbstractSyntaxTreeCFixture(
        ExtractAbstractSyntaxTreeUseCase useCase,
        CJsonSerializer cJsonSerializer,
        ConfigurationJsonSerializer configurationJsonSerializer)
    {
        var configuration = configurationJsonSerializer.Read("my_c_library/config.json");
        var request = configuration.ExtractAbstractSyntaxTreeC;

        var output = Output = useCase.Execute(request);
        var input = output.Input;

        Assert.True(output.IsSuccessful);
        Assert.True(output.Diagnostics.Length == 0);

        Assert.True(input.OutputFilePath != null);
        var ast = cJsonSerializer.Read(input.OutputFilePath!);
        Assert.True(ast != null);

        FunctionsByName = ast!.Functions.ToImmutableDictionary(x => x.Name, x => x);
        EnumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

        Assert.True(FunctionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "char*");
        Assert.True(function.Parameters.IsDefaultOrEmpty);
    }
}
