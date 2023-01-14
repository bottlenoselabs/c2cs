// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Linq;
using C2CS.Data.C.Model;
using C2CS.Data.C.Serialization;
using C2CS.ReadCodeC;
using C2CS.ReadCodeC.Data.Models;
using C2CS.Tests.C.Data.Models;
using C2CS.Tests.Foundation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests.C;

[PublicAPI]
public class TestCCode : TestBase
{
    public static TheoryData<string> Enums() => new()
    {
        "EnumForceUInt32"
    };

    [Theory]
    [MemberData(nameof(Enums))]
    public void Enum(string name)
    {
        foreach (var ast in AbstractSyntaxTrees)
        {
            var value = ast.GetEnum(name);
            AssertValue(name, value, $"{ast.TargetPlatformRequested}/Enums");
        }
    }

    private readonly FeatureReadCodeC _feature;
    private readonly IReaderCCode _readerCCode;
    private readonly CJsonSerializer _jsonSerializer;
    private readonly IFileSystem _fileSystem;

    private readonly TestFixtureCCode _fixture;

    public ImmutableArray<TestCCodeAbstractSyntaxTree> AbstractSyntaxTrees => _fixture.AbstractSyntaxTrees;

    public TestCCode()
        : base("C/Data/Values", false)
    {
        var services = TestHost.Services;

        _feature = services.GetService<FeatureReadCodeC>()!;
        _jsonSerializer = services.GetService<CJsonSerializer>()!;
        _readerCCode = services.GetService<IReaderCCode>()!;
        _fileSystem = services.GetService<IFileSystem>()!;

        _fixture = GetFixture();
    }

    private TestFixtureCCode GetFixture()
    {
        var abstractSyntaxTrees = GetAbstractSyntaxTrees();
        Assert.True(abstractSyntaxTrees.Length > 0, "Failed to read C code.");

        var fixture = new TestFixtureCCode(abstractSyntaxTrees);
        return fixture;
    }

    private ImmutableArray<TestCCodeAbstractSyntaxTree> GetAbstractSyntaxTrees()
    {
        var output = _feature.Execute(_readerCCode.Options);

        if (!output.IsSuccess ||
            output.Diagnostics.Any(x => x.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Panic))
        {
            return ImmutableArray<TestCCodeAbstractSyntaxTree>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<TestCCodeAbstractSyntaxTree>();

        foreach (var input in output.AbstractSyntaxTrees)
        {
            var abstractSyntaxTree = GetAbstractSyntaxTree(input);
            builder.Add(abstractSyntaxTree);
        }

        return builder.ToImmutable();
    }

    private TestCCodeAbstractSyntaxTree GetAbstractSyntaxTree(ReadCodeCInputAbstractSyntaxTree input)
    {
        var ast = _jsonSerializer.Read(input.OutputFilePath);
        var functions = CreateTestFunctions(ast);
        var enums = CreateTestEnums(ast);
        var structs = CreateTestRecords(ast);
        var macroObjects = CreateTestMacroObjects(ast);

        var data = new TestCCodeAbstractSyntaxTree(
            input.ExplorerOptions,
            input.ParseOptions,
            ast.PlatformRequested,
            ast.PlatformActual,
            functions,
            enums,
            structs,
            macroObjects);
        return data;
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
            SizeOf = value.TypeInfo.SizeOf
        };

        return result;
    }

    private ImmutableDictionary<string, CTestMacroObject> CreateTestMacroObjects(CAbstractSyntaxTree ast)
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
