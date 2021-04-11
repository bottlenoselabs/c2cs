// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using ClangSharp.Interop;

namespace C2CS.Languages.C
{
    public static class ClangParser
    {
        public static CXTranslationUnit ParseTranslationUnit(
            string headerFilePath,
            ImmutableArray<string> clangArgs)
        {
            var clangArgsConcat = string.Join(" ", clangArgs);
            Console.WriteLine($"libclang arguments: {clangArgsConcat}");

            if (!TryParseTranslationUnit(headerFilePath, clangArgs, out var translationUnit))
            {
                throw new ClangParserException("libclang failed.");
            }

            var diagnostics = GetCompilationDiagnostics(translationUnit);
            if (diagnostics.IsDefaultOrEmpty)
            {
                return translationUnit;
            }

            Console.Error.WriteLine("Clang diagnostics:");
            var hasErrors = false;
            foreach (var diagnostic in diagnostics)
            {
                Console.Error.Write("\t");
                Console.Error.WriteLine(diagnostic.Format(CXDiagnostic.DefaultDisplayOptions).ToString());

                var severity = diagnostic.Severity;
                if (severity == CXDiagnosticSeverity.CXDiagnostic_Error ||
                    severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
                {
                    hasErrors = true;
                }
            }

            if (hasErrors)
            {
                throw new ClangParserException("Clang parsing errors.");
            }

            return translationUnit;
        }

        private static bool TryParseTranslationUnit(
            string filePath,
            ImmutableArray<string> commandLineArgs,
            out CXTranslationUnit translationUnit)
        {
            // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
            var flags = CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
                        CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
                        CXTranslationUnit_Flags.CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles |
                        CXTranslationUnit_Flags.CXTranslationUnit_SkipFunctionBodies;

            var index = CXIndex.Create();
            var errorCode = CXTranslationUnit.TryParse(
                index,
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

        private static ImmutableArray<CXDiagnostic> GetCompilationDiagnostics(CXTranslationUnit translationUnit)
        {
            var diagnosticsCount = (int)translationUnit.NumDiagnostics;
            var builder = ImmutableArray.CreateBuilder<CXDiagnostic>(diagnosticsCount);

            for (uint i = 0; i < diagnosticsCount; ++i)
            {
                var diagnostic = translationUnit.GetDiagnostic(i);
                builder.Add(diagnostic);
            }

            return builder.ToImmutable();
        }

        public class ClangParserException : Exception
        {
            public ClangParserException(string message)
                : base(message)
            {
            }
        }
    }
}
