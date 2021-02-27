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
	// TODO: Clean up this class; remove clang types from the responsibility of generating C# code.

	internal sealed class CSharpCodeGenerator
	{
		private readonly string _libraryName;
		private readonly ClangLayoutCalculator _layoutCalculator;
		private readonly Dictionary<CXType, TypeSyntax> _cSharpTypesByClangType = new();
		private readonly Dictionary<CXCursor, string> _cSharpNamesByClangCursor = new();

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
				.OrderByDescending(x => x is FieldDeclarationSyntax)
				.ThenByDescending(x => x is MethodDeclarationSyntax)
				.ThenByDescending(x => x is StructDeclarationSyntax)
				.ThenByDescending(x => x is EnumDeclarationSyntax);
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
			// TODO: Other calling conventions
			var clangCallingConvention = function.Type.FunctionTypeCallingConv;
			CallingConvention cSharpCallingConvention;
			switch (clangCallingConvention)
			{
				case CXCallingConv.CXCallingConv_C:
					cSharpCallingConvention = CallingConvention.Cdecl;
					break;
				default:
					throw new NotImplementedException();
			}

			var typeClang = function.ResultType;
			var returnType = GetCSharpType(typeClang);
			var method = MethodDeclaration(returnType, function.Spelling.CString)
				.WithDllImportAttribute(cSharpCallingConvention)
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
		public EnumDeclarationSyntax CreateEnum(CXCursor cursor)
		{
			var cSharpEnumName = ClangName(cursor);
			var cSharpEnumType = GetCSharpEnumType(cursor);
			var cSharpEnum = EnumDeclaration(cSharpEnumName)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddBaseListTypes(SimpleBaseType(ParseTypeName(cSharpEnumType)));

			var clangEnum = ClangEnum(cursor);
			var clangEnumMembers = clangEnum.ChildrenOfKind(CXCursorKind.CXCursor_EnumConstantDecl);
			var cSharpEnumMembers = new EnumMemberDeclarationSyntax[clangEnumMembers.Length];
			for (var i = 0; i < clangEnumMembers.Length; i++)
			{
				var clangEnumConstant = clangEnumMembers[i];
				var clangEnumMemberName = ClangName(clangEnumConstant).Replace($"{cSharpEnumName}_", string.Empty);
				var clangEnumValue = clangEnumConstant.EnumConstantDeclValue;

				var cSharpEqualsValueClause = CreateEqualsValueClause(clangEnumValue, cSharpEnumType);
				cSharpEnumMembers[i] = EnumMemberDeclaration(clangEnumMemberName)
					.WithEqualsValue(cSharpEqualsValueClause);
			}

			return cSharpEnum.AddMembers(cSharpEnumMembers);

			static string GetCSharpEnumType(CXCursor clangEnum)
			{
				var clangEnumType = clangEnum.kind == CXCursorKind.CXCursor_TypedefDecl
					? clangEnum.TypedefDeclUnderlyingType.Declaration.EnumDecl_IntegerType
					: clangEnum.EnumDecl_IntegerType;

				var enumTypeKind = clangEnumType.kind;
				return enumTypeKind switch
				{
					CXTypeKind.CXType_Int => "int",
					CXTypeKind.CXType_UInt => "uint",
					_ => throw new NotImplementedException($@"The enum type is not yet supported: {enumTypeKind}.")
				};
			}

			static CXCursor ClangEnum(CXCursor cursor)
			{
				if (cursor.kind != CXCursorKind.CXCursor_TypedefDecl)
				{
					return cursor;
				}

				var underlyingType = cursor.TypedefDeclUnderlyingType;
				// ReSharper disable once ConvertIfStatementToReturnStatement
				if (underlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
				{
					return underlyingType.NamedType.Declaration;
				}

				return underlyingType.Declaration;
			}
		}

		public StructDeclarationSyntax CreateStruct(CXCursor cursor)
		{
			var cSharpName = ClangName(cursor);
			var clangLayout = _layoutCalculator.CalculateLayout(cursor);
			var clangType = cursor.Type;
			var cSharpType = GetCSharpType(clangType);
			var clangRecord = ClangRecord(cursor);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, clangLayout.Size, clangLayout.Alignment);

			var clangFields = clangRecord.ChildrenOfKind(CXCursorKind.CXCursor_FieldDecl);
			var cSharpStructMembers = new List<MemberDeclarationSyntax>(clangFields.Length);
			foreach (var fieldC in clangFields)
			{
				AddField(cSharpType, fieldC);
			}

			cSharpStruct = cSharpStruct.AddMembers(cSharpStructMembers.ToArray());
			return cSharpStruct;

			void AddField(TypeSyntax parentStructSyntax, CXCursor clangField)
			{
				if (clangField.Type.Declaration.IsAnonymous)
				{
					var clangFieldType = clangField.Type.Declaration;
					var anonymousStruct = CreateStruct(clangFieldType);
					cSharpStructMembers.Add(anonymousStruct);
				}

				var field = CreateStructField(clangField, out var needsToBeWrapped);
				cSharpStructMembers.Add(field);

				if (!needsToBeWrapped)
                {
                    return;
                }

				var method = AddWrappedFieldMethod(clangField, parentStructSyntax);
				cSharpStructMembers.Add(method);
			}

			MethodDeclarationSyntax AddWrappedFieldMethod(CXCursor fieldC, TypeSyntax structTypeSyntax)
			{
				var clangType = fieldC.Type;
				var cSharpTypeArrayElement = GetCSharpType(clangType);
				var cSharpTypeField = PointerType(cSharpTypeArrayElement);
				return CreateStructFieldWrapperMethod(structTypeSyntax, cSharpTypeField, fieldC);
			}

			static CXCursor ClangRecord(CXCursor cursor)
			{
				if (cursor.kind != CXCursorKind.CXCursor_TypedefDecl)
				{
					return cursor;
				}

				var underlyingType = cursor.TypedefDeclUnderlyingType;
				// ReSharper disable once ConvertIfStatementToReturnStatement
				if (underlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
				{
					return underlyingType.NamedType.Declaration;
				}

				return underlyingType.Declaration;
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private FieldDeclarationSyntax CreateStructField(CXCursor clangField, out bool needsToBeWrapped)
		{
			var typeClang = clangField.Type;
			var cSharpName = ClangName(clangField);
			var cSharpType = GetCSharpType(typeClang);
			var cSharpTypeString = cSharpType.ToFullString();

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
				var isValidFixedType = IsValidCSharpTypeSpellingForFixedBuffer(cSharpTypeString);
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

					cSharpType = PredefinedType(Token(typeTokenSyntaxKind));
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
					VariableDeclaration(cSharpType)
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
			var cSharpName = ClangName(clangTypedef);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, (int)clangType.SizeOf, (int)clangType.AlignOf);

			var cSharpFieldType = ParseTypeName("IntPtr");
			var cSharpFieldVariable = VariableDeclarator(Identifier("Handle"));
			var cSharpField = FieldDeclaration(
					VariableDeclaration(cSharpFieldType)
						.WithVariables(SingletonSeparatedList(cSharpFieldVariable)))
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeFieldOffset(0, (int)clangTypedef.Type.SizeOf, 0);
			cSharpStruct = cSharpStruct.AddMembers(cSharpField);

			return cSharpStruct;
		}

		[SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "API decision.")]
		public StructDeclarationSyntax CreateExternalStruct(CXCursor clangTypedef)
		{
			var clangType = clangTypedef.Type;
			var cSharpName = ClangName(clangTypedef);

			var cSharpStruct = StructDeclaration(cSharpName)
				.WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
				.WithAttributeStructLayout(LayoutKind.Explicit, (int)clangType.SizeOf, (int)clangType.AlignOf);

			return cSharpStruct;
		}

		private IEnumerable<ParameterSyntax> CreateMethodParameters(IReadOnlyCollection<CXCursor> clangFunctionParameters)
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

		private ParameterSyntax CreateMethodParameter(CXCursor clangMethodParameter, string parameterName)
		{
			var cSharpMethodParameter = Parameter(Identifier(parameterName));

			var clangType = clangMethodParameter.Type;
			var baseClangType = GetClangBaseType(clangType);
			if (baseClangType != default && baseClangType.IsConstQualified)
			{
				cSharpMethodParameter = cSharpMethodParameter
					.WithAttribute("In");
			}

			var cSharpType = GetCSharpType(clangType);
			cSharpMethodParameter = cSharpMethodParameter
				.WithType(cSharpType);

			return cSharpMethodParameter;
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

		private VariableDeclarationSyntax CreateConstantVariable(CXCursor clangEnumConstant)
		{
			var clangType = clangEnumConstant.Type;
			var cSharpType = GetCSharpType(clangType);

			var cSharpTypeString = cSharpType.ToFullString();
			var literalSyntaxToken = cSharpTypeString switch
			{
				"short" => Literal((short)clangEnumConstant.EnumConstantDeclValue),
				"int" => Literal((int)clangEnumConstant.EnumConstantDeclValue),
				_ => throw new NotImplementedException(
					$@"The enum constant literal expression is not yet supported: {cSharpTypeString}.")
			};

			var cSharpName = ClangName(clangEnumConstant);
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

		private TypeSyntax GetCSharpType(CXType clangType, CXType? baseClangType = null)
		{
			if (_cSharpTypesByClangType.TryGetValue(clangType, out var cSharpType))
			{
				return cSharpType;
			}

			if (baseClangType == null)
			{
				baseClangType = GetClangBaseType(clangType);
			}

			string baseClangTypeSpelling;
			// TODO: Function pointers
			if (baseClangType.Value == default)
			{
				baseClangTypeSpelling = "void";
			}
			else
			{
				baseClangTypeSpelling = ClangTypeToCSharpTypeString(baseClangType.Value);
			}

			if (clangType.Declaration.IsAnonymous)
			{
				var clangName = ClangName(clangType.Declaration);
				cSharpType = ParseTypeName(clangName);
			}
			else
			{
				cSharpType = clangType.TypeClass switch
				{
					CX_TypeClass.CX_TypeClass_Pointer when baseClangType.Value == default => PointerType(ParseTypeName(baseClangTypeSpelling)),
					CX_TypeClass.CX_TypeClass_Typedef when baseClangType.Value == default => PointerType(ParseTypeName(baseClangTypeSpelling)),
					CX_TypeClass.CX_TypeClass_Pointer => PointerType(GetCSharpType(clangType.PointeeType, baseClangType)),
					CX_TypeClass.CX_TypeClass_Typedef => ParseTypeName(clangType.Spelling.CString
						.Replace("const ", string.Empty).Trim()),
					CX_TypeClass.CX_TypeClass_Enum => ParseTypeName(baseClangTypeSpelling.Replace("enum ", string.Empty)
						.Trim()),
					CX_TypeClass.CX_TypeClass_Record => ParseTypeName(baseClangTypeSpelling.Replace("struct ", string.Empty)
						.Trim()),
					CX_TypeClass.CX_TypeClass_Elaborated => GetCSharpType(clangType.NamedType, baseClangType),
					CX_TypeClass.CX_TypeClass_Builtin => ParseTypeName(ClangTypeToCSharpTypeString(baseClangType!.Value)),
					CX_TypeClass.CX_TypeClass_ConstantArray => ParseTypeName(baseClangTypeSpelling),
					_ => throw new NotImplementedException()
				};
			}

			_cSharpTypesByClangType.Add(clangType, cSharpType);
			return cSharpType;
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

		private static string ClangTypeToCSharpTypeString(CXType clangType)
		{
			var result = clangType.kind switch
			{
				CXTypeKind.CXType_Void => "void",
				CXTypeKind.CXType_Bool => "CBool",
				CXTypeKind.CXType_Char_S => "sbyte",
				CXTypeKind.CXType_Char_U => "byte",
				CXTypeKind.CXType_UChar => "byte",
				CXTypeKind.CXType_UShort => "ushort",
				CXTypeKind.CXType_UInt => "uint",
				CXTypeKind.CXType_ULong => clangType.SizeOf == 8 ? "ulong" : "uint",
				CXTypeKind.CXType_ULongLong => "ulong",
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

		private string ClangName(CXCursor cursor)
		{
			if (_cSharpNamesByClangCursor.TryGetValue(cursor, out var name))
			{
				return name;
			}

			if (cursor.IsAnonymous)
			{
				var fieldName = string.Empty;
				var parent = cursor.SemanticParent;
				parent.VisitChildren(child =>
				{
					if (child.kind == CXCursorKind.CXCursor_FieldDecl && child.Type.Declaration == cursor)
					{
						fieldName = child.Spelling.CString;
					}
				});

				if (cursor.kind == CXCursorKind.CXCursor_UnionDecl)
				{
					name = $"Anonymous_Union_{fieldName}";
				}
				else
				{
					name = $"Anonymous_Struct_{fieldName}";
				}
			}
			else
			{
				name = cursor.Spelling.CString;
				if (string.IsNullOrEmpty(name))
				{
					var clangType = cursor.Type;
					if (clangType.kind == CXTypeKind.CXType_Pointer)
					{
						clangType = clangType.PointeeType;
					}

					name = clangType.Spelling.CString;
				}
			}

			_cSharpNamesByClangCursor.Add(cursor, name);
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

				if (name == "lock" || name == "string" || name == "base" || name == "ref")
				{
					name = $"@{name}";
				}
			}

			return name;
		}
	}
}
