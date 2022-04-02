// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.Serialization;
using C2CS.Feature.ReadCodeC;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using C2CS.Serialization;
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

    public ExtractAbstractSyntaxTreeCFixture(
        ReadCodeCUseCase useCase,
        CJsonSerializer cJsonSerializer,
        ConfigurationJsonSerializer configurationJsonSerializer)
    {
        var configuration = configurationJsonSerializer.Read("my_c_library/config.json");
        var configurationReadC = configuration.ReadC;
        var output = useCase.Execute(configurationReadC!);
        AbstractSyntaxTrees = ParseAbstractSyntaxTrees(output, cJsonSerializer);
    }

    private ImmutableArray<AbstractSyntaxTreeFixtureData> ParseAbstractSyntaxTrees(
        ReadCodeCOutput output, CJsonSerializer cJsonSerializer)
    {
        if (!output.IsSuccessful || output.Diagnostics.Length != 0)
        {
            return ImmutableArray<AbstractSyntaxTreeFixtureData>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<AbstractSyntaxTreeFixtureData>();

        foreach (var options in output.AbstractSyntaxTreesOptions)
        {
            if (string.IsNullOrEmpty(options.OutputFilePath))
            {
                continue;
            }

            var ast = cJsonSerializer.Read(options.OutputFilePath);
            var functionsByName = ast.Functions.ToImmutableDictionary(x => x.Name, x => x);
            var enumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

            var data = new AbstractSyntaxTreeFixtureData
            {
                FunctionsByName = functionsByName,
                EnumsByName = enumsByName
            };

            builder.Add(data);
        }

        return builder.ToImmutable();
    }

    public void AssertPlatform()
    {
        foreach (var abstractSyntaxTree in AbstractSyntaxTrees!)
        {
            Assert.True(abstractSyntaxTree.FunctionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
            Assert.Equal(CFunctionCallingConvention.Cdecl, function!.CallingConvention);
            Assert.Equal("char*", function.ReturnType);
            Assert.True(function.Parameters.IsDefaultOrEmpty);
        }
    }
}
