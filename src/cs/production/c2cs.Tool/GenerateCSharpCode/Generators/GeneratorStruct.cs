// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorNodeStruct(ILogger<GeneratorNodeStruct> logger)
    : BaseGenerator<CRecord>(logger)
{
    protected override string GenerateCode(string nameCSharp, CodeGeneratorContext context, CRecord record)
    {
        var codeStructMembers = GenerateCodeStructMembers(context.NameMapper, record.Fields);
        var membersCode = string.Join("\n\n", codeStructMembers);

        var code = $$"""
                     [StructLayout(LayoutKind.Explicit, Size = {{record.SizeOf}}, Pack = {{record.AlignOf}})]
                     public struct {{nameCSharp}}
                     {
                         {{membersCode}}
                     }
                     """;

        return code;
    }

    private string[] GenerateCodeStructMembers(NameMapper nameMapper, ImmutableArray<CRecordField> fields)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var field in fields)
        {
            var code = GenerateCodeStructField(nameMapper, field);
            builder.Add(code);
        }

        var structMembers = builder.ToArray();
        return structMembers;
    }

    private string GenerateCodeStructField(NameMapper nameMapper, CRecordField field)
    {
        var fieldNameCSharp = nameMapper.GetIdentifierCSharp(field.Name);
        var isArray = field.Type.ArraySizeOf != null;
        var code = isArray
            ? GenerateCodeStructFieldFixedBuffer(fieldNameCSharp, nameMapper, field)
            : GenerateCodeStructFieldNormal(fieldNameCSharp, nameMapper, field);
        return code;
    }

    private string GenerateCodeStructFieldFixedBuffer(
        string fieldNameCSharp,
        NameMapper nameMapper,
        CRecordField field)
    {
        var elementTypeName = nameMapper.GetTypeNameCSharp(field.Type.InnerType!);
        if (elementTypeName.EndsWith('*'))
        {
            elementTypeName = "IntPtr";
        }

        var typeNameIsFixedBufferCompatible = elementTypeName is "byte"
            or "char"
            or "short"
            or "int"
            or "long"
            or "sbyte"
            or "ushort"
            or "uint"
            or "ulong"
            or "float"
            or "double";

        string code;
        if (typeNameIsFixedBufferCompatible)
        {
            var fixedBufferTypeName = nameMapper.GetTypeNameCSharp(field.Type.InnerType!);
            code = $"""
                        [FieldOffset({field.OffsetOf})] // size = {field.Type.SizeOf}
                        public fixed {fixedBufferTypeName} {fieldNameCSharp}[{field.Type.ArraySizeOf}];
                        """;
        }
        else
        {
            code = $$"""
                        [FieldOffset({{field.OffsetOf}})] // size = {{field.Type.SizeOf}}
                        public fixed byte _{{fieldNameCSharp}}[{{field.Type.SizeOf}}]; // {{field.Type.Name}}
                        """;
        }

        return code;
    }

    private string GenerateCodeStructFieldNormal(
        string fieldNameCSharp,
        NameMapper nameMapper,
        CRecordField field)
    {
        var fieldTypeNameCSharp = nameMapper.GetTypeNameCSharp(field.Type);

        string code;
#pragma warning disable IDE0045
        if (fieldTypeNameCSharp == "CString")
#pragma warning restore IDE0045
        {
            code = $$"""
                     [FieldOffset({{field.OffsetOf}})] // size = {{field.Type.SizeOf}}
                     public {{fieldTypeNameCSharp}} _{{fieldNameCSharp}};

                     public string {{fieldNameCSharp}}
                     {
                         get
                         {
                             return CString.ToString(_{{fieldNameCSharp}});
                         }
                         set
                         {
                             _{{fieldNameCSharp}} = CString.FromString(value);
                         }
                     }
                     """;
        }
        else
        {
            code = $$"""
                     [FieldOffset({{field.OffsetOf}})]
                     public {{fieldTypeNameCSharp}} {{fieldNameCSharp}}; // size = {{field.Type.SizeOf}}
                     """;
        }

        return code;
    }
}
