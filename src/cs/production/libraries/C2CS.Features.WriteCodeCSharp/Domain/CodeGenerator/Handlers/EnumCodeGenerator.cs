// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.Domain.CodeGenerator.Handlers;

public class EnumCodeGenerator : GenerateCodeHandler<CSharpEnum>
{
    public EnumCodeGenerator(
        ILogger<EnumCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpEnum node)
    {
        var attributesString = context.GenerateCodeAttributes(node.Attributes);

        var enumName = node.Name;
        var enumSizeOf = node.SizeOf!.Value;
        var enumIntegerTypeName = enumSizeOf switch
        {
            1 => "sbyte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new NotImplementedException(
                $"The enum size is not supported: '{enumName}' of size {node.SizeOf}.")
        };

        var values = EnumValues(enumIntegerTypeName, node.Values);
        var valuesString = values.Select(x => x.ToFullString());
        var members = string.Join(",\n", valuesString);

        var code = $@"
{attributesString}
public enum {enumName} : {enumIntegerTypeName}
    {{
        {members}
    }}
";

        var member = context.ParseMemberCode<EnumDeclarationSyntax>(code);
        return member;
    }

    private static EnumMemberDeclarationSyntax[] EnumValues(
        string enumIntegerTypeName, ImmutableArray<CSharpEnumValue> values)
    {
        var builder = ImmutableArray.CreateBuilder<EnumMemberDeclarationSyntax>(values.Length);

        foreach (var value in values)
        {
            var enumEqualsValue = EmitEnumEqualsValue(value.Value, enumIntegerTypeName);
            var member = SyntaxFactory.EnumMemberDeclaration(value.Name)
                .WithEqualsValue(enumEqualsValue);

            builder.Add(member);
        }

        return builder.ToArray();
    }

    private static EqualsValueClauseSyntax EmitEnumEqualsValue(long value, string enumIntegerTypeName)
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

        return SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken));
    }
}
