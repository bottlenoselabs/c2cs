// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using ClangSharp;
using ClangSharp.Interop;

namespace C2CS
{
	public class CodeCParser
	{
		private readonly CXIndex _index;

		public CodeCParser()
		{
			_index = CXIndex.Create();
		}

		public bool TryParseFile(string filePath, string[] commandLineArgs, out TranslationUnit translationUnit)
		{
			// https://clang.llvm.org/doxygen/group__CINDEX__TRANSLATION__UNIT.html#ga4c8b0a3c559d14f80f78aba8c185e711
			// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
			const CXTranslationUnit_Flags flags = CXTranslationUnit_Flags.CXTranslationUnit_None |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
			                                      CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes;
			// ReSharper restore BitwiseOperatorOnEnumWithoutFlags

			var errorCode = CXTranslationUnit.TryParse(
				_index,
				filePath,
				commandLineArgs,
				Array.Empty<CXUnsavedFile>(),
				flags,
				out var handle);

			if (errorCode != CXErrorCode.CXError_Success)
			{
				translationUnit = null!;
				return false;
			}

			translationUnit = TranslationUnit.GetOrCreate(handle);
			return translationUnit != null;
		}
	}
}
