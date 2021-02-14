// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
	internal static class RosylnExtensions
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

		internal static ParameterSyntax WithAttribute(
			this ParameterSyntax parameterSyntax,
			string name)
		{
			return parameterSyntax.WithAttributeLists(
				SingletonList(
					AttributeList(
						SingletonSeparatedList(
							Attribute(
								IdentifierName(name))))));
		}

		internal static MethodDeclarationSyntax WithDllImportAttribute(
			this MethodDeclarationSyntax methodDeclarationSyntax,
			CallingConvention callingConvention)
		{
			return methodDeclarationSyntax.WithAttributeLists(
				SingletonList(
					AttributeList(
						SingletonSeparatedList(
							Attribute(
								IdentifierName("DllImport"),
								AttributeArgumentList(
									SeparatedList<AttributeArgumentSyntax>(
										new SyntaxNodeOrToken[]
										{
											AttributeArgument(
												IdentifierName("LibraryName")),
											Token(SyntaxKind.CommaToken),
											AttributeArgument(
													MemberAccessExpression(
														SyntaxKind.SimpleMemberAccessExpression,
														IdentifierName("CallingConvention"),
														IdentifierName(callingConvention.ToString())))
												.WithNameEquals(
													NameEquals(
														IdentifierName("CallingConvention")))
										})))))));
		}

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
