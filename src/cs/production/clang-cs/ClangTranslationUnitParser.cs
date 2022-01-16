// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using static bottlenoselabs.clang;

public static class ClangTranslationUnitParser
{
    public static CXTranslationUnit Parse(
        string headerFilePath,
        ImmutableArray<string> clangArgs)
    {
        var clangArgsConcat = string.Join(" ", clangArgs);
        Console.WriteLine($"libclang: Parsing '{headerFilePath}' with the following arguments...");
        Console.WriteLine($"\t{clangArgsConcat}");

        if (!TryParseTranslationUnit(headerFilePath, clangArgs, out var translationUnit))
        {
            throw new ClangException("libclang failed.");
        }

        var diagnostics = GetCompilationDiagnostics(translationUnit);
        if (diagnostics.IsDefaultOrEmpty)
        {
            return translationUnit;
        }

        var defaultDisplayOptions = clang_defaultDiagnosticDisplayOptions();
        Console.Error.WriteLine("Clang diagnostics:");
        var hasErrors = false;
        foreach (var diagnostic in diagnostics)
        {
            Console.Error.Write("\t");
            var clangString = clang_formatDiagnostic(diagnostic, defaultDisplayOptions);
            var diagnosticStringC = clang_getCString(clangString);
            var diagnosticString = Runtime.CStrings.String(diagnosticStringC);
            Console.Error.WriteLine(diagnosticString);

            var severity = clang_getDiagnosticSeverity(diagnostic);
            if (severity == CXDiagnosticSeverity.CXDiagnostic_Error ||
                severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
            {
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            throw new ClangException("Clang parsing errors.");
        }

        return translationUnit;
    }

    private static unsafe bool TryParseTranslationUnit(
        string filePath,
        ImmutableArray<string> commandLineArgs,
        out CXTranslationUnit translationUnit)
    {
        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        const uint options = 0x00001000 | // CXTranslationUnit_IncludeAttributedTypes
                             0x00004000 | // CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles
                             0x00000040 | // CXTranslationUnit_SkipFunctionBodies
                             0x1 | // CXTranslationUnit_DetailedPreprocessingRecord
                             0x0;

        var index = clang_createIndex(0, 0);
        var cSourceFilePath = Runtime.CStrings.CString(filePath);
        var cCommandLineArgs = Runtime.CStrings.CStringArray(commandLineArgs.AsSpan());

        CXErrorCode errorCode;
        fixed (CXTranslationUnit* translationUnitPointer = &translationUnit)
        {
            errorCode = clang_parseTranslationUnit2(
                index,
                cSourceFilePath,
                cCommandLineArgs,
                commandLineArgs.Length,
                (CXUnsavedFile*)IntPtr.Zero,
                0,
                options,
                translationUnitPointer);
        }

        var result = errorCode == CXErrorCode.CXError_Success;
        return result;
    }

    private static ImmutableArray<CXDiagnostic> GetCompilationDiagnostics(CXTranslationUnit translationUnit)
    {
        var diagnosticsCount = (int)clang_getNumDiagnostics(translationUnit);
        var builder = ImmutableArray.CreateBuilder<CXDiagnostic>(diagnosticsCount);

        for (uint i = 0; i < diagnosticsCount; ++i)
        {
            var diagnostic = clang_getDiagnostic(translationUnit, i);
            builder.Add(diagnostic);
        }

        return builder.ToImmutable();
    }
}
