// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using C2CS;

public static class ClangTranslationUnitParser
{
    public static clang.CXTranslationUnit Parse(
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

        var defaultDisplayOptions = clang.clang_defaultDiagnosticDisplayOptions();
        Console.Error.WriteLine("Clang diagnostics:");
        var hasErrors = false;
        foreach (var diagnostic in diagnostics)
        {
            Console.Error.Write("\t");
            var clangString = clang.clang_formatDiagnostic(diagnostic, defaultDisplayOptions);
            var diagnosticStringC = clang.clang_getCString(clangString);
            var diagnosticString = clang.CStrings.String(diagnosticStringC);
            Console.Error.WriteLine(diagnosticString);

            var severity = clang.clang_getDiagnosticSeverity(diagnostic);
            if (severity == clang.CXDiagnosticSeverity.CXDiagnostic_Error ||
                severity == clang.CXDiagnosticSeverity.CXDiagnostic_Fatal)
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
        out clang.CXTranslationUnit translationUnit)
    {
        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        const uint options = 0x00001000 | // CXTranslationUnit_IncludeAttributedTypes
                             0x00004000 | // CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles
                             0x00000040 | // CXTranslationUnit_SkipFunctionBodies
                             0x1 | // CXTranslationUnit_DetailedPreprocessingRecord
                             0x0;

        var index = clang.clang_createIndex(0, 0);
        var cSourceFilePath = clang.CStrings.CString(filePath);
        var cCommandLineArgs = clang.CStrings.CStringArray(commandLineArgs.AsSpan());

        clang.CXErrorCode errorCode;
        fixed (clang.CXTranslationUnit* translationUnitPointer = &translationUnit)
        {
            errorCode = clang.clang_parseTranslationUnit2(
                index,
                cSourceFilePath,
                cCommandLineArgs,
                commandLineArgs.Length,
                (clang.CXUnsavedFile*)IntPtr.Zero,
                0,
                options,
                translationUnitPointer);
        }

        var result = errorCode == clang.CXErrorCode.CXError_Success;
        return result;
    }

    private static ImmutableArray<clang.CXDiagnostic> GetCompilationDiagnostics(clang.CXTranslationUnit translationUnit)
    {
        var diagnosticsCount = (int)clang.clang_getNumDiagnostics(translationUnit);
        var builder = ImmutableArray.CreateBuilder<clang.CXDiagnostic>(diagnosticsCount);

        for (uint i = 0; i < diagnosticsCount; ++i)
        {
            var diagnostic = clang.clang_getDiagnostic(translationUnit, i);
            builder.Add(diagnostic);
        }

        return builder.ToImmutable();
    }
}
