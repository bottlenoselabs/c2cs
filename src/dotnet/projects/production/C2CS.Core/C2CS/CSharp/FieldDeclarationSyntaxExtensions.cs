// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
	internal static class FieldDeclarationSyntaxExtensions
	{
		internal static FieldDeclarationSyntax WithAttributeFieldOffset(
			this FieldDeclarationSyntax fieldDeclarationSyntax,
			int offset,
			int size,
			int padding)
		{
			return fieldDeclarationSyntax.WithAttributeLists(
				SingletonList(
					AttributeList(
							SingletonSeparatedList(
								Attribute(
									IdentifierName("FieldOffset"),
									AttributeArgumentList(
										SeparatedList(new[]
										{
											AttributeArgument(
												LiteralExpression(
													SyntaxKind.NumericLiteralExpression,
													Literal(offset)))
										})))))
						.WithCloseBracketToken(
							Token(
								TriviaList(),
								SyntaxKind.CloseBracketToken,
								TriviaList(
									Comment($"/* size = {size}, padding = {padding} */"))))));
		}
	}
}
