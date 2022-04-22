// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using C2CS.Data.Serialization;
using C2CS.Feature.ReadCodeC;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using C2CS.Tests.Common.Data.Model.C;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ReadCodeCFixture
{
    public readonly ImmutableArray<ReadCodeCFixtureContext> Contexts;

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
        Contexts = ParseAbstractSyntaxTrees(output, cJsonSerializer);
    }

    private ImmutableArray<ReadCodeCFixtureContext> ParseAbstractSyntaxTrees(
        ReadCodeCOutput output, CJsonSerializer cJsonSerializer)
    {
        if (!output.IsSuccessful || output.Diagnostics.Length != 0)
        {
            return ImmutableArray<ReadCodeCFixtureContext>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ReadCodeCFixtureContext>();

        foreach (var options in output.AbstractSyntaxTreesOptions)
        {
            if (string.IsNullOrEmpty(options.OutputFilePath))
            {
                continue;
            }

            var ast = cJsonSerializer.Read(options.OutputFilePath);
            var functions = TestFunctions(ast);
            var enums = TestEnums(ast);
            var structs = TestStructs(ast);

            var data = new ReadCodeCFixtureContext(
                ast.Platform,
                functions,
                enums,
                structs);

            builder.Add(data);
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, CTestFunction> TestFunctions(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestFunction>();

        foreach (var function in ast.Functions)
        {
            var result = TestFunction(function);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestFunction TestFunction(CFunction value)
    {
        var parameters = TestFunctionParameters(value.Parameters);

        var result = new CTestFunction
        {
            Name = value.Name,
#pragma warning disable CA1308
            CallingConvention = value.CallingConvention.ToString().ToLowerInvariant(),
#pragma warning restore CA1308
            ReturnTypeName = value.ReturnType,
            Parameters = parameters
        };
        return result;
    }

    private static ImmutableArray<CTestFunctionParameter> TestFunctionParameters(ImmutableArray<CFunctionParameter> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestFunctionParameter>();

        foreach (var value in values)
        {
            var result = TestFunctionParameter(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestFunctionParameter TestFunctionParameter(CFunctionParameter value)
    {
        var result = new CTestFunctionParameter
        {
            Name = value.Name,
            TypeName = value.Type
        };

        return result;
    }

    private static ImmutableDictionary<string, CTestEnum> TestEnums(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestEnum>();

        foreach (var @enum in ast.Enums)
        {
            var result = TestEnum(@enum);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestEnum TestEnum(CEnum value)
    {
        var values = TestEnumValues(value.Values);

        var result = new CTestEnum
        {
            Name = value.Name,
            IntegerType = value.IntegerType,
            Values = values
        };
        return result;
    }

    private static ImmutableArray<CTestEnumValue> TestEnumValues(ImmutableArray<CEnumValue> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestEnumValue>();

        foreach (var value in values)
        {
            var result = TestEnumValue(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestEnumValue TestEnumValue(CEnumValue value)
    {
        var result = new CTestEnumValue
        {
            Name = value.Name,
            Value = value.Value
        };
        return result;
    }

    private static ImmutableDictionary<string, CTestStruct> TestStructs(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestStruct>();

        foreach (var value in ast.Structs)
        {
            var result = TestStruct(value);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestStruct TestStruct(CStruct value)
    {
        var name = value.Name;
        var fields = TestStructFields(value.Fields);

        var result = new CTestStruct
        {
            Name = name,
            ParentName = value.ParentName,
            SizeOf = value.SizeOf,
            AlignOf = value.AlignOf,
            Fields = fields
        };

        return result;
    }

    private static ImmutableArray<CTestStructField> TestStructFields(ImmutableArray<CStructField> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestStructField>();

        foreach (var value in values)
        {
            var result = TestStructField(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestStructField TestStructField(CStructField value)
    {
        var result = new CTestStructField
        {
            Name = value.Name,
            TypeName = value.Type,
            OffsetOf = value.OffsetOf,
            PaddingOf = value.PaddingOf,
            SizeOf = value.SizeOf
        };

        return result;
    }

    public void AssertTargetPlatforms()
    {
        Assert.True(Contexts.Length > 0);

        foreach (var abstractSyntaxTree in Contexts)
        {
            var function = abstractSyntaxTree.GetFunction("pinvoke_get_platform_name");
            Assert.Equal("cdecl", function.CallingConvention);
            Assert.Equal("char*", function.ReturnTypeName);
            Assert.True(function.Parameters.IsDefaultOrEmpty);
        }
    }
}
