// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS.Contexts.WriteCodeCSharp.Domain;

[PublicAPI]
public static class ExtensionsRosyln
{
    public static T AddRegionStart<T>(this T node, string regionName, bool addDoubleTrailingNewLine)
        where T : SyntaxNode
    {
        var trivia = node.GetLeadingTrivia();
        var index = 0;

        trivia = trivia
            .Insert(index++, CarriageReturnLineFeed)
            .Insert(index++, GetRegionLeadingTrivia(regionName));

        if (addDoubleTrailingNewLine)
        {
            trivia = trivia
                .Insert(index++, CarriageReturnLineFeed)
                .Insert(index, CarriageReturnLineFeed);
        }
        else
        {
            trivia = trivia
                .Insert(index, CarriageReturnLineFeed);
        }

        return node.WithLeadingTrivia(trivia);
    }

    public static T AddRegionEnd<T>(this T node)
        where T : SyntaxNode
    {
        var trivia = node.GetTrailingTrivia();
        var index = 0;

        trivia = trivia
            .Insert(index++, CarriageReturnLineFeed)
            .Insert(index++, CarriageReturnLineFeed);

        trivia = trivia.Insert(index, GetRegionTrailingTrivia());

        return node.WithTrailingTrivia(trivia);
    }

    private static SyntaxTrivia GetRegionLeadingTrivia(string regionName, bool isActive = true)
    {
        return Trivia(
            RegionDirectiveTrivia(isActive)
                .WithEndOfDirectiveToken(
                    Token(
                        TriviaList(PreprocessingMessage($" {regionName}")),
                        SyntaxKind.EndOfDirectiveToken,
                        TriviaList())));
    }

    private static SyntaxTrivia GetRegionTrailingTrivia(bool isActive = true)
    {
        return Trivia(EndRegionDirectiveTrivia(isActive));
    }
}
