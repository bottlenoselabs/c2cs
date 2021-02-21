// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS
{
	public class ClangCodeParser
	{
		private readonly CXIndex _index;

		public ClangCodeParser()
		{
			_index = CXIndex.Create();
		}

		public bool TryParseFile(string filePath, ImmutableArray<string> commandLineArgs, out CXTranslationUnit translationUnit)
		{
			// https://clang.llvm.org/doxygen/group__CINDEX__TRANSLATION__UNIT.html#ga4c8b0a3c559d14f80f78aba8c185e711
			// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
			const CXTranslationUnit_Flags flags = CXTranslationUnit_Flags.CXTranslationUnit_None |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies;
			// ReSharper restore BitwiseOperatorOnEnumWithoutFlags

			var errorCode = CXTranslationUnit.TryParse(
				_index,
				filePath,
				commandLineArgs.AsSpan(),
				Array.Empty<CXUnsavedFile>(),
				flags,
				out translationUnit);

			if (errorCode == CXErrorCode.CXError_Success)
            {
                return translationUnit != null;
            }

			translationUnit = null!;
			return false;
		}
	}
}
