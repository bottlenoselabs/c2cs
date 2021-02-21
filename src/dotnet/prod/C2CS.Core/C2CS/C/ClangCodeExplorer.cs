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

		private readonly List<CXCursor> _clangFunctions = new();
		private readonly List<CXCursor> _clangEnums = new();
		private readonly List<CXCursor> _clangRecords = new();
		private readonly List<CXCursor> _clangOpaqueTypes = new();
		private readonly List<CXCursor> _clangExternalTypes = new();

		public ClangCodeExploreResult Explore(CXTranslationUnit translationUnit)
		{
			_translationUnit = translationUnit;

			_filePath = Path.GetFullPath(_translationUnit.Spelling.CString);
			_directoryPath = Path.GetDirectoryName(_filePath) ?? string.Empty;

			var clangExternalFunctions = GetExternFunctions();
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

		private ImmutableArray<CXCursor> GetExternFunctions()
		{
			var externFunctions = new List<CXCursor>();
			_translationUnit.Cursor.VisitChildren(child =>
			{
				if (child.Kind != CXCursorKind.CXCursor_FunctionDecl ||
				    child.Linkage != CXLinkageKind.CXLinkage_External)
				{
					return;
				}

				if (IsExternalCursor(child))
                {
                    return;
                }

				externFunctions.Add(child);
			});

			return externFunctions.ToImmutableArray();
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
			function.VisitChildren(VisitCursor);
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

			record.VisitChildren(VisitCursor);
			_clangRecords.Add(record);
		}

		private void VisitTypedef(CXCursor typedef)
		{
			if (typedef.TypedefDeclUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Pointer)
			{
				var pointeeType = typedef.TypedefDeclUnderlyingType.PointeeType;
				if (pointeeType.kind == CXTypeKind.CXType_Void)
				{
					VisitOpaqueType(typedef);
				}
				else if (pointeeType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
				{
					var namedType = pointeeType.NamedType;
					if (namedType.kind == CXTypeKind.CXType_Record)
					{
						var recordType = namedType;
						var childrenCount = 0;
						recordType.Declaration.VisitChildren(_ => childrenCount += 1);
						if (childrenCount == 0)
						{
							VisitOpaqueType(typedef);
						}
					}
				}
				else if (pointeeType.TypeClass == CX_TypeClass.CX_TypeClass_FunctionProto)
				{
					VisitFunctionProto(pointeeType);
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (IsExternalCursor(typedef))
			{
				VisitExternalType(typedef);
			}
			else
			{
				VisitType(typedef.TypedefDeclUnderlyingType);
			}
		}

		private bool IsExternalCursor(CXCursor cursor)
		{
			cursor.Location.GetFileLocation(out var file, out _, out _, out _);
			var cursorFilePath = file.TryGetRealPathName().CString;
			var cursorDirectoryPath = Path.GetDirectoryName(cursorFilePath);
			return cursorDirectoryPath != _directoryPath;
		}

		private void VisitOpaqueType(CXCursor opaqueType)
		{
			_clangOpaqueTypes.Add(opaqueType);
		}

		private void VisitExternalType(CXCursor typedef)
		{
			_clangExternalTypes.Add(typedef);
		}

		private void VisitFunctionProto(CXType functionProto)
		{
			VisitType(functionProto.ResultType);
			functionProto.VisitChildren(child => VisitType(child.Type));
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
