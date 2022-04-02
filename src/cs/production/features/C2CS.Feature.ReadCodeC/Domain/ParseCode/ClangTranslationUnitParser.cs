// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Domain.ParseCode.Diagnostics;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain.ParseCode;

public sealed class ClangTranslationUnitParser
{
    private readonly ILogger _logger;

    public ClangTranslationUnitParser(ILogger logger)
    {
        _logger = logger;
    }

    public CXTranslationUnit Parse(
        DiagnosticsSink diagnosticsSink,
        string filePath,
        ImmutableArray<string> arguments)
    {
        var argumentsString = string.Join(" ", arguments);

        if (!TryParseTranslationUnit(filePath, arguments, out var translationUnit))
        {
            var up = new ClangException();
            _logger.ParseTranslationUnitFailedArguments(filePath, argumentsString, up);
            throw up;
        }

        var clangDiagnostics = GetClangDiagnostics(translationUnit);
        var isSuccess = true;
        if (!clangDiagnostics.IsDefaultOrEmpty)
        {
            var defaultDisplayOptions = clang_defaultDiagnosticDisplayOptions();
            foreach (var clangDiagnostic in clangDiagnostics)
            {
                var clangString = clang_formatDiagnostic(clangDiagnostic, defaultDisplayOptions);
                var diagnosticStringC = clang_getCString(clangString);
                var diagnosticString = Runtime.CStrings.String(diagnosticStringC);
                var severity = clang_getDiagnosticSeverity(clangDiagnostic);

                var diagnosticSeverity = severity switch
                {
                    CXDiagnosticSeverity.CXDiagnostic_Fatal => DiagnosticSeverity.Panic,
                    CXDiagnosticSeverity.CXDiagnostic_Error => DiagnosticSeverity.Error,
                    CXDiagnosticSeverity.CXDiagnostic_Warning => DiagnosticSeverity.Warning,
                    CXDiagnosticSeverity.CXDiagnostic_Note => DiagnosticSeverity.Information,
                    CXDiagnosticSeverity.CXDiagnostic_Ignored => DiagnosticSeverity.Information,
                    _ => DiagnosticSeverity.Error
                };

                if (severity == CXDiagnosticSeverity.CXDiagnostic_Error ||
                    severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
                {
                    isSuccess = false;
                }

                var diagnostic = new ClangTranslationUnitParserDiagnostic(diagnosticSeverity, diagnosticString);
                diagnosticsSink.Add(diagnostic);
            }
        }

        if (isSuccess)
        {
            _logger.ParseTranslationUnitSuccess(filePath, argumentsString, clangDiagnostics.Length);
        }
        else
        {
            _logger.ParseTranslationUnitFailedDiagnostics(filePath, argumentsString, clangDiagnostics.Length);
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

    private static ImmutableArray<CXDiagnostic> GetClangDiagnostics(CXTranslationUnit translationUnit)
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
