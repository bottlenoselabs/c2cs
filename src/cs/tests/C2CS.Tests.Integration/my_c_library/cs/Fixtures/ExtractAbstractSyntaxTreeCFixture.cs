// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ExtractAbstractSyntaxTreeCFixture
{
    public readonly ImmutableArray<AbstractSyntaxTreeFixtureData> AbstractSyntaxTrees;

    public sealed class AbstractSyntaxTreeFixtureData
    {
        public ImmutableDictionary<string, CFunction> FunctionsByName { get; init; } = ImmutableDictionary<string, CFunction>.Empty;

        public ImmutableDictionary<string, CEnum> EnumsByName { get; init; } = ImmutableDictionary<string, CEnum>.Empty;
    }

    public ExtractOutput Output { get; }

    public ExtractAbstractSyntaxTreeCFixture(
        ExtractUseCase useCase,
        CJsonSerializer cJsonSerializer,
        ConfigurationJsonSerializer configurationJsonSerializer)
    {
        var configuration = configurationJsonSerializer.Read("my_c_library/config.json");
        var request = configuration.ExtractC;
        Assert.True(request != null);

        var output = Output = useCase.Execute(request!);
        var input = output.Input;

        Assert.True(output.IsSuccessful);
        Assert.True(output.Diagnostics.Length == 0);

        var builder = ImmutableArray.CreateBuilder<AbstractSyntaxTreeFixtureData>();

        foreach (var inputAbstractSyntaxTree in input.InputAbstractSyntaxTrees)
        {
            Assert.True(inputAbstractSyntaxTree.OutputFilePath != null);
            var ast = cJsonSerializer.Read(inputAbstractSyntaxTree.OutputFilePath!);
            Assert.True(ast != null);

            var functionsByName = ast!.Functions.ToImmutableDictionary(x => x.Name, x => x);
            var enumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

            Assert.True(functionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
            Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(function.ReturnType == "char*");
            Assert.True(function.Parameters.IsDefaultOrEmpty);

            var data = new AbstractSyntaxTreeFixtureData
            {
                FunctionsByName = functionsByName,
                EnumsByName = enumsByName
            };

            builder.Add(data);
        }

        AbstractSyntaxTrees = builder.ToImmutable();
    }
}
