// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using C2CS.Commands.WriteCodeCSharp.Data;
using c2ffi.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;

public class StructCodeGenerator : GenerateCodeHandler<CSharpStruct>
{
    private Version _spanNetCoreRequiredVersion = new Version("2.1");

    public StructCodeGenerator(
        ILogger<StructCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpStruct node)
    {
        return Struct(context, node, false);
    }

    private StructDeclarationSyntax Struct(CSharpCodeGeneratorContext context, CSharpStruct @struct, bool isNested)
    {
        var memberSyntaxes = StructMembers(context, @struct.Name, @struct.Fields, @struct.NestedStructs);
        var memberStrings = memberSyntaxes.Select(x => x.ToFullString());
        var members = string.Join("\n\n", memberStrings);

        var code = $$"""

                     [StructLayout(LayoutKind.Explicit, Size = {{@struct.SizeOf}}, Pack = {{@struct.AlignOf}})]
                     public struct {{@struct.Name}}
                     {
                     	{{members}}
                     }

                     """;

        if (isNested)
        {
            code = code.Trim();
        }

        var member = context.ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }

    private MemberDeclarationSyntax[] StructMembers(
        CSharpCodeGeneratorContext context,
        string structName,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<CSharpStruct> nestedStructs)
    {
        var builder = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        StructFields(context, structName, fields, builder);

        foreach (var nestedStruct in nestedStructs)
        {
            var syntax = Struct(context, nestedStruct, true);
            builder.Add(syntax);
        }

        var structMembers = builder.ToArray();
        return structMembers;
    }

    private void StructFields(
        CSharpCodeGeneratorContext context,
        string structName,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<MemberDeclarationSyntax>.Builder builder)
    {
        foreach (var field in fields)
        {
            StructField(context, structName, builder, field);
        }
    }

    private void StructField(
        CSharpCodeGeneratorContext context,
        string structName,
        ImmutableArray<MemberDeclarationSyntax>.Builder builder,
        CSharpStructField field)
    {
        if (field.Type.IsArray)
        {
            StructFieldFixedBuffer(context, structName, builder, field);
        }
        else
        {
            var fieldMember = StructField(context, field);
            builder.Add(fieldMember);
        }
    }

    private void StructFieldFixedBuffer(
        CSharpCodeGeneratorContext context,
        string structName,
        ImmutableArray<MemberDeclarationSyntax>.Builder builder,
        CSharpStructField field)
    {
        var elementTypeName = field.Type.Name[..^1];
        if (elementTypeName.EndsWith('*'))
        {
            elementTypeName = "IntPtr";
        }

        var elementType = field.Type.InnerType;
        var elementTypeIsEnum = elementType != null && (elementType.OriginalNodeKind == CNodeKind.Enum || (elementType.OriginalNodeKind == CNodeKind.TypeAlias && elementType.InnerType!.OriginalNodeKind == CNodeKind.Enum));
        var typeNameIsFixedBufferCompatible = elementTypeIsEnum ||
            elementTypeName is "byte" or "char" or "short" or "int" or "long" or "sbyte" or "ushort" or "uint"
                or "ulong" or "float" or "double";

        if (typeNameIsFixedBufferCompatible)
        {
            var fixedBufferTypeName = elementTypeIsEnum ? "int" : elementTypeName;
            var code = $"""

                        [FieldOffset({field.OffsetOf})] // size = {field.Type.SizeOf}
                        public fixed {fixedBufferTypeName} {field.Name}[{field.Type.ArraySizeOf}];

                        """.Trim();
            var fieldMember = context.ParseMemberCode<FieldDeclarationSyntax>(code);
            builder.Add(fieldMember);
        }
        else
        {
            var code = $"""

                        [FieldOffset({field.OffsetOf})] // size = {field.Type.SizeOf}
                        public fixed byte {field.BackingFieldName}[{field.Type.SizeOf}]; // {field.Type.OriginalName}

                        """.Trim();
            var fieldMember = context.ParseMemberCode<FieldDeclarationSyntax>(code);
            builder.Add(fieldMember);

            var methodMember = StructFieldFixedBufferProperty(context, structName, field);
            builder.Add(methodMember);
        }
    }

    private FieldDeclarationSyntax StructField(
        CSharpCodeGeneratorContext context,
        CSharpStructField field)
    {
        string code;
        if (field.Type.Name == "CString")
        {
            code = $$"""

                     [FieldOffset({{field.OffsetOf}})] // size = {{field.Type.SizeOf}}
                     public {{field.Type.FullName}} _{{field.Name}};

                     public string {{field.Name}}
                     {
                     	get
                     	{
                             return CString.ToString(_{{field.Name}});
                     	}
                         set
                         {
                             _{{field.Name}} = CString.FromString(value);
                         }
                     }

                     """.Trim();
        }
        else
        {
            code = $"""

                    [FieldOffset({field.OffsetOf})] // size = {field.Type.SizeOf}
                    public {field.Type.FullName} {field.Name};

                    """.Trim();
        }

        var member = context.ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }

    private PropertyDeclarationSyntax StructFieldFixedBufferProperty(
        CSharpCodeGeneratorContext context,
        string structName,
        CSharpStructField field)
    {
        string code;

        if (field.Type.Name == "CString")
        {
            code = $$"""

                     public string {{field.Name}}
                     {
                     	get
                     	{
                     		fixed ({{structName}}*@this = &this)
                     		{
                     		    var pointer = &@this->{{field.BackingFieldName}}[0];
                                var cString = new CString(pointer);
                                return CString.ToString(cString);
                     		}
                     	}
                     }

                     """.Trim();
        }
        else if (field.Type.Name == "CStringWide")
        {
            code = $$"""

                     public string {{field.Name}}
                     {
                     	get
                     	{
                     		fixed ({{structName}}*@this = &this)
                     		{
                     			var pointer = &@this->{{field.BackingFieldName}}[0];
                                 var cString = new CStringWide(pointer);
                                 return StringWide.ToString(cString);
                     		}
                     	}
                     }

                     """.Trim();
        }
        else
        {
            var fieldTypeName = field.Type.Name;
            var elementType = fieldTypeName[..^1];
            if (elementType.EndsWith('*'))
            {
                elementType = "IntPtr";
            }

            var isAtLeastNetCoreV21 = context.Options.TargetFramework.Framework == ".NETCoreApp" &&
                                  context.Options.TargetFramework.Version >= _spanNetCoreRequiredVersion;

            if (isAtLeastNetCoreV21)
            {
                code = $$"""

                         public readonly Span<{{elementType}}> {{field.Name}}
                         {
                         	get
                         	{
                         		fixed ({{structName}}*@this = &this)
                         		{
                         			var pointer = &@this->{{field.BackingFieldName}}[0];
                         			var span = new Span<{{elementType}}>(pointer, {{field.Type.ArraySizeOf}});
                         			return span;
                         		}
                         	}
                         }

                         """.Trim();
            }
            else
            {
                code = $$"""

                         public {{elementType}}[] {{field.Name}}
                         {
                         	get
                         	{
                         		fixed ({{structName}}*@this = &this)
                         		{
                         			var pointer = ({{elementType}}*)&@this->{{field.BackingFieldName}}[0];
                         			var array = new {{elementType}}[{{field.Type.ArraySizeOf}}];
                                    for (var i = 0; i < {{field.Type.ArraySizeOf}}; i++, pointer++)
                                    {
                                        array[i] = *pointer;
                                    }
                                    return array;
                                }
                            }
                         }

                         """.Trim();
            }
        }

        return context.ParseMemberCode<PropertyDeclarationSyntax>(code);
    }
}
