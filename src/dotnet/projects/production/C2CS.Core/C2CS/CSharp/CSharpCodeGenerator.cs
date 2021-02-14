// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using ClangType = ClangSharp.Type;

namespace C2CS
{
	internal sealed class CSharpCodeGenerator
	{
		private readonly string _libraryName;
		private readonly ClangLayoutCalculator _layoutCalculator;

		public CSharpCodeGenerator(string libraryName, ClangLayoutCalculator layoutCalculator)
		{
			_libraryName = libraryName;
			_layoutCalculator = layoutCalculator;
		}

		public ClassDeclarationSyntax CreatePInvokeClass(string name, ImmutableArray<MemberDeclarationSyntax> members)
		{
			var newMembers = new List<MemberDeclarationSyntax>();

			var libraryNameField = FieldDeclaration(
					VariableDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)))
						.WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("LibraryName"))
							.WithInitializer(
								EqualsValueClause(LiteralExpression(
									SyntaxKind.StringLiteralExpression,
									Literal(_libraryName)))))))
				.WithModifiers(
					TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ConstKeyword)));

			newMembers.Add(libraryNameField);

			var membersSorted = members
				.OrderBy(x => x is FieldDeclarationSyntax)
				.ThenBy(x => x is EnumDeclarationSyntax)
				.ThenBy(x => x is StructDeclarationSyntax)
				.ThenBy(x => x is MethodDeclarationSyntax);
			newMembers.AddRange(membersSorted);

			return ClassDeclaration(name)
				.AddModifiers(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.StaticKeyword),
					Token(SyntaxKind.UnsafeKeyword),
					Token(SyntaxKind.PartialKeyword))
				.WithMembers(List(
					newMembers));
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public MethodDeclarationSyntax CreateExternMethod(CXCursor function)
		{
			var typeClang = function.ResultType;
			var baseTypeClang = GetClangBaseType(typeClang);
			var returnType = GetTypeSyntax(typeClang, baseTypeClang);
			var method = MethodDeclaration(returnType, function.Spelling.CString)
				.WithDllImportAttribute()
				.WithModifiers(TokenList(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.StaticKeyword),
					Token(SyntaxKind.ExternKeyword)))
				.WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

			var parametersC = new List<CXCursor>();
			function.VisitChildren(child =>
			{
				if (child.kind == CXCursorKind.CXCursor_ParmDecl)
				{
					parametersC.Add(child);
				}
			});
			var parameters = CreateMethodParameters(parametersC);

			method = method
				.AddParameterListParameters(parameters.ToArray());

			return method;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public FieldDeclarationSyntax CreateConstant(CXCursor enumConstant)
		{
			var variableDeclarationCSharp = CreateConstantVariable(enumConstant);
			var constFieldDeclarationCSharp = FieldDeclaration(variableDeclarationCSharp)
				.WithModifiers(
					TokenList(
						Token(SyntaxKind.PublicKeyword),
						Token(SyntaxKind.ConstKeyword)));

			return constFieldDeclarationCSharp;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public EnumDeclarationSyntax CreateEnum(CXCursor clangEnum)
		{
			var cSharpEnumName = ClangDisplayName(clangEnum);
			var cSharpEnumType = GetEnumSyntaxKind(clangEnum);
			var cSharpEnum = EnumDeclaration(cSharpEnumName)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddBaseListTypes(SimpleBaseType(ParseTypeName(cSharpEnumType)));

			var clangEnumMembers = clangEnum.ChildrenOfKind(CXCursorKind.CXCursor_EnumConstantDecl);
			var cSharpEnumMembers = new EnumMemberDeclarationSyntax[clangEnumMembers.Length];
			for (var i = 0; i < clangEnumMembers.Length; i++)
			{
				var clangEnumConstant = clangEnumMembers[i];
				var clangEnumMemberName = ClangDisplayName(clangEnumConstant).Replace($"{cSharpEnumName}_", string.Empty);
				var clangEnumValue = clangEnumConstant.EnumConstantDeclValue;

				var cSharpEqualsValueClause = CreateEqualsValueClause(clangEnumValue, cSharpEnumType);
				cSharpEnumMembers[i] = EnumMemberDeclaration(clangEnumMemberName)
					.WithEqualsValue(cSharpEqualsValueClause);
			}

			return cSharpEnum.AddMembers(cSharpEnumMembers);
		}

		public StructDeclarationSyntax CreateStruct(CXCursor clangRecord)
		{
			if (clangRecord.IsAnonymous)
			{
				// TODO: Anon structs/unions
				throw new NotImplementedException();
			}

			var cSharpName = ClangDisplayName(clangRecord);
			var clangLayout = _layoutCalculator.CalculateLayout(clangRecord);
			var clangType = clangRecord.Type;
			var clangBaseType = GetClangBaseType(clangType);
			var cSharpType = GetTypeSyntax(clangType, clangBaseType);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, clangLayout.Size, clangLayout.Alignment);

			var clangRecordFieldMembers = clangRecord.ChildrenOfKind(CXCursorKind.CXCursor_FieldDecl);
			var members = new List<MemberDeclarationSyntax>(clangRecordFieldMembers.Length);
			for (var i = 0; i < clangRecordFieldMembers.Length; i++)
			{
				var fieldC = clangRecordFieldMembers[i];
				CreateStructHelperAddField(cSharpType, fieldC, members);
			}

			cSharpStruct = cSharpStruct.AddMembers(members.ToArray());

			return cSharpStruct;
		}

		private static MethodDeclarationSyntax CreateStructHelperCreateWrappedFieldMethod(CXCursor fieldC, TypeSyntax structTypeSyntax)
		{
			var typeClang = fieldC.Type;
			var baseTypeClang = GetClangBaseType(typeClang);
			var arrayElementTypeSyntax = GetTypeSyntax(typeClang, baseTypeClang);
			var fieldTypeSyntax = PointerType(arrayElementTypeSyntax);
			return CreateStructFieldWrapperMethod(structTypeSyntax, fieldTypeSyntax, fieldC);
		}

		private void CreateStructHelperAddField(
			TypeSyntax parentStructSyntax,
			CXCursor clangField,
			ICollection<MemberDeclarationSyntax> members)
		{
			var clangType = clangField.Type;
			var type = clangType.TypeClass switch
			{
				CX_TypeClass.CX_TypeClass_ConstantArray => clangType.ElementType,
				CX_TypeClass.CX_TypeClass_Elaborated => clangType.NamedType,
				CX_TypeClass.CX_TypeClass_Typedef => clangType.Declaration.TypedefDeclUnderlyingType,
				_ => clangField.Type
			};

			// it's possible to have an array of an anon
			if (type.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
			{
				type = type.NamedType;
			}

			switch (type.TypeClass)
			{
				case CX_TypeClass.CX_TypeClass_Pointer:
				{
					// TODO: Use delegate / function pointer; https://github.com/lithiumtoast/c2cs/issues/2
					// typeName = "IntPtr";
					break;
				}

				case CX_TypeClass.CX_TypeClass_Record when type.Declaration.IsAnonymous:
				{
					var recordC = type.Declaration;
					if (recordC.kind == CXCursorKind.CXCursor_StructDecl)
					{
						var anonStruct = CreateStruct(recordC);
						members.Add(anonStruct);
					}
					else if (recordC.kind == CXCursorKind.CXCursor_UnionDecl)
					{
						throw new NotImplementedException();
					}

					break;
				}

				default:
				{
					// typeName = fieldC.Type.AsString;
					break;
				}
			}

			var field = CreateStructField(clangField, out var needsToBeWrapped);

			members.Add(field);

			if (needsToBeWrapped)
			{
				var method = CreateStructHelperCreateWrappedFieldMethod(clangField, parentStructSyntax);
				members.Add(method);
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private FieldDeclarationSyntax CreateStructField(CXCursor clangField, out bool needsToBeWrapped)
		{
			var typeClang = clangField.Type;
			var cSharpName = ClangDisplayName(clangField);
			TypeSyntax type;

			// TODO: Function pointers
			if (typeClang.TypeClass == CX_TypeClass.CX_TypeClass_Pointer &&
			    typeClang.PointeeType.TypeClass == CX_TypeClass.CX_TypeClass_FunctionProto)
			{
				// Function pointers not implemented yet
				type = PointerType(ParseTypeName("void"));
			}
			else
			{
				var baseTypeClang = GetClangBaseType(typeClang);
				type = GetTypeSyntax(typeClang, baseTypeClang);
			}

			var typeFullString = type.ToFullString();

			needsToBeWrapped = false;

			VariableDeclaratorSyntax variable;
			var isArray = typeClang.TypeClass == CX_TypeClass.CX_TypeClass_ConstantArray;
			if (!isArray)
			{
				cSharpName = SanitizeName(cSharpName);
				variable = VariableDeclarator(Identifier(cSharpName));
			}
			else
			{
				var isValidFixedType = IsValidCSharpTypeSpellingForFixedBuffer(typeFullString);
				if (isValidFixedType)
				{
					cSharpName = SanitizeName(cSharpName);
					var sizeInBytes = typeClang.SizeOf;
					var alignInBytes = typeClang.AlignOf;
					var arraySize = sizeInBytes / alignInBytes;
					variable = VariableDeclarator(Identifier(cSharpName))
						.WithArgumentList(
							BracketedArgumentList(
								SingletonSeparatedList(
									Argument(
										LiteralExpression(
											SyntaxKind.NumericLiteralExpression,
											Literal((int)arraySize))))));
				}
				else
				{
					var typeTokenSyntaxKind = typeClang.AlignOf switch
					{
						1 => SyntaxKind.ByteKeyword,
						2 => SyntaxKind.UShortKeyword,
						4 => SyntaxKind.UIntKeyword,
						8 => SyntaxKind.ULongKeyword,
						_ => throw new ArgumentException("Invalid field alignment.")
					};

					type = PredefinedType(Token(typeTokenSyntaxKind));
					variable = VariableDeclarator(Identifier($"_{cSharpName}"))
						.WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
							Argument(
								BinaryExpression(
									SyntaxKind.DivideExpression,
									LiteralExpression(
										SyntaxKind.NumericLiteralExpression,
										Literal((int)typeClang.SizeOf)),
									LiteralExpression(
										SyntaxKind.NumericLiteralExpression,
										Literal((int)typeClang.AlignOf)))))));

					needsToBeWrapped = true;
				}
			}

			var layout = _layoutCalculator.CalculateLayout(clangField);
			var field = FieldDeclaration(
					VariableDeclaration(type)
						.WithVariables(SingletonSeparatedList(variable)))
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeFieldOffset(layout.FieldAddress, layout.Size, layout.FieldPadding);

			if (isArray)
			{
				field = field.AddModifiers(Token(SyntaxKind.FixedKeyword))
					.WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(
						Comment($"/* original type is `{typeClang}` */"))));
			}

			return field;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public StructDeclarationSyntax CreateOpaqueStruct(CXCursor clangTypedef)
		{
			var clangType = clangTypedef.Type;
			var cSharpName = ClangDisplayName(clangTypedef);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, (int)clangType.SizeOf, (int)clangType.AlignOf);

			var cSharpFieldType = ParseTypeName("IntPtr");
			var cSharpFieldVariable = VariableDeclarator(Identifier("Handle"));
			var cSharpField = FieldDeclaration(
					VariableDeclaration(cSharpFieldType)
						.WithVariables(SingletonSeparatedList(cSharpFieldVariable)))
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeFieldOffset(0, 8, 1);
			cSharpStruct = cSharpStruct.AddMembers(cSharpField);

			return cSharpStruct;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public StructDeclarationSyntax CreateExternalStruct(CXCursor clangTypedef)
		{
			var clangType = clangTypedef.Type;
			var cSharpName = ClangDisplayName(clangTypedef);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, (int)clangType.SizeOf, (int)clangType.AlignOf);

			return cSharpStruct;
		}

		private static IEnumerable<ParameterSyntax> CreateMethodParameters(IReadOnlyCollection<CXCursor> clangFunctionParameters)
		{
			var cSharpMethodParameters = new List<ParameterSyntax>(clangFunctionParameters.Count);
			var cSharpMethodParameterNames = new HashSet<string>();

			foreach (var clangFunctionParameter in clangFunctionParameters)
			{
				// var clangDisplayName = ClangDisplayName(clangFunctionParameter);
				var cSharpMethodParameterName = SanitizeName(clangFunctionParameter.Spelling.CString);

				while (cSharpMethodParameterNames.Contains(cSharpMethodParameterName))
				{
					var numberSuffixMatch = Regex.Match(cSharpMethodParameterName, "\\d$");
					if (numberSuffixMatch.Success)
					{
						var parameterNameWithoutSuffix = cSharpMethodParameterName.Substring(0, numberSuffixMatch.Index);
						cSharpMethodParameterName = ParameterNameUniqueSuffix(parameterNameWithoutSuffix, numberSuffixMatch.Value);
					}
					else
					{
						cSharpMethodParameterName = ParameterNameUniqueSuffix(cSharpMethodParameterName, string.Empty);
					}
				}

				cSharpMethodParameterNames.Add(cSharpMethodParameterName);
				var cSharpMethodParameter = CreateMethodParameter(clangFunctionParameter, cSharpMethodParameterName);
				cSharpMethodParameters.Add(cSharpMethodParameter);
			}

			return cSharpMethodParameters;

			static string ParameterNameUniqueSuffix(string parameterNameWithoutSuffix, string parameterSuffix)
			{
				if (parameterSuffix == string.Empty)
				{
					return parameterNameWithoutSuffix + "2";
				}

				var parameterSuffixNumber = int.Parse(parameterSuffix, NumberStyles.Integer, CultureInfo.InvariantCulture);
				parameterSuffixNumber += 1;
				var parameterName = parameterNameWithoutSuffix + parameterSuffixNumber;
				return parameterName;
			}
		}

		private static ParameterSyntax CreateMethodParameter(CXCursor clangMethodParameter, string parameterName)
		{
			var cSharpMethodParameter = Parameter(Identifier(parameterName));

			var clangType = clangMethodParameter.Type;
			var baseClangType = GetClangBaseType(clangType);
			var cSharpType = GetTypeSyntax(clangType, baseClangType);

			if (baseClangType != default && baseClangType.IsConstQualified)
			{
				cSharpMethodParameter = cSharpMethodParameter
					.WithAttribute("In");
			}

			cSharpMethodParameter = cSharpMethodParameter
				.WithType(cSharpType);

			return cSharpMethodParameter;
		}

		private static string GetEnumSyntaxKind(CXCursor clangEnum)
		{
			var enumTypeKind = clangEnum.EnumDecl_IntegerType.kind;

			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			return enumTypeKind switch
			{
				CXTypeKind.CXType_Int => "int",
				CXTypeKind.CXType_UInt => "uint",
				_ => throw new NotImplementedException($@"The enum type is not yet supported: {enumTypeKind}.")
			};
		}

		private static EqualsValueClauseSyntax CreateEqualsValueClause(long value, string type)
		{
			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			var literalToken = type switch
			{
				"int" => Literal((int)value),
				"uint" => Literal((uint)value),
				_ => throw new NotImplementedException($"The syntax kind is not yet supported: {type}.")
			};

			return EqualsValueClause(
				LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken));
		}

		private static VariableDeclarationSyntax CreateConstantVariable(CXCursor clangEnumConstant)
		{
			var clangType = clangEnumConstant.Type;
			var baseClangType = GetClangBaseType(clangType);
			var cSharpType = GetTypeSyntax(clangType, baseClangType);

			var cSharpTypeString = cSharpType.ToFullString();
			var literalSyntaxToken = cSharpTypeString switch
			{
				"short" => Literal((short)clangEnumConstant.EnumConstantDeclValue),
				"int" => Literal((int)clangEnumConstant.EnumConstantDeclValue),
				_ => throw new NotImplementedException(
					$@"The enum constant literal expression is not yet supported: {cSharpTypeString}.")
			};

			var cSharpName = ClangDisplayName(clangEnumConstant);
			var variable = VariableDeclaration(cSharpType)
				.WithVariables(
					SingletonSeparatedList(
						VariableDeclarator(
								Identifier(cSharpName))
							.WithInitializer(
								EqualsValueClause(LiteralExpression(
									SyntaxKind.NumericLiteralExpression, literalSyntaxToken)))));

			return variable;
		}

		private static MethodDeclarationSyntax CreateStructFieldWrapperMethod(
			TypeSyntax structType,
			TypeSyntax fieldType,
			CXCursor clangField)
		{
			var identifierName = clangField.Spelling.CString;
			var fieldIdentifierName = $"_{clangField.Spelling.CString}";

			var body = Block(SingletonList<StatementSyntax>(FixedStatement(
				VariableDeclaration(PointerType(structType))
					.WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("@this"))
						.WithInitializer(EqualsValueClause(
							PrefixUnaryExpression(SyntaxKind.AddressOfExpression, ThisExpression()))))),
				Block(
					LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
						.WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("pointer"))
							.WithInitializer(EqualsValueClause(CastExpression(
								PointerType(fieldType),
								PrefixUnaryExpression(SyntaxKind.AddressOfExpression, ElementAccessExpression(
										MemberAccessExpression(
											SyntaxKind.PointerMemberAccessExpression,
											IdentifierName("@this"),
											IdentifierName(fieldIdentifierName)))
									.WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
										Argument(LiteralExpression(
											SyntaxKind.NumericLiteralExpression,
											Literal(0))))))))))))),
					LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
						.WithVariables(SingletonSeparatedList(VariableDeclarator(
								Identifier("pointerOffset"))
							.WithInitializer(EqualsValueClause(
								IdentifierName("index")))))),
					ReturnStatement(RefExpression(PrefixUnaryExpression(
						SyntaxKind.PointerIndirectionExpression,
						ParenthesizedExpression(BinaryExpression(
							SyntaxKind.AddExpression,
							IdentifierName("pointer"),
							IdentifierName("pointerOffset"))))))))));

			return MethodDeclaration(RefType(fieldType), identifierName)
				.WithModifiers(TokenList(
					Token(SyntaxKind.PublicKeyword)))
				.WithParameterList(ParameterList(SingletonSeparatedList(
					Parameter(Identifier("index"))
						.WithType(PredefinedType(Token(SyntaxKind.IntKeyword)))
						.WithDefault(EqualsValueClause(LiteralExpression(
							SyntaxKind.NumericLiteralExpression,
							Literal(0)))))))
				.WithBody(body);
		}

		private static CXType GetClangBaseType(CXType typeClang)
		{
			switch (typeClang.TypeClass)
			{
				case CX_TypeClass.CX_TypeClass_Pointer:
				{
					var pointeeType = GetClangBaseType(typeClang.PointeeType);
					return pointeeType;
				}

				case CX_TypeClass.CX_TypeClass_Typedef:
				{
					var underlyingType = GetClangBaseType(typeClang.Declaration.TypedefDeclUnderlyingType);
					return underlyingType;
				}

				case CX_TypeClass.CX_TypeClass_Elaborated:
				{
					var elaboratedType = GetClangBaseType(typeClang.NamedType);
					return elaboratedType;
				}

				case CX_TypeClass.CX_TypeClass_ConstantArray:
				{
					var elementType = GetClangBaseType(typeClang.ArrayElementType);
					return elementType;
				}

				case CX_TypeClass.CX_TypeClass_Enum:
				case CX_TypeClass.CX_TypeClass_Record:
				case CX_TypeClass.CX_TypeClass_Builtin:
				{
					return typeClang;
				}

				case CX_TypeClass.CX_TypeClass_FunctionProto:
				{
					// TODO: Function pointers
					return default;
				}

				default:
					throw new NotImplementedException();
			}
		}

		private static TypeSyntax GetTypeSyntax(CXType typeClang, CXType baseTypeClang)
		{
			string baseTypeClangSpelling;

			// TODO: Function pointers
			if (baseTypeClang == default)
			{
				baseTypeClangSpelling = "void";
			}
			else
			{
				baseTypeClangSpelling = ConvertClangTypeToCSharpTypeString(baseTypeClang);
			}

			switch (typeClang.TypeClass)
			{
				case CX_TypeClass.CX_TypeClass_Pointer:
				{
					// TODO: Function pointers
					if (baseTypeClang == default)
					{
						return PointerType(ParseTypeName("void"));
					}

					var pointeeTypeSyntax = GetTypeSyntax(typeClang.PointeeType, baseTypeClang);
					var pointerTypeSyntax = PointerType(pointeeTypeSyntax);
					return pointerTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_Enum:
				{
					baseTypeClangSpelling = baseTypeClangSpelling.Replace("enum ", string.Empty).Trim();
					var enumTypeSyntax = ParseTypeName(baseTypeClangSpelling);
					return enumTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_Record:
				{
					baseTypeClangSpelling = baseTypeClangSpelling.Replace("struct ", string.Empty).Trim();
					var recordTypeSyntax = ParseTypeName(baseTypeClangSpelling);
					return recordTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_Elaborated:
				{
					var elaboratedTypeSyntax = GetTypeSyntax(typeClang.NamedType, baseTypeClang);
					return elaboratedTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_Builtin:
				{
					var typeCSharpString = ConvertClangTypeToCSharpTypeString(baseTypeClang);
					var builtinTypeSyntax = ParseTypeName(typeCSharpString);
					return builtinTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_ConstantArray:
				{
					// Just return the base type which is the array element type
					var baseTypeSyntax = ParseTypeName(baseTypeClangSpelling);
					return baseTypeSyntax;
				}

				case CX_TypeClass.CX_TypeClass_Typedef:
				{
					// TODO: Function pointers
					if (baseTypeClang == default)
					{
						return PointerType(ParseTypeName("void"));
					}

					var typeClangSpelling = typeClang.Spelling.CString.Replace("const ", string.Empty).Trim();
					var typedefSyntax = ParseTypeName(typeClangSpelling);
					return typedefSyntax;
				}

				default:
					throw new NotImplementedException();
			}
		}

		private static string ConvertClangTypeToCSharpTypeString(CXType clangType)
		{
			var result = clangType.kind switch
			{
				CXTypeKind.CXType_Void => "void",
				CXTypeKind.CXType_Bool => "CBool",
				CXTypeKind.CXType_UShort => "ushort",
				CXTypeKind.CXType_UInt => "uint",
				CXTypeKind.CXType_ULong => clangType.SizeOf == 8 ? "ulong" : "uint",
				CXTypeKind.CXType_ULongLong => "ulong",
				CXTypeKind.CXType_Char_S => "sbyte",
				CXTypeKind.CXType_Short => "short",
				CXTypeKind.CXType_Int => "int",
				CXTypeKind.CXType_Long => clangType.SizeOf == 8 ? "long" : "int",
				CXTypeKind.CXType_LongLong => "long",
				CXTypeKind.CXType_Float => "float",
				CXTypeKind.CXType_Double => "double",
				CXTypeKind.CXType_Record => clangType.Spelling.CString.Replace("struct ", string.Empty).Trim(),
				CXTypeKind.CXType_Enum => clangType.Spelling.CString.Replace("enum ", string.Empty).Trim(),
				_ => throw new NotImplementedException()
			};

			result = result.Replace("const ", string.Empty).Trim();

			return result;
		}

		private static bool IsValidCSharpTypeSpellingForFixedBuffer(string typeString)
		{
			return typeString switch
			{
				"bool" => true,
				"byte" => true,
				"char" => true,
				"short" => true,
				"int" => true,
				"long" => true,
				"sbyte" => true,
				"ushort" => true,
				"uint" => true,
				"ulong" => true,
				"float" => true,
				"double" => true,
				_ => false
			};
		}

		public static string ClangDisplayName(CXCursor cursor)
		{
			var name = cursor.Spelling.CString;
			if (string.IsNullOrEmpty(name))
			{
				var clangType = cursor.Type;
				if (clangType.kind == CXTypeKind.CXType_Pointer)
				{
					clangType = clangType.PointeeType;
				}

				name = clangType.Spelling.CString;
			}

			return name;
		}

		private static string SanitizeName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				// HACK: "handle" is meant for opaque types which are usually the first param for C functions exposing C++ functions
				name = "handle";
			}
			else
			{
				if (name.Contains("const "))
				{
					name = name.Replace("const ", string.Empty).Trim();
				}

				if (name.Contains("struct "))
				{
					name = name.Replace("struct ", string.Empty).Trim();
				}

				if (name.Contains("enum "))
				{
					name = name.Replace("enum ", string.Empty).Trim();
				}

				if (name == "lock" || name == "string" || name == "base")
				{
					name = $"@{name}";
				}
			}

			return name;
		}
	}
}
