// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Runtime.InteropServices;
using ClangSharp.Interop;

namespace C2CS.Languages.C
{
	public static unsafe class ClangExtensions
	{
		public delegate void VisitChildCursorAction(CXCursor child, CXCursor parent);

		private static readonly CXCursorVisitor Visit = Visitor;

		public static void VisitChildren(this CXCursor cursor, VisitChildCursorAction visitAction)
		{
			var handle = GCHandle.Alloc(visitAction);
			var clientData = new CXClientData((IntPtr)handle);

			cursor.VisitChildren(Visit, clientData);

			handle.Free();
		}

		private static CXChildVisitResult Visitor(CXCursor childCursor, CXCursor childParent, void* data)
		{
			var handle = (GCHandle)(IntPtr)data;
			var action = (VisitChildCursorAction)handle.Target!;
			action(childCursor, childParent);
			return CXChildVisitResult.CXChildVisit_Continue;
		}

		public static bool IsSystemCursor(this CXCursor cursor)
		{
			return cursor.Location.IsInSystemHeader;
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
					return IsSystemType(type.PointeeType);
				case CXTypeKind.CXType_ConstantArray:
				case CXTypeKind.CXType_Typedef:
				case CXTypeKind.CXType_Elaborated:
				case CXTypeKind.CXType_Record:
				case CXTypeKind.CXType_Enum:
				case CXTypeKind.CXType_FunctionProto:
					return IsSystemCursor(type.Declaration);
				default:
					throw new NotImplementedException();
			}
		}
	}
}
