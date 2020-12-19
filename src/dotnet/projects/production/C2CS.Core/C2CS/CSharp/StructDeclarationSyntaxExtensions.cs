// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
    internal static class StructDeclarationSyntaxExtensions
    {
        internal static StructDeclarationSyntax WithAttributeStructLayout(
            this StructDeclarationSyntax structDeclarationSyntax,
            LayoutKind layoutKind,
            int size,
            int pack)
        {
            var layoutKindMemberAccessExpression = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(
                    "LayoutKind"),
                IdentifierName(
                    $@"{layoutKind}"));
            var sizeAssignmentExpression =
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("Size"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(size)));
            var packAssignmentExpression =
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName("Pack"),
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(pack)));
            return structDeclarationSyntax.WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName("StructLayout"),
                                AttributeArgumentList(
                                    SeparatedList(new[]
                                    {
                                        AttributeArgument(layoutKindMemberAccessExpression),
                                        AttributeArgument(sizeAssignmentExpression),
                                        AttributeArgument(packAssignmentExpression)
                                    })))))));
        }
    }
}
