// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
		private string[] _includeDirectories = null!;
		private static ClangCodeExplorer _instance = null!;

		private readonly List<CXCursor> _opaqueRecordsC = new();
		private readonly List<CXCursor> _enumsC = new();
		private readonly List<CXCursor> _recordsC = new();
		private readonly List<CXCursor> _functionsC = new();

		public event ClangFoundEnumDelegate? EnumFound;

		public event ClangFoundRecordDelegate? RecordFound;

		public event ClangFoundFunctionDelegate? FunctionFound;

		public event ClangFoundOpaqueTypeDelegate? OpaqueTypeFound;

		public event ClangFoundExternalTypeDelegate? ExternalTypeFound;

		public ClangCodeExplorer()
		{
			_instance = this;
		}

		public void Explore(CXTranslationUnit translationUnit, IEnumerable<string>? includeDirectories)
		{
			_translationUnit = translationUnit;

			_filePath = Path.GetFullPath(_translationUnit.Spelling.CString);
			_directoryPath = Path.GetDirectoryName(_filePath) ?? string.Empty;

			_includeDirectories = includeDirectories?.ToArray() ?? Array.Empty<string>();

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

			foreach (var typedefDecl in _opaqueRecordsC)
			{
				OnFoundOpaqueType(typedefDecl);
			}

			foreach (var @enum in _enumsC)
			{
				OnFoundEnum(@enum);
			}

			foreach (var record in _recordsC)
			{
				OnFoundRecord(record);
			}

			foreach (var function in _functionsC)
			{
				OnFoundFunction(function);
			}
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

			_functionsC.Add(function);
		}

		private void VisitEnum(CXCursor @enum)
		{
			_enumsC.Add(@enum);
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

			_recordsC.Add(record);
		}

		private void VisitTypedef(CXCursor typedef)
		{
			if (typedef.TypedefDeclUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Pointer)
			{
				var pointerType = typedef.TypedefDeclUnderlyingType;

				if (pointerType.PointeeType.kind == CXTypeKind.CXType_Void)
				{
					_opaqueRecordsC.Add(typedef);
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
							_opaqueRecordsC.Add(typedef);
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
				// ReSharper disable once LoopCanBeConvertedToQuery
				foreach (var includeDirectory in _includeDirectories)
				{
					if (!cursorFilePath.StartsWith(includeDirectory, StringComparison.Ordinal))
					{
						continue;
					}

					VisitExternalType(typedef);
					break;
				}

				return;
			}

			VisitType(typedef.TypedefDeclUnderlyingType);
		}

		private void VisitExternalType(CXCursor typedef)
		{
			OnFoundExternalType(typedef);
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

		private void OnFoundEnum(CXCursor enumDeclaration)
		{
			EnumFound?.Invoke(enumDeclaration);
		}

		private void OnFoundRecord(CXCursor recordDeclaration)
		{
			RecordFound?.Invoke(recordDeclaration);
		}

		private void OnFoundFunction(CXCursor functionDeclaration)
		{
			FunctionFound?.Invoke(functionDeclaration);
		}

		private void OnFoundOpaqueType(CXCursor typedef)
		{
			OpaqueTypeFound?.Invoke(typedef);
		}

		private void OnFoundExternalType(CXCursor typedef)
		{
			ExternalTypeFound?.Invoke(typedef);
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
