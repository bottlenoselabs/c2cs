// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS
{
	internal static class ClangExtensions
	{
		public delegate void VisitChildAction(CXCursor child);

		public static unsafe void VisitChildren(this CXCursor cursor, VisitChildAction visitAction)
		{
			var clientData = new CXClientData(IntPtr.Zero);

			cursor.VisitChildren(Visitor, clientData);

			CXChildVisitResult Visitor(CXCursor childCursor, CXCursor childParent, void* data)
			{
				visitAction(childCursor);
				return CXChildVisitResult.CXChildVisit_Continue;
			}
		}

		public static unsafe void VisitChildren(this CXType type, VisitChildAction visitAction)
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
			cursor.VisitChildren(child =>
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
