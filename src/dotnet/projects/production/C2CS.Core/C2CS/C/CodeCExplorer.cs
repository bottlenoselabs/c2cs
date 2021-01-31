// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using ClangSharp;
using ClangSharp.Interop;
using ClangCursor = ClangSharp.Cursor;
using Type = ClangSharp.Type;

namespace C2CS
{
	internal class CodeCExplorer
	{
		private readonly HashSet<ClangCursor> _visitedCursors = new();
		private string _filePath = string.Empty;
		private string[] _includeDirectories = null!;

		public event CodeCFoundEnumDelegate? EnumFound;

		public event CodeCFoundRecordDelegate? RecordFound;

		public event CodeCFoundFunctionDelegate? FunctionFound;

		public event CodeCFoundTypeAliasDelegate? TypeAliasFound;

		public event CodeCFoundFunctionProtoDelegate? FunctionProtoFound;

		public void Explore(TranslationUnit translationUnit, IEnumerable<string>? includeDirectories)
		{
			var translationUnitDeclaration = translationUnit.TranslationUnitDecl;
			_filePath = Path.GetFullPath(translationUnitDeclaration.Spelling);
			_includeDirectories = includeDirectories?.ToArray() ?? Array.Empty<string>();

			foreach (var declaration in translationUnitDeclaration.Decls)
			{
				if (declaration.Kind != CX_DeclKind.CX_DeclKind_Function)
				{
					continue;
				}

				if (declaration.Handle.Linkage != CXLinkageKind.CXLinkage_External)
				{
					continue;
				}

				VisitCursor(declaration);
			}
		}

