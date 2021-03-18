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

		public static bool IsInSystem(this CXCursor cursor)
		{
			return cursor.Location.IsInSystemHeader;
		}
	}
}
