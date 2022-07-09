// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Linq;
using C2CS.Contexts.ReadCodeC;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Data.Serialization;
using C2CS.Contexts.ReadCodeC.Domain;
using C2CS.Data.Serialization;
using C2CS.Foundation.Diagnostics;
using C2CS.Tests.Common;
using C2CS.Tests.Common.Data.Model.C;
using JetBrains.Annotations;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

[PublicAPI]
public sealed class ReadCodeCFixture : TestFixture
{
    public readonly ImmutableArray<ReadCodeCFixtureContext> Contexts;

    public ReadCodeCFixture(
        ReadCodeCUseCase useCase,
        CJsonSerializer cJsonSerializer,
        BindgenConfigurationJsonSerializer configurationJsonSerializer)
    {
#pragma warning disable CA1308
        var os = Native.OperatingSystem.ToString().ToLowerInvariant();
#pragma warning restore CA1308
        var configurationFilePath = $"c/tests/my_c_library/config_{os}.json";
        var configuration = configurationJsonSerializer.Read(configurationFilePath);
        var configurationReadC = configuration.ReadCCode;
        var output = useCase.Execute(configurationReadC!);
        Contexts = GetContexts(output, cJsonSerializer);
    }

    private ImmutableArray<ReadCodeCFixtureContext> GetContexts(
        ReadCodeCOutput output, CJsonSerializer jsonSerializer)
    {
        if (!output.IsSuccess ||
            output.Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Panic))
        {
            return ImmutableArray<ReadCodeCFixtureContext>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<ReadCodeCFixtureContext>();

        foreach (var input in output.AbstractSyntaxTreesOptions)
        {
            if (string.IsNullOrEmpty(input.OutputFilePath))
            {
                continue;
            }

            var ast = jsonSerializer.Read(input.OutputFilePath);
            var functions = CreateTestFunctions(ast);
            var enums = CreateTestEnums(ast);
            var structs = CreateTestRecords(ast);
            var macroObjects = CreateMacroObjects(ast);

            var data = new ReadCodeCFixtureContext(
                input.ExplorerOptions,
                input.ParseOptions,
                ast.PlatformRequested,
                ast.PlatformActual,
                functions,
                enums,
                structs,
                macroObjects);

            builder.Add(data);
        }

        return builder.ToImmutable();
    }

    private static ImmutableDictionary<string, CTestFunction> CreateTestFunctions(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestFunction>();

        foreach (var function in ast.Functions.Values)
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
            ReturnTypeName = value.ReturnTypeInfo.Name,
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
            TypeName = value.TypeInfo.Name
        };

        return result;
    }

    private static ImmutableDictionary<string, CTestEnum> CreateTestEnums(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestEnum>();

        foreach (var @enum in ast.Enums.Values)
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
            IntegerType = value.IntegerTypeInfo.Name,
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

        foreach (var value in ast.Records.Values)
        {
            var result = CreateTestRecord(value);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private static CTestRecord CreateTestRecord(CRecord value)
    {
        var name = value.Name;
        var fields = CreateTestRecordFields(value.Fields);

        var result = new CTestRecord
        {
            Name = name,
            SizeOf = value.SizeOf,
            AlignOf = value.AlignOf,
            Fields = fields,
            IsUnion = false
        };

        return result;
    }

    private static ImmutableArray<CTestRecordField> CreateTestRecordFields(ImmutableArray<CRecordField> values)
    {
        var builder = ImmutableArray.CreateBuilder<CTestRecordField>();

        foreach (var value in values)
        {
            var result = CreateTestRecordField(value);
            builder.Add(result);
        }

        return builder.ToImmutable();
    }

    private static CTestRecordField CreateTestRecordField(CRecordField value)
    {
        var result = new CTestRecordField
        {
            Name = value.Name,
            TypeName = value.TypeInfo.Name,
            OffsetOf = value.OffsetOf,
            PaddingOf = value.PaddingOf ?? 0,
            SizeOf = value.TypeInfo.SizeOf
        };

        return result;
    }

    private ImmutableDictionary<string, CTestMacroObject> CreateMacroObjects(CAbstractSyntaxTree ast)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, CTestMacroObject>();

        foreach (var value in ast.MacroObjects.Values)
        {
            var result = CreateMacroObject(value);
            builder.Add(result.Name, result);
        }

        return builder.ToImmutable();
    }

    private CTestMacroObject CreateMacroObject(CMacroObject value)
    {
        var result = new CTestMacroObject
        {
            Name = value.Name,
            TypeName = value.Type.Name,
            Value = value.Value
        };

        return result;
    }
}
