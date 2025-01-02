// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestStructField
{
    public string Name { get; }

    public string TypeName { get; }

    public int? OffsetOf { get; }

    public CSharpTestStructField(
        FieldDeclarationSyntax syntaxNode,
        CSharpTestStructLayout structLayout)
    {
        var variableSyntaxNode = syntaxNode.Declaration;
        Name = variableSyntaxNode.Variables[0].Identifier.Text;
        TypeName = variableSyntaxNode.Type.ToString();
        OffsetOf = structLayout.LayoutKind == "LayoutKind.Explicit" ? FieldOffsetOf(syntaxNode) : null;
    }

    public override string ToString()
    {
        return Name;
    }

    private int? FieldOffsetOf(FieldDeclarationSyntax syntaxNode)
    {
        int? offsetOf = null;

        var attribute = syntaxNode.TryGetAttribute("FieldOffset");
        _ = attribute.Should().NotBeNull();

        var expression = attribute!.ArgumentList!.Arguments[0].Expression;
        if (expression is LiteralExpressionSyntax literalExpression)
        {
            offsetOf = int.Parse(literalExpression.Token.ValueText, CultureInfo.InvariantCulture);
        }

        _ = offsetOf.Should().NotBeNull();
        return offsetOf;
    }
}
