// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
	internal static class MethodDeclarationSyntaxExtensions
	{
		internal static MethodDeclarationSyntax WithDllImportAttribute(this MethodDeclarationSyntax methodDeclarationSyntax)
		{
			return methodDeclarationSyntax.WithAttributeLists(
				SingletonList(
					AttributeList(
						SingletonSeparatedList(
							Attribute(
								IdentifierName("DllImport"),
								AttributeArgumentList(
									SeparatedList(new[]
									{
										AttributeArgument(
											IdentifierName("LibraryName"))
									})))))));
		}
	}
}
