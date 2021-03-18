// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using ClangSharp.Interop;

namespace C2CS.Languages.C
{
	public static unsafe class ClangExtensions
	{
		public delegate void VisitChildCursorAction(CXCursor child, CXCursor parent);

		public delegate void VisitChildAction(CXCursor child);

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

		public static void VisitChildren(this CXType type, VisitChildAction visitAction)
		{
			var clientData = new CXClientData(IntPtr.Zero);

			type.VisitFields(Visitor, clientData);

			CXVisitorResult Visitor(CXCursor childCursor, void* data)
			{
				visitAction(childCursor);
				return CXVisitorResult.CXVisit_Continue;
			}
		}

		public static ImmutableArray<CXCursor> ChildrenOfKind(this CXCursor cursor, CXCursorKind kind)
		{
			var childrenBuilder = ImmutableArray.CreateBuilder<CXCursor>();
			cursor.VisitChildren((child, _) =>
			{
				if (child.kind == kind)
				{
					childrenBuilder.Add(child);
				}
			});
			return childrenBuilder.ToImmutable();
		}

		public static bool IsInSystem(this CXCursor cursor)
		{
			return cursor.Location.IsInSystemHeader;
		}
	}
}
