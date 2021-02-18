// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ClangSharp.Interop;

namespace C2CS
{
	internal class ClangCodeExplorer
	{
		private CXTranslationUnit _translationUnit = null!;
		private readonly HashSet<CXCursor> _visitedCursors = new();
		private readonly HashSet<CXType> _visitedTypes = new();

		private string _filePath = string.Empty;
		private string _directoryPath = string.Empty;
		private static ClangCodeExplorer _instance = null!;

		private readonly List<CXCursor> _clangFunctions = new();
		private readonly List<CXCursor> _clangEnums = new();
		private readonly List<CXCursor> _clangRecords = new();
		private readonly List<CXCursor> _clangOpaqueTypes = new();
		private readonly List<CXCursor> _clangExternalTypes = new();

		public ClangCodeExplorer()
		{
			_instance = this;
		}

		public ClangCodeExploreResult Explore(CXTranslationUnit translationUnit)
		{
			_translationUnit = translationUnit;

			_filePath = Path.GetFullPath(_translationUnit.Spelling.CString);
			_directoryPath = Path.GetDirectoryName(_filePath) ?? string.Empty;

			var clangExternalFunctionsBuilder = ImmutableArray.CreateBuilder<CXCursor>();
			_translationUnit.Cursor.VisitChildren(child =>
			{
				if (child.Kind == CXCursorKind.CXCursor_FunctionDecl && child.Linkage == CXLinkageKind.CXLinkage_External)
				{
					child.Location.GetFileLocation(out var file, out _, out _, out _);
					var cursorFilePath = file.TryGetRealPathName().CString;
					var cursorDirectoryPath = Path.GetDirectoryName(cursorFilePath);
					if (cursorDirectoryPath == _directoryPath)
					{
						clangExternalFunctionsBuilder.Add(child);
					}
				}
			});
			var clangExternalFunctions = clangExternalFunctionsBuilder.ToImmutable();

			foreach (var clangFunction in clangExternalFunctions)
			{
				VisitCursor(clangFunction);
			}

			var result = new ClangCodeExploreResult()
			{
				Functions = _clangFunctions.ToImmutableArray(),
				Records = _clangRecords.ToImmutableArray(),
				Enums = _clangEnums.ToImmutableArray(),
				OpaqueTypes = _clangOpaqueTypes.ToImmutableArray(),
				ExternalTypes = _clangExternalTypes.ToImmutableArray()
			};

			_visitedCursors.Clear();
			_visitedTypes.Clear();
			_clangFunctions.Clear();
			_clangRecords.Clear();
			_clangEnums.Clear();
			_clangOpaqueTypes.Clear();
			_clangExternalTypes.Clear();

			return result;
		}

		private bool CanVisitCursor(CXCursor cursor)
		{
			if (_visitedCursors.Contains(cursor))
			{
				return false;
			}

			_visitedCursors.Add(cursor);

			return true;
		}

