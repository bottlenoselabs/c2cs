// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using ClangSharp;

namespace C2CS
{
	internal static class CursorExtensions
	{
		public static string GetFilePath(this Cursor cursor)
		{
			var location = cursor.Location;
			location.GetFileLocation(out var file, out _, out _, out _);
			return file.TryGetRealPathName().CString;
		}
	}
}
