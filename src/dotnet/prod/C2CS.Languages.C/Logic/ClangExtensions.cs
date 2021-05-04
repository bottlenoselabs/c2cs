// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using C2CS.Tools;
using static libclang;

namespace C2CS.Languages.C
{
	public static unsafe class ClangExtensions
	{
		private static VisitData[] _visits = Array.Empty<VisitData>();
		private static int _visitsCount;

		private readonly struct VisitData
		{
			public readonly VisitChildCursorAction Action;
			public readonly int Depth;

			public VisitData(VisitChildCursorAction action, int depth)
			{
				Action = action;
				Depth = depth;
			}
		}

		public delegate void VisitChildCursorAction(CXCursor child, CXCursor parent, int depth);

		private static readonly CXCursorVisitor Visit;

		static ClangExtensions()
		{
			Visit.Pointer = &Visitor;
		}

		public static bool VisitChildren(this CXCursor cursor, int depth, VisitChildCursorAction visitAction)
		{
			var visitData = new VisitData(visitAction, depth);
			var visitsCount = Interlocked.Increment(ref _visitsCount);
			if (visitsCount > _visits.Length)
			{
				Array.Resize(ref _visits, visitsCount * 2);
			}

			_visits[visitsCount - 1] = visitData;

			var clientData = default(CXClientData);
			clientData.Pointer = (void*) _visitsCount;
			var didBreak = clang_visitChildren(cursor, Visit, clientData) != 0;

			Interlocked.Decrement(ref _visitsCount);
			return didBreak;
		}

		[UnmanagedCallersOnly]
		private static CXChildVisitResult Visitor(CXCursor childCursor, CXCursor childParent, CXClientData data)
		{
			var visitIndex = (int)data.Pointer;
			var visitData = _visits[visitIndex - 1];
			visitData.Action(childCursor, childParent, visitData.Depth);
			return CXChildVisitResult.CXChildVisit_Continue;
		}

		public static bool IsSystemCursor(this CXCursor cursor)
		{
			var location = clang_getCursorLocation(cursor);
			var isInSystemHeader = clang_Location_isInSystemHeader(location) > 0U;
			return isInSystemHeader;
		}

		public static bool IsSystemType(this CXType type)
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
					return IsSystemType(pointeeType);
				case CXTypeKind.CXType_ConstantArray:
				case CXTypeKind.CXType_IncompleteArray:
				case CXTypeKind.CXType_Typedef:
				case CXTypeKind.CXType_Elaborated:
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
				case CXTypeKind.CXType_FunctionProto:
					var declaration = clang_getTypeDeclaration(type);
					return IsSystemCursor(declaration);
				default:
					throw new NotImplementedException();
			}
		}

		public static (string FilePath, int LineNumber, int LineColumn) GetLocation(this CXCursor clangCursor)
		{
			var location = clang_getCursorLocation(clangCursor);
			CXFile file;
			uint lineNumber;
			uint lineColumn;
			uint offset;
			clang_getFileLocation(location, &file, &lineNumber, &lineColumn, &offset);

			var handle = (IntPtr)file.Pointer;
			if (handle == IntPtr.Zero)
			{
				return (string.Empty, 0, 0);
			}

			var fileName = clang_getFileName(file);
			var cString = clang_getCString(fileName);
			var fileNamePath = NativeTools.MapString(cString);

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

			var result = NativeTools.MapString(cString);
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

			var result = NativeTools.MapString(cString);
			return result;
		}
	}
}
