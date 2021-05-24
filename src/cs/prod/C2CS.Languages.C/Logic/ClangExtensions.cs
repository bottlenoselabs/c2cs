// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using static libclang;

namespace C2CS.Languages.C
{
	public static unsafe class ClangExtensions
	{
		public struct ClangVisitNode
		{
			public CXCursor Cursor;
			public CXCursor CursorParent;

			public ClangVisitNode(CXCursor cursor, CXCursor cursorParent)
			{
				Cursor = cursor;
				CursorParent = cursorParent;
			}
		}

		private static VisitInstance[] _visitInstances = Array.Empty<VisitInstance>();
		private static int _visitsCount;

		private readonly struct VisitInstance
		{
			public readonly VisitPredicate Predicate;
			public readonly ImmutableArray<ClangVisitNode>.Builder NodeBuilder;

			public VisitInstance(VisitPredicate predicate)
			{
				Predicate = predicate;
				NodeBuilder = ImmutableArray.CreateBuilder<ClangVisitNode>();
			}
		}

		public delegate bool VisitPredicate(CXCursor child, CXCursor parent);

		private static readonly CXCursorVisitor Visit;

		static ClangExtensions()
		{
			Visit.Pointer = &Visitor;
		}

		[SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Justification = "Result is useless.")]
		public static ImmutableArray<ClangVisitNode> GetDescendents(
			this CXCursor cursor, VisitPredicate predicate)
		{
			var visitData = new VisitInstance(predicate);
			var visitsCount = Interlocked.Increment(ref _visitsCount);
			if (visitsCount > _visitInstances.Length)
			{
				Array.Resize(ref _visitInstances, visitsCount * 2);
			}

			_visitInstances[visitsCount - 1] = visitData;

			CXClientData clientData = default(CXClientData);
			clientData.Data = (void*) _visitsCount;
			clang_visitChildren(cursor, Visit, clientData);

			Interlocked.Decrement(ref _visitsCount);

			var result = visitData.NodeBuilder.ToImmutable();
			return result;
		}

		[UnmanagedCallersOnly]
		private static CXChildVisitResult Visitor(CXCursor child, CXCursor parent, CXClientData clientData)
		{
			var index = (int)clientData.Data;
			var data = _visitInstances[index - 1];

			var result = data.Predicate(child, parent);
			if (!result)
			{
				return CXChildVisitResult.CXChildVisit_Continue;
			}

			var node = new ClangVisitNode(child, parent);
			data.NodeBuilder.Add(node);

			return CXChildVisitResult.CXChildVisit_Continue;
		}

		public static bool IsSystem(this CXCursor cursor)
		{
			var location = clang_getCursorLocation(cursor);
			var isInSystemHeader = clang_Location_isInSystemHeader(location) > 0U;
			return isInSystemHeader;
		}

		public static bool IsSystem(this CXType type)
		{
			var kind = type.kind;

			switch (kind)
			{
				case CXTypeKind.CXType_Void:
				case CXTypeKind.CXType_Bool:
				case CXTypeKind.CXType_Char_S:
				case CXTypeKind.CXType_Char_U:
				case CXTypeKind.CXType_UChar:
				case CXTypeKind.CXType_UShort:
				case CXTypeKind.CXType_UInt:
				case CXTypeKind.CXType_ULong:
				case CXTypeKind.CXType_ULongLong:
				case CXTypeKind.CXType_Short:
				case CXTypeKind.CXType_Int:
				case CXTypeKind.CXType_Long:
				case CXTypeKind.CXType_LongLong:
				case CXTypeKind.CXType_Float:
				case CXTypeKind.CXType_Double:
					return true;
				case CXTypeKind.CXType_Pointer:
					var pointeeType = clang_getPointeeType(type);
					return IsSystem(pointeeType);
				case CXTypeKind.CXType_ConstantArray:
				case CXTypeKind.CXType_IncompleteArray:
					var elementType = clang_getElementType(type);
					return IsSystem(elementType);
				case CXTypeKind.CXType_Typedef:
				case CXTypeKind.CXType_Elaborated:
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
				case CXTypeKind.CXType_FunctionProto:
					var declaration = clang_getTypeDeclaration(type);
					return IsSystem(declaration);
				case CXTypeKind.CXType_FunctionNoProto:
					return false;
				case CXTypeKind.CXType_Attributed:
					var modifiedType = clang_Type_getModifiedType(type);
					return IsSystem(modifiedType);
				default:
					throw new NotImplementedException();
			}
		}

		public static (string FilePath, int LineNumber, int LineColumn) GetLocation(this CXCursor clangCursor)
		{
			var location = clang_getCursorLocation(clangCursor);
			CXFile file;
			ulong lineNumber;
			ulong lineColumn;
			ulong offset;
			clang_getFileLocation(location, &file, &lineNumber, &lineColumn, &offset);

			var handle = (IntPtr)file.Data;
			if (handle == IntPtr.Zero)
			{
				return (string.Empty, 0, 0);
			}

			var fileName = clang_getFileName(file);
			var cString = clang_getCString(fileName);
			var fileNamePath = NativeRuntime.MapString(cString);

			var result = (fileNamePath, (int)lineNumber, (int)lineColumn);
			return result;
		}

		public static string GetName(this CXCursor clangCursor)
		{
			var spelling = clang_getCursorSpelling(clangCursor);

			var cString = clang_getCString(spelling);
			if ((IntPtr) cString == IntPtr.Zero)
			{
				return string.Empty;
			}

			var result = NativeRuntime.MapString(cString);
			return result;
		}

		public static string GetName(this CXType clangType)
		{
			var spelling = clang_getTypeSpelling(clangType);

			var cString = clang_getCString(spelling);
			if ((IntPtr) cString == IntPtr.Zero)
			{
				return string.Empty;
			}

			var result = NativeRuntime.MapString(cString);
			if (result.Contains("struct "))
			{
				result = result.Replace("struct ", string.Empty);
			}

			if (result.Contains("enum "))
			{
				result = result.Replace("enum ", string.Empty);
			}

			if (result.Contains("const "))
			{
				result = result.Replace("const ", string.Empty);
			}

			return result;
		}

		public static bool IsPrimitive(this CXType type)
		{
			return type.kind switch
			{
				CXTypeKind.CXType_Void => true,
				CXTypeKind.CXType_Bool => true,
				CXTypeKind.CXType_Char_S => true,
				CXTypeKind.CXType_Char_U => true,
				CXTypeKind.CXType_UChar => true,
				CXTypeKind.CXType_UShort => true,
				CXTypeKind.CXType_UInt => true,
				CXTypeKind.CXType_ULong => true,
				CXTypeKind.CXType_ULongLong => true,
				CXTypeKind.CXType_Short => true,
				CXTypeKind.CXType_Int => true,
				CXTypeKind.CXType_Long => true,
				CXTypeKind.CXType_LongLong => true,
				CXTypeKind.CXType_Float => true,
				CXTypeKind.CXType_Double => true,
				_ => false
			};
		}

		public static CXType GetResultType(this CXType type)
		{
			var resultType = clang_getResultType(type);
			if (resultType.kind == CXTypeKind.CXType_Pointer)
			{
				resultType = clang_getPointeeType(resultType);
			}

			return resultType;
		}
	}
}
