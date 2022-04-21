// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.Serialization;
using C2CS.Feature.ReadCodeC;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ReadCodeCFixture
{
    public readonly ImmutableArray<AbstractSyntaxTreeFixtureData> AbstractSyntaxTrees;

    public sealed class AbstractSyntaxTreeFixtureData
    {
        private readonly ImmutableDictionary<string, CFunction> _functionsByName;
        private readonly ImmutableDictionary<string, CEnum> _enumsByName;
        private readonly ImmutableDictionary<string, CStruct> _structsByName;

        public AbstractSyntaxTreeFixtureData(
            ImmutableDictionary<string, CFunction> functionsByName,
            ImmutableDictionary<string, CEnum> enumsByName,
            ImmutableDictionary<string, CStruct> structsByName)
        {
            _functionsByName = functionsByName;
            _enumsByName = enumsByName;
            _structsByName = structsByName;
        }

        public CFunction GetFunction(string name)
        {
            var exists = _functionsByName.TryGetValue(name, out var value);
            Assert.True(exists, $"The function `{name}` does not exist.");
            return value!;
        }

        public CEnum GetEnum(string name)
        {
            var exists = _enumsByName.TryGetValue(name, out var value);
            Assert.True(exists, $"The enum `{name}` does not exist.");
            return value!;
        }

        public CStruct GetStruct(string name)
        {
            var exists = _structsByName.TryGetValue(name, out var value);
            Assert.True(exists, $"The struct `{name}` does not exist.");
            return value!;
        }
    }

    public ReadCodeCFixture(
        ReadCodeCUseCase useCase,
        CJsonSerializer cJsonSerializer,
        ConfigurationJsonSerializer configurationJsonSerializer)
    {
#pragma warning disable CA1308
        var os = Native.OperatingSystem.ToString().ToLowerInvariant();
#pragma warning restore CA1308
        var configurationFilePath = $"c/tests/my_c_library/config_{os}.json";
        var configuration = configurationJsonSerializer.Read(configurationFilePath);
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
            var structsByName = ast.Structs.ToImmutableDictionary(x => x.Name);

            var data = new AbstractSyntaxTreeFixtureData(
                functionsByName, enumsByName, structsByName);

            builder.Add(data);
        }

        return builder.ToImmutable();
    }

    public void AssertPlatform()
    {
        Assert.True(AbstractSyntaxTrees.Length > 0);

        foreach (var abstractSyntaxTree in AbstractSyntaxTrees)
        {
            var function = abstractSyntaxTree.GetFunction("pinvoke_get_platform_name");
            Assert.Equal(CFunctionCallingConvention.Cdecl, function.CallingConvention);
            Assert.Equal("char*", function.ReturnType);
            Assert.True(function.Parameters.IsDefaultOrEmpty);
        }
    }
}
