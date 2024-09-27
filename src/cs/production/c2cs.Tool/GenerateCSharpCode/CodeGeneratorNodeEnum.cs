// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeEnum : CodeGeneratorNodeBase<CEnum>
{
    public CodeGeneratorNodeEnum(
        ILogger<CodeGeneratorNodeEnum> logger,
        NameMapper nameMapper)
        : base(logger, nameMapper)
    {
    }

    protected override SyntaxNode GenerateCode(
        string nameCSharp,
        CodeGeneratorDocumentPInvokeContext context,
        CEnum node)
    {
        var size = node.SizeOf;
        var integerTypeNameCSharp = size switch
        {
            1 => "sbyte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new NotImplementedException(
                $"The enum size is not supported: '{nameCSharp}' of size {node.SizeOf}.")
        };

        var valuesString = EnumValuesCode(integerTypeNameCSharp, node.Values);
        var enumMembers = string.Join(",\n", valuesString);

        var code = $$"""

                     public enum {{nameCSharp}} : {{integerTypeNameCSharp}}
                         {
                             {{enumMembers}}
                         }

                     """;

        return ParseMemberCode<EnumDeclarationSyntax>(code);
    }

    private static string[] EnumValuesCode(
        string enumIntegerTypeName, ImmutableArray<CEnumValue> values)
    {
        var builder = ImmutableArray.CreateBuilder<string>(values.Length);

        foreach (var value in values)
        {
            var enumEqualsValue = EnumEqualsValue(value.Value, enumIntegerTypeName);
            var memberString = SyntaxFactory.EnumMemberDeclaration(value.Name)
                .WithEqualsValue(enumEqualsValue).ToFullString();

            builder.Add(memberString);
        }

        return builder.ToArray();
    }

    private static EqualsValueClauseSyntax EnumEqualsValue(long value, string enumIntegerTypeName)
    {
        var literalToken = enumIntegerTypeName switch
        {
            "sbyte" => SyntaxFactory.Literal((sbyte)value),
            "short" => SyntaxFactory.Literal((short)value),
            "int" => SyntaxFactory.Literal((int)value),
            "long" => SyntaxFactory.Literal(value),
            _ => throw new NotImplementedException(
                $"The enum integer type name is not supported: {enumIntegerTypeName}.")
        };

        return SyntaxFactory.EqualsValueClause(
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression, literalToken));
    }
}
