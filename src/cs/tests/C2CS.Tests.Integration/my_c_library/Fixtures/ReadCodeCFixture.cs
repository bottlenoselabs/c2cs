// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Linq;
using C2CS.Data.Serialization;
using C2CS.Feature.ReadCodeC;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using C2CS.Foundation.Diagnostics;
using C2CS.Tests.Common.Data.Model.C;

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
        Contexts = GetContexts(output, cJsonSerializer);
    }

    private ImmutableArray<ReadCodeCFixtureContext> GetContexts(
        ReadCodeCOutput output, CJsonSerializer jsonSerializer)
    {
        if (!output.IsSuccessful ||
            output.Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Panic))
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

            var ast = jsonSerializer.Read(options.OutputFilePath);
            var functions = CreateTestFunctions(ast);
            var enums = CreateTestEnums(ast);
            var structs = CreateTestRecords(ast);

            var data = new ReadCodeCFixtureContext(
                ast.Platform,
                functions,
                enums,
                structs);

            builder.Add(data);
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, CTestFunction> CreateTestFunctions(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestFunction>();

        foreach (var function in ast.Functions)
        {
            var result = CreateTestFunction(function);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestFunction CreateTestFunction(CFunction value)
    {
        var parameters = CreateTestFunctionParameters(value.Parameters);

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

    private static ImmutableArray<CTestFunctionParameter> CreateTestFunctionParameters(
        ImmutableArray<CFunctionParameter> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestFunctionParameter>();

        foreach (var value in values)
        {
            var result = CreateTestFunctionParameter(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestFunctionParameter CreateTestFunctionParameter(CFunctionParameter value)
    {
        var result = new CTestFunctionParameter
        {
            Name = value.Name,
            TypeName = value.Type
        };

        return result;
    }

    private static ImmutableDictionary<string, CTestEnum> CreateTestEnums(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestEnum>();

        foreach (var @enum in ast.Enums)
        {
            var result = CreateTestEnum(@enum);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestEnum CreateTestEnum(CEnum value)
    {
        var values = CreateTestEnumValues(value.Values);

        var result = new CTestEnum
        {
            Name = value.Name,
            IntegerType = value.IntegerType,
            Values = values
        };
        return result;
    }

    private static ImmutableArray<CTestEnumValue> CreateTestEnumValues(ImmutableArray<CEnumValue> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestEnumValue>();

        foreach (var value in values)
        {
            var result = CreateTestEnumValue(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestEnumValue CreateTestEnumValue(CEnumValue value)
    {
        var result = new CTestEnumValue
        {
            Name = value.Name,
            Value = value.Value
        };
        return result;
    }

    private static ImmutableDictionary<string, CTestRecord> CreateTestRecords(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestRecord>();

        foreach (var value in ast.Structs)
        {
            var result = CreateTestRecordStruct(value);
            builder.Add(result.Name, result);
        }

        foreach (var value in ast.Unions)
        {
            var result = CreateTestRecordUnion(value);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestRecord CreateTestRecordStruct(CStruct value)
    {
        var name = value.Name;
        var fields = CreateTestStructFields(value.Fields);

        var result = new CTestRecord
        {
            Name = name,
            ParentName = value.ParentName,
            SizeOf = value.SizeOf,
            AlignOf = value.AlignOf,
            Fields = fields,
            IsUnion = false
        };

        return result;
    }

    private static ImmutableArray<CTestRecordField> CreateTestStructFields(ImmutableArray<CStructField> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestRecordField>();

        foreach (var value in values)
        {
            var result = CreateTestRecordField(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestRecordField CreateTestRecordField(CStructField value)
    {
        var result = new CTestRecordField
        {
            Name = value.Name,
            TypeName = value.Type,
            OffsetOf = value.OffsetOf,
            PaddingOf = value.PaddingOf,
            SizeOf = value.SizeOf
        };

        return result;
    }

    private static CTestRecord CreateTestRecordUnion(CUnion value)
    {
        var name = value.Name;
        var fields = CreateTestUnionFields(value.Fields);

        var result = new CTestRecord
        {
            Name = name,
            ParentName = value.ParentName,
            SizeOf = value.SizeOf,
            AlignOf = value.AlignOf,
            Fields = fields,
            IsUnion = false
        };

        return result;
    }

    private static ImmutableArray<CTestRecordField> CreateTestUnionFields(ImmutableArray<CUnionField> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestRecordField>();

        foreach (var value in values)
        {
            var result = CreateTestUnionField(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestRecordField CreateTestUnionField(CUnionField value)
    {
        var result = new CTestRecordField
        {
            Name = value.Name,
            TypeName = value.Type,
            OffsetOf = 0,
            PaddingOf = 0,
            SizeOf = value.SizeOf
        };

        return result;
    }
}
