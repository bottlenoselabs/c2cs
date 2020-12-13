// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
    internal static class SyntaxNodeFormattingExtensions
    {
        public static ClassDeclarationSyntax Format(this ClassDeclarationSyntax rootNode)
        {
            rootNode = rootNode
                .NormalizeWhitespace()
                .TwoNewLinesForLastField()
                .RemoveLeadingTriviaForPointers()
                .AddSpaceTriviaForPointers()
                .TwoNewLinesForEveryExternMethodExceptLast()
                .TwoNewLinesForEveryStructFieldExceptLast();
            return rootNode;
        }

        private static TNode TwoNewLinesForLastField<TNode>(this TNode rootNode)
            where TNode : SyntaxNode
        {
            var lastField = rootNode.ChildNodes().OfType<FieldDeclarationSyntax>().Last();
            var lastNode = rootNode.ChildNodes().Last();
            if (lastNode != lastField)
            {
                rootNode = rootNode.ReplaceNode(lastField, lastField
                    .WithTrailingTrivia(CarriageReturnLineFeed, CarriageReturnLineFeed));
            }

            return rootNode;
        }

        private static TNode RemoveLeadingTriviaForPointers<TNode>(this TNode rootNode)
            where TNode : SyntaxNode
        {
            return rootNode.ReplaceNodes(
                rootNode.DescendantNodes()
                    .OfType<PointerTypeSyntax>().Select(x => x.ElementType),
                (_, node) => node.WithoutTrailingTrivia());
        }

        private static TNode AddSpaceTriviaForPointers<TNode>(this TNode rootNode)
            where TNode : SyntaxNode
        {
            return rootNode.ReplaceNodes(
                rootNode.DescendantNodes().OfType<PointerTypeSyntax>(),
                (_, node) => node.WithTrailingTrivia(Space));
        }

        private static TNode TwoNewLinesForEveryExternMethodExceptLast<TNode>(this TNode rootNode)
            where TNode : SyntaxNode
        {
            var methods = rootNode.ChildNodes().OfType<MethodDeclarationSyntax>().ToArray();
            var lastNode = rootNode.ChildNodes().Last();
            return rootNode.ReplaceNodes(
                methods,
                (_, method) =>
                {
                    if (method == lastNode)
                    {
                        return method;
                    }

                    var triviaToAdd = new[]
                    {
                        CarriageReturnLineFeed
                    };

                    var trailingTrivia = method.GetTrailingTrivia();

                    return trailingTrivia.Count == 0
                        ? method.WithTrailingTrivia(triviaToAdd)
                        : method.InsertTriviaAfter(trailingTrivia.Last(), triviaToAdd);
                });
        }

        private static TNode TwoNewLinesForEveryStructFieldExceptLast<TNode>(this TNode rootNode)
            where TNode : SyntaxNode
        {
            var fields = rootNode.DescendantNodes().OfType<FieldDeclarationSyntax>().ToArray();
            return rootNode.ReplaceNodes(
                fields,
                (_, field) =>
                {
                    if (!(field.Parent is StructDeclarationSyntax @struct))
                    {
                        return field;
                    }

                    var lastNode = @struct.ChildNodes().OfType<FieldDeclarationSyntax>().Last();
                    if (field == lastNode)
                    {
                        return field;
                    }

                    var triviaToAdd = new[]
                    {
                        CarriageReturnLineFeed
                    };

                    return field.InsertTriviaAfter(field.GetTrailingTrivia().Last(), triviaToAdd);
                });
        }
    }
}
