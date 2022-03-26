// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS.Feature.BindgenCSharp.Domain.Logic;

[PublicAPI]
public static class ExtensionsRosyln
{
    public static T AddRegion<T>(this T node, string regionName)
        where T : SyntaxNode
    {
        var nodeLeadingTrivia = node.GetLeadingTrivia();
        var nodeTrailingTrivia = node.GetTrailingTrivia();
        var leadingTrivia = nodeLeadingTrivia
            .Insert(0, CarriageReturnLineFeed)
            .Insert(1, GetRegionLeadingTrivia(regionName))
            .Insert(2, CarriageReturnLineFeed)
            .Insert(3, CarriageReturnLineFeed);
        var trailingTrivia = nodeTrailingTrivia
            .Insert(0, CarriageReturnLineFeed)
            .Insert(1, CarriageReturnLineFeed)
            .Insert(2, GetRegionTrailingTrivia())
            .Insert(3, CarriageReturnLineFeed);

        return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
    }

    private static SyntaxTrivia GetRegionLeadingTrivia(string regionName)
    {
        return Trivia(
            RegionDirectiveTrivia(true)
                .WithEndOfDirectiveToken(
                    Token(
                        TriviaList(PreprocessingMessage($" {regionName}")),
                        SyntaxKind.EndOfDirectiveToken,
                        TriviaList())));
    }

    private static SyntaxTrivia GetRegionTrailingTrivia()
    {
        return Trivia(EndRegionDirectiveTrivia(true));
    }
}
