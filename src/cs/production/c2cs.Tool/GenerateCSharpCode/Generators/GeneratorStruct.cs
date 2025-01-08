// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Linq;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorStruct(ILogger<GeneratorStruct> logger)
    : BaseGenerator<CRecord>(logger)
{
    public override string GenerateCode(CodeGeneratorContext context, string nameCSharp, CRecord record)
    {
        var codeStructMembers = GenerateCodeStructMembers(
            context, nameCSharp, record.Fields, record.NestedRecords, 0);
        var membersCode = string.Join("\n\n", codeStructMembers);

        var code = $$"""
                     [StructLayout(LayoutKind.Explicit, Size = {{record.SizeOf}}, Pack = {{record.AlignOf}})]
                     public {{(context.Input.IsEnabledRefStructs ? "ref" : string.Empty)}} partial struct {{nameCSharp}}
                     {
                         {{membersCode}}
                     }
                     """;

        return code;
    }

    private ImmutableArray<string> GenerateCodeStructMembers(
        CodeGeneratorContext context,
        string structNameCSharp,
        ImmutableArray<CRecordField> fields,
        ImmutableArray<CRecord> nestedStructs,
        int parentFieldOffsetOf)
    {
        var codeFields = ImmutableArray.CreateBuilder<string>();
        var codeNestedStructs = ImmutableArray.CreateBuilder<string>();

        var nestedStructIndex = 0;
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var fieldOffsetOf = parentFieldOffsetOf + field.OffsetOf;

            if (field.Type.IsAnonymous ?? false)
            {
                var nestedStruct = nestedStructs[nestedStructIndex++];
                var fieldNameCSharp = context.NameMapper.GetIdentifierCSharp(field.Name);
                if (string.IsNullOrEmpty(fieldNameCSharp))
                {
                    var codeNestedStructMembers = GenerateCodeStructMembers(
                        context, structNameCSharp, nestedStruct.Fields, nestedStruct.NestedRecords, fieldOffsetOf);
                    codeFields.AddRange(codeNestedStructMembers);
                }
                else
                {
                    var fieldNameCSharpParts = fieldNameCSharp.Split('_', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => $"{x[0].ToString().ToUpperInvariant()}{x.AsSpan(1)}");
                    var nestedStructName = $"{structNameCSharp}_{string.Join('_', fieldNameCSharpParts)}";
                    var codeNestedStruct = GenerateCode(context, nestedStructName, nestedStruct);
                    codeNestedStructs.Add(codeNestedStruct);

                    var codeField = GenerateCodeStructField(
                        context.NameMapper, field, fieldNameCSharp, nestedStructName, fieldOffsetOf);
                    codeFields.Add(codeField);
                }
            }
            else
            {
                var fieldNameCSharp = context.NameMapper.GetIdentifierCSharp(field.Name);
                var fieldTypeNameCSharp = context.NameMapper.GetTypeNameCSharp(field.Type);
                var codeField = GenerateCodeStructField(
                    context.NameMapper, field, fieldNameCSharp, fieldTypeNameCSharp, fieldOffsetOf);
                codeFields.Add(codeField);
            }
        }

        var result = codeFields.ToImmutable().AddRange(codeNestedStructs.ToImmutable());
        return result;
    }

    private string GenerateCodeStructField(
        NameMapper nameMapper,
        CRecordField field,
        string fieldNameCSharp,
        string fieldTypeNameCSharp,
        int fieldOffsetOf)
    {
        var isArray = field.Type.ArraySizeOf != null;
        var code = isArray
            ? GenerateCodeStructFieldFixedBuffer(field, fieldNameCSharp, nameMapper, fieldOffsetOf)
            : GenerateCodeStructFieldNormal(field, fieldNameCSharp, fieldTypeNameCSharp, fieldOffsetOf);
        return code;
    }

    private string GenerateCodeStructFieldFixedBuffer(
        CRecordField field,
        string fieldNameCSharp,
        NameMapper nameMapper,
        int fieldOffsetOf)
    {
        var elementTypeNameCSharp = nameMapper.GetTypeNameCSharp(field.Type.InnerType!);
        if (elementTypeNameCSharp.EndsWith('*'))
        {
            elementTypeNameCSharp = "IntPtr";
        }

        var typeNameIsFixedBufferCompatible = elementTypeNameCSharp is "byte"
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
            code = $"""
                        [FieldOffset({fieldOffsetOf})] // size = {field.Type.SizeOf}
                        public fixed {elementTypeNameCSharp} {fieldNameCSharp}[{field.Type.ArraySizeOf}];
                        """;
        }
        else
        {
            fieldNameCSharp = $"_{fieldNameCSharp.TrimStart('@')}";
            code = $"""
                    [FieldOffset({fieldOffsetOf})] // size = {field.Type.SizeOf}
                    public fixed byte {fieldNameCSharp}[{field.Type.SizeOf}]; // {field.Type.Name}
                    """;
        }

        return code;
    }

    private string GenerateCodeStructFieldNormal(
        CRecordField field,
        string fieldNameCSharp,
        string fieldTypeNameCSharp,
        int fieldOffsetOf)
    {
        string code;
#pragma warning disable IDE0045
        if (fieldTypeNameCSharp == "CString")
#pragma warning restore IDE0045
        {
            var backingFieldNameCSharp = $"_{fieldNameCSharp.TrimStart('@')}";

            code = $$"""
                     [FieldOffset({{fieldOffsetOf}})] // size = {{field.Type.SizeOf}}
                     public {{fieldTypeNameCSharp}} {{backingFieldNameCSharp}};

                     public string {{fieldNameCSharp}}
                     {
                         get
                         {
                             return CString.ToString({{backingFieldNameCSharp}});
                         }
                         set
                         {
                             {{backingFieldNameCSharp}} = CString.FromString(value);
                         }
                     }
                     """;
        }
        else
        {
            code = $$"""
                     [FieldOffset({{fieldOffsetOf}})]
                     public {{fieldTypeNameCSharp}} {{fieldNameCSharp}}; // size = {{field.Type.SizeOf}}
                     """;
        }

        return code;
    }
}