		[SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
		private void VisitCursor(CXCursor cursor)
		{
			if (!CanVisitCursor(cursor))
			{
				return;
			}

			if (cursor.IsDeclaration)
			{
				VisitDeclaration(cursor);
			}
			else if (cursor.IsReference)
			{
				VisitCursor(cursor.Type.Declaration);
			}
		}

		private bool CanVisitType(CXType type)
		{
			if (_visitedTypes.Contains(type))
			{
				return false;
			}

			_visitedTypes.Add(type);

			return true;
		}

		[SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
		private void VisitType(CXType type)
		{
			if (!CanVisitType(type))
			{
				return;
			}

			switch (type.TypeClass)
			{
				case CX_TypeClass.CX_TypeClass_Attributed:
					VisitType(type.ModifiedType);
					break;
				case CX_TypeClass.CX_TypeClass_Elaborated:
					VisitType(type.NamedType);
					break;
				case CX_TypeClass.CX_TypeClass_Pointer:
				case CX_TypeClass.CX_TypeClass_LValueReference:
				case CX_TypeClass.CX_TypeClass_RValueReference:
					VisitType(type.PointeeType);
					break;
				case CX_TypeClass.CX_TypeClass_Typedef:
					VisitCursor(type.Declaration);
					break;
				case CX_TypeClass.CX_TypeClass_IncompleteArray:
					VisitType(type.ElementType);
					break;
				case CX_TypeClass.CX_TypeClass_FunctionProto:
					VisitFunctionProto(type);
					break;
				case CX_TypeClass.CX_TypeClass_Record:
				case CX_TypeClass.CX_TypeClass_Enum:
					VisitCursor(type.Declaration);
					break;
				case CX_TypeClass.CX_TypeClass_ConstantArray:
					VisitType(type.ElementType);
					break;
				case CX_TypeClass.CX_TypeClass_Builtin:
					// Ignored
					break;
				default:
					throw UnsupportedType(type);
			}
		}

		private void VisitDeclaration(CXCursor declaration)
		{
			switch (declaration.DeclKind)
			{
				case CX_DeclKind.CX_DeclKind_Enum:
					VisitEnum(declaration);
					break;
				case CX_DeclKind.CX_DeclKind_Record:
					VisitRecord(declaration);
					break;
				case CX_DeclKind.CX_DeclKind_Typedef:
					VisitTypedef(declaration);
					break;
				case CX_DeclKind.CX_DeclKind_Function:
					VisitFunction(declaration);
					break;
				case CX_DeclKind.CX_DeclKind_ParmVar:
				case CX_DeclKind.CX_DeclKind_Field:
					VisitType(declaration.Type);
					break;
				default:
					var up = UnsupportedDeclaration(declaration);
					throw up;
			}
		}

		private void VisitFunction(CXCursor function)
		{
			VisitType(function.ResultType);

			function.VisitChildren(child =>
			{
				var explorer = _instance;
				explorer.VisitCursor(child);
			});

			_clangFunctions.Add(function);
		}

		private void VisitEnum(CXCursor @enum)
		{
			_clangEnums.Add(@enum);
		}

		private void VisitRecord(CXCursor record)
		{
			var isTypeForward = record != record.Definition;
			if (isTypeForward)
			{
				return;
			}

			record.VisitChildren(child =>
			{
				var explorer = _instance;
				explorer.VisitCursor(child);
			});

			_clangRecords.Add(record);
		}

		private void VisitTypedef(CXCursor typedef)
		{
			if (typedef.TypedefDeclUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Pointer)
			{
				var pointerType = typedef.TypedefDeclUnderlyingType;

				if (pointerType.PointeeType.kind == CXTypeKind.CXType_Void)
				{
					_clangOpaqueTypes.Add(typedef);
				}

				if (pointerType.PointeeType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
				{
					var elaboratedType = pointerType.PointeeType;
					var namedType = elaboratedType.NamedType;
					if (namedType.kind == CXTypeKind.CXType_Record)
					{
						var recordType = namedType;
						var childrenCount = 0;
						recordType.Declaration.VisitChildren(_ =>
						{
							childrenCount += 1;
						});
						if (childrenCount == 0)
						{
							_clangOpaqueTypes.Add(typedef);
						}
					}
				}
				else if (pointerType.PointeeType.TypeClass == CX_TypeClass.CX_TypeClass_FunctionProto)
				{
					var functionProtoType = pointerType.PointeeType;
					VisitType(functionProtoType.ResultType);
					functionProtoType.VisitChildren(child =>
					{
						var explorer = _instance;
						explorer.VisitType(child.Type);
					});
				}
			}

			typedef.Location.GetFileLocation(out var file, out _, out _, out _);
			var cursorFilePath = file.TryGetRealPathName().CString;
			var cursorDirectoryPath = Path.GetDirectoryName(cursorFilePath);
			if (cursorDirectoryPath != _directoryPath)
			{
				VisitExternalType(typedef);
				return;
			}

			VisitType(typedef.TypedefDeclUnderlyingType);
		}

		private void VisitExternalType(CXCursor typedef)
		{
			_clangExternalTypes.Add(typedef);
		}

		private void VisitFunctionProto(CXType functionProto)
		{
			VisitType(functionProto.ResultType);

			functionProto.VisitChildren(child =>
			{
				var explorer = _instance;
				explorer.VisitType(child.Type);
			});
		}

		private static Exception UnsupportedDeclaration(CXCursor declaration)
		{
			return new NotImplementedException($"Not yet supported declaration kind `{declaration.DeclKind}`: '{declaration}'.");
		}

		private static Exception UnsupportedType(CXType type)
		{
			return new NotImplementedException($"Not yet supported type class `{type.TypeClass}`: '{type}'.");
		}
	}
}