		[SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
		private CodeCExploreResult VisitCursor(Cursor cursor)
		{
			var cursorFilePath = cursor.GetFilePath();

			if (cursorFilePath != _filePath)
			{
				if (cursorFilePath.StartsWith("/Applications/Xcode.app/", StringComparison.Ordinal))
				{
					return CodeCExploreResult.Ignored;
				}

				// ReSharper disable once LoopCanBeConvertedToQuery
				foreach (var includeDirectory in _includeDirectories)
				{
					if (cursorFilePath.StartsWith(includeDirectory, StringComparison.Ordinal))
					{
						return CodeCExploreResult.Ignored;
					}
				}

				throw new NotImplementedException();
			}

			if (!CanVisitCursor(cursor))
			{
				return CodeCExploreResult.Ignored;
			}

			return cursor switch
			{
				Decl declaration => VisitDeclaration(declaration),
				BinaryOperator _ => CodeCExploreResult.Ignored,
				IntegerLiteral _ => CodeCExploreResult.Ignored,
				_ => VisitUnsupportedCursor(cursor)
			};
		}

		private CodeCExploreResult VisitDeclaration(Decl declaration)
		{
			return declaration switch
			{
				EnumDecl @enum => VisitEnum(@enum),
				RecordDecl record => VisitRecord(record),
				TypedefDecl alias => VisitTypeAlias(alias),
				FunctionDecl function => VisitFunction(function),
				ParmVarDecl parameter => VisitType(parameter.Type),
				FieldDecl field => VisitType(field.Type),
				_ => VisitUnsupportedDeclaration(declaration)
			};
		}

		private CodeCExploreResult VisitFunction(FunctionDecl function)
		{
			var allIgnored = true;

			var result = VisitType(function.ReturnType);
			if (result == CodeCExploreResult.Processed)
			{
				allIgnored = false;
			}

			// ReSharper disable once ForCanBeConvertedToForeach
			for (var i = 0; i < function.Parameters.Count; i++)
			{
				var param = function.Parameters[i];
				var result2 = VisitCursor(param);
				if (result2 == CodeCExploreResult.Processed)
				{
					allIgnored = false;
				}
			}

			OnFoundFunction(function);

			return allIgnored ? CodeCExploreResult.Ignored : CodeCExploreResult.Processed;
		}

		private CodeCExploreResult VisitEnum(EnumDecl @enum)
		{
			OnFoundEnum(@enum);
			return CodeCExploreResult.Processed;
		}

		private CodeCExploreResult VisitRecord(RecordDecl record)
		{
			var allIgnored = true;

			foreach (var field in record.Fields)
			{
				var result = VisitCursor(field);
				if (result == CodeCExploreResult.Processed)
				{
					allIgnored = false;
				}
			}

			OnFoundRecord(record);

			return allIgnored ? CodeCExploreResult.Ignored : CodeCExploreResult.Processed;
		}

		private CodeCExploreResult VisitTypeAlias(TypedefDecl typeAlias)
		{
			var underlingType = typeAlias.UnderlyingType;
			if (underlingType is PointerType pointerType)
			{
				underlingType = pointerType.PointeeType;
			}

			switch (underlingType)
			{
				case BuiltinType:
					OnFoundTypeAlias(typeAlias);
					break;
				case FunctionProtoType functionProtoType:
					OnFoundFunctionPointer(functionProtoType);
					break;
			}

			return VisitType(typeAlias.UnderlyingType);
		}

		private bool CanVisitCursor(ClangCursor cursor)
		{
			if (_visitedCursors.Contains(cursor))
			{
				return false;
			}

			_visitedCursors.Add(cursor);

			return true;
		}

		private CodeCExploreResult VisitType(Type type)
		{
			return type switch
			{
				AttributedType attributedType => VisitType(attributedType.ModifiedType),
				ElaboratedType elaboratedType => VisitType(elaboratedType.NamedType),
				PointerType pointerType => VisitType(pointerType.PointeeType),
				ReferenceType referenceType => VisitType(referenceType.PointeeType),
				TypedefType definitionType => VisitCursor(definitionType.Decl),
				IncompleteArrayType arrayType => VisitType(arrayType.ElementType),
				BuiltinType _ => CodeCExploreResult.Ignored,
				FunctionProtoType functionType => VisitFunctionType(functionType),
				RecordType recordType => VisitCursor(recordType.Decl),
				EnumType enumType => VisitCursor(enumType.Decl),
				ConstantArrayType arrayType => VisitType(arrayType.ElementType),
				_ => VisitUnsupportedType(type)
			};
		}

		private CodeCExploreResult VisitFunctionType(FunctionProtoType functionType)
		{
			var allIgnored = true;
			foreach (var parameterType in functionType.ParamTypes)
			{
				var result = VisitType(parameterType);
				if (result == CodeCExploreResult.Processed)
				{
					allIgnored = false;
				}
			}

			return allIgnored ? CodeCExploreResult.Ignored : CodeCExploreResult.Processed;
		}

		private void OnFoundEnum(EnumDecl enumDeclaration)
		{
			EnumFound?.Invoke(enumDeclaration);
		}

		private void OnFoundRecord(RecordDecl recordDeclaration)
		{
			RecordFound?.Invoke(recordDeclaration);
		}

		private void OnFoundFunction(FunctionDecl functionDeclaration)
		{
			FunctionFound?.Invoke(functionDeclaration);
		}

		private void OnFoundTypeAlias(TypedefDecl typeAlias)
		{
			TypeAliasFound?.Invoke(typeAlias);
		}

		private void OnFoundFunctionPointer(FunctionProtoType functionProtoType)
		{
			FunctionProtoFound?.Invoke(functionProtoType);
		}

		private static CodeCExploreResult VisitUnsupportedCursor(Cursor cursor)
		{
			throw new NotImplementedException($"Not yet supported '{cursor}'.");
		}

		private static CodeCExploreResult VisitUnsupportedDeclaration(Decl declaration)
		{
			throw new NotImplementedException($"Not yet supported '{declaration}'.");
		}

		private static CodeCExploreResult VisitUnsupportedType(Type type)
		{
			throw new NotImplementedException($"Not yet supported '{type}'.");
		}
	}
}
