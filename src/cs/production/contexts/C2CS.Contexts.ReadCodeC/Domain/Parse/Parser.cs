// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Domain.Parse.Diagnostics;
using C2CS.Foundation.Diagnostics;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse;

public sealed partial class Parser
{
    private readonly ILogger<Parser> _logger;
    private readonly ClangArgumentsBuilder _clangArgumentsBuilder;

    public Parser(
        ILogger<Parser> logger,
        ClangArgumentsBuilder clangArgumentsBuilder)
    {
        _logger = logger;
        _clangArgumentsBuilder = clangArgumentsBuilder;
    }

    public CXTranslationUnit TranslationUnit(
        string filePath,
        DiagnosticsSink diagnosticsSink,
        TargetPlatform targetPlatform,
        ParseOptions options)
    {
        var arguments = _clangArgumentsBuilder.Build(
            diagnosticsSink,
            targetPlatform,
            options,
            false);

        var argumentsString = string.Join(" ", arguments);

        if (!TryParseTranslationUnit(filePath, arguments, out var translationUnit))
        {
            var up = new ClangException();
            LogFailureInvalidArguments(filePath, argumentsString, up);
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
                var diagnosticString = clangString.String();
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
            LogSuccessDiagnostics(filePath, argumentsString, clangDiagnostics.Length);
        }
        else
        {
            LogFailureDiagnostics(filePath, argumentsString, clangDiagnostics.Length);
        }

        return translationUnit;
    }

    private static unsafe bool TryParseTranslationUnit(
        string filePath,
        ImmutableArray<string> commandLineArgs,
        out CXTranslationUnit translationUnit,
        bool skipFunctionBodies = true)
    {
        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        uint options = 0x00001000 | // CXTranslationUnit_IncludeAttributedTypes
                             0x00004000 | // CXTranslationUnit_IgnoreNonErrorsFromIncludedFiles
                             0x1 | // CXTranslationUnit_DetailedPreprocessingRecord
                             0x0;

        if (skipFunctionBodies)
        {
            options |= 0x00000040; // CXTranslationUnit_SkipFunctionBodies
        }

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

    public ImmutableArray<CMacroObject> MacroObjects(
        CXTranslationUnit translationUnit,
        DiagnosticsSink diagnosticsSink,
        TargetPlatform targetPlatform,
        ParseOptions options)
    {
        if (!options.IsEnabledMacroObjects)
        {
            return ImmutableArray<CMacroObject>.Empty;
        }

        var arguments = _clangArgumentsBuilder.Build(
            diagnosticsSink,
            targetPlatform,
            options,
            true);

        var linkedPaths = _clangArgumentsBuilder.GetLinkedPaths();
        var cursors = MacroObjectCursors(translationUnit, options);
        var macroObjectCandidates = MacroObjectCandidates(options, cursors, linkedPaths);
        var filePath = WriteMacroObjectsFile(macroObjectCandidates);
        var macroObjects = Macros(arguments, filePath);

        File.Delete(filePath);
        var result = macroObjects.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    private ImmutableArray<CMacroObject> Macros(
        ImmutableArray<string> arguments,
        string filePath)
    {
        var argumentsString = string.Join(" ", arguments);
        var parsedTranslationUnit = TryParseTranslationUnit(filePath, arguments, out var translationUnit, false);
        if (!parsedTranslationUnit)
        {
            var up = new ClangException();
            LogFailureInvalidArguments(filePath, argumentsString, up);
            throw up;
        }

        using var streamReader = new StreamReader(filePath);
        var result = GetMacroObjects(translationUnit, streamReader);
        clang_disposeTranslationUnit(translationUnit);
        return result;
    }

    private ImmutableArray<CMacroObject> GetMacroObjects(CXTranslationUnit translationUnit, StreamReader reader)
    {
        var builder = ImmutableArray.CreateBuilder<CMacroObject>();

        var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);
        var functionCursor = translationUnitCursor
            .GetDescendents(static (cursor, _) => IsFunctionWithMacroVariables(cursor)).FirstOrDefault();
        var compoundStatement = functionCursor.GetDescendents(static (cursor, _) => IsCompoundStatement(cursor)).FirstOrDefault();
        var declarationStatements = compoundStatement.GetDescendents(static (cursor, _) => IsDeclarationStatement(cursor));
        var readerLineNumber = 0;

        foreach (var declarationStatement in declarationStatements)
        {
            var variable = declarationStatement.GetDescendents(static (cursor, _) => IsVariable(cursor)).FirstOrDefault();
            var variableName = variable.Name();
            var macroName = variableName.Replace("variable_", string.Empty, StringComparison.InvariantCultureIgnoreCase);
            var cursor = variable.GetDescendents().FirstOrDefault();
            var macro = Macro(macroName, cursor, reader, ref readerLineNumber);
            if (macro == null)
            {
                continue;
            }

            builder.Add(macro);
        }

        return builder.ToImmutable();
    }

    private static CMacroObject? Macro(string name, CXCursor cursor, StreamReader reader, ref int readerLineNumber)
    {
        var macroValue = EvaluateMacroValue(cursor);
        if (macroValue == null)
        {
            return null;
        }

        var location = MacroLocation(cursor, reader, ref readerLineNumber);

        var type = clang_getCursorType(cursor);
        var kind = MacroTypeKind(type);
        var typeName = type.Name();
        var sizeOf = (int)clang_Type_getSizeOf(type);
        var typeInfo = new CTypeInfo
        {
            Name = typeName,
            Kind = kind,
            SizeOf = sizeOf
        };

        var macro = new CMacroObject
        {
            Name = name,
            Value = macroValue,
            Type = typeInfo,
            Location = location
        };

        return macro;
    }

    private static CKind MacroTypeKind(CXType type)
    {
        if (type.IsPrimitive())
        {
            return CKind.Primitive;
        }

        return type.kind switch
        {
            CXTypeKind.CXType_Typedef => CKind.TypeAlias,
            CXTypeKind.CXType_Enum => CKind.Enum,
            CXTypeKind.CXType_Pointer => CKind.Pointer,
            CXTypeKind.CXType_ConstantArray => CKind.Array,
            _ => CKind.Unknown
        };
    }

    private static CLocation MacroLocation(CXCursor cursor, StreamReader reader, ref int readerLineNumber)
    {
        var location = cursor.Location(null, null, null);
        var locationCommentLineNumber = location.LineNumber - 1;

        if (readerLineNumber > locationCommentLineNumber)
        {
            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            readerLineNumber = 0;
        }

        var line = string.Empty;
        while (readerLineNumber != locationCommentLineNumber)
        {
            line = reader.ReadLine() ?? string.Empty;
            readerLineNumber++;
        }

        var locationString = line.TrimStart('/');
        var locationStringParse = locationString.Split(':');
        var locationFilePath = locationStringParse[0].Trim();
        if (!int.TryParse(locationStringParse[1], out var locationLineNumber))
        {
            // TODO: Fix
            Console.WriteLine();
        }

        if (!int.TryParse(locationStringParse[2], out var locationLineColumn))
        {
            // TODO: Fix
            Console.WriteLine();
        }

        var actualLocation = new CLocation
        {
            FileName = Path.GetFileName(locationFilePath),
            FilePath = locationFilePath,
            LineNumber = locationLineNumber,
            LineColumn = locationLineColumn
        };
        return actualLocation;
    }

    private static string? EvaluateMacroValue(CXCursor cursor)
    {
        var evaluateResult = clang_Cursor_Evaluate(cursor);
        var kind = clang_EvalResult_getKind(evaluateResult);
        string value;

        switch (kind)
        {
            case CXEvalResultKind.CXEval_UnExposed:
                return null;
            case CXEvalResultKind.CXEval_Int:
            {
                var integerValue = clang_EvalResult_getAsInt(evaluateResult);
                value = integerValue.ToString(CultureInfo.InvariantCulture);
                break;
            }

            case CXEvalResultKind.CXEval_StrLiteral or CXEvalResultKind.CXEval_CFStr:
            {
                var stringValueC = clang_EvalResult_getAsStr(evaluateResult);
                var stringValue = Marshal.PtrToStringAnsi(stringValueC)!;
                value = "\"" + stringValue + "\"";
                break;
            }

            case CXEvalResultKind.CXEval_Float:
            {
                var floatValue = clang_EvalResult_getAsDouble(evaluateResult);
                value = floatValue.ToString(CultureInfo.InvariantCulture);
                break;
            }

            default:
                throw new NotImplementedException();
        }

        clang_EvalResult_dispose(evaluateResult);
        return value;
    }

    private static bool IsVariable(CXCursor cxCursor)
    {
        return cxCursor.kind == CXCursorKind.CXCursor_VarDecl;
    }

    private static bool IsDeclarationStatement(CXCursor cursor)
    {
        return cursor.kind == CXCursorKind.CXCursor_DeclStmt;
    }

    private static bool IsCompoundStatement(CXCursor cursor)
    {
        return cursor.kind == CXCursorKind.CXCursor_CompoundStmt;
    }

    private static bool IsFunctionWithMacroVariables(CXCursor cursor)
    {
        var sourceLocation = clang_getCursorLocation(cursor);
        var isFromMainFile = clang_Location_isFromMainFile(sourceLocation) > 0;
        if (!isFromMainFile)
        {
            return false;
        }

        return cursor.kind == CXCursorKind.CXCursor_FunctionDecl;
    }

    private static string WriteMacroObjectsFile(ImmutableArray<MacroObjectCandidate> macroObjectCandidates)
    {
        var tempFilePath = Path.GetTempFileName();
        using var fileStream = File.OpenWrite(tempFilePath);
        using var writer = new StreamWriter(fileStream);

        var includeHeaderFilePaths = new HashSet<string>();

        foreach (var macroObject in macroObjectCandidates)
        {
            var includeHeaderFilePath = macroObject.Location.FullFilePath;
            if (includeHeaderFilePaths.Contains(includeHeaderFilePath))
            {
                continue;
            }

            includeHeaderFilePaths.Add(includeHeaderFilePath);
            writer.Write("#include \"");
            writer.Write(includeHeaderFilePath);
            writer.WriteLine("\"");
        }

        var codeStart = @"
int main(void)
{";
        writer.WriteLine(codeStart);

        foreach (var macroObjectCandidate in macroObjectCandidates)
        {
            if (macroObjectCandidate.Tokens.IsDefaultOrEmpty)
            {
                continue;
            }

            writer.WriteLine("// " + macroObjectCandidate.Location);
            writer.Write("\tauto variable_");
            writer.Write(macroObjectCandidate.Name);
            writer.Write(" = ");
            foreach (var token in macroObjectCandidate.Tokens)
            {
                writer.Write(token);
            }

            writer.WriteLine(";");
        }

        const string codeEnd = @"
}";
        writer.WriteLine(codeEnd);
        writer.Flush();
        writer.Close();

        return tempFilePath;
    }

    private static ImmutableArray<MacroObjectCandidate> MacroObjectCandidates(
        ParseOptions options, ImmutableArray<CXCursor> cursors, ImmutableDictionary<string, string> linkedPaths)
    {
        var macroObjectsBuilder = ImmutableArray.CreateBuilder<MacroObjectCandidate>();
        foreach (var cursor in cursors)
        {
            var macroObjectCandidate = MacroObjectCandidate(cursor, options.MacroObjectNamesAllowed, linkedPaths, options.UserIncludeDirectories);
            if (macroObjectCandidate == null)
            {
                continue;
            }

            macroObjectsBuilder.Add(macroObjectCandidate);
        }

        return macroObjectsBuilder.ToImmutable();
    }

    private static MacroObjectCandidate? MacroObjectCandidate(
        CXCursor cursor,
        ImmutableArray<string> macroObjectNamesAllowed,
        ImmutableDictionary<string, string> linkedPaths,
        ImmutableArray<string>? userIncludeDirectories)
    {
        var name = cursor.Name();

        var isAllowed = macroObjectNamesAllowed.IsDefaultOrEmpty || macroObjectNamesAllowed.Contains(name);
        if (!isAllowed)
        {
            return null;
        }

        var location = cursor.Location(null, linkedPaths, userIncludeDirectories);

        // clang doesn't have a thing where we can easily get a value of a macro
        // we need to:
        //  1. get the text range of the cursor
        //  2. get the tokens over said text range
        //  3. go through the tokens to parse the value
        // this means we get to do token parsing ourselves, yay!
        // NOTE: The first token will always be the name of the macro
        var translationUnit = clang_Cursor_getTranslationUnit(cursor);
        string[] tokens;
        unsafe
        {
            var range = clang_getCursorExtent(cursor);
            var tokensC = (CXToken*)0;
            uint tokensCount = 0;

            clang_tokenize(translationUnit, range, &tokensC, &tokensCount);

            var isFlag = tokensCount is 0 or 1;
            if (isFlag)
            {
                clang_disposeTokens(translationUnit, tokensC, tokensCount);
                return null;
            }

            tokens = new string[tokensCount - 1];
            for (var i = 1; i < (int)tokensCount; i++)
            {
                var tokenString = clang_getTokenSpelling(translationUnit, tokensC[i]).String();

                // CLANG BUG?: https://github.com/FNA-XNA/FAudio/blob/b84599a5e6d7811b02329709a166a337de158c5e/include/FAPOBase.h#L90
                if (tokenString.StartsWith('\\'))
                {
                    tokenString = tokenString.TrimStart('\\');
                }

                if (tokenString.StartsWith("__", StringComparison.InvariantCulture) && tokenString.EndsWith("__", StringComparison.InvariantCulture))
                {
                    clang_disposeTokens(translationUnit, tokensC, tokensCount);
                    return null;
                }

                tokens[i - 1] = tokenString.Trim();
            }

            clang_disposeTokens(translationUnit, tokensC, tokensCount);
        }

        var result = new MacroObjectCandidate
        {
            Name = name,
            Tokens = tokens.ToImmutableArray(),
            Location = location
        };

        return result;
    }

    private static ImmutableArray<CXCursor> MacroObjectCursors(CXTranslationUnit translationUnit, ParseOptions options)
    {
        var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);
        var cursors = translationUnitCursor.GetDescendents(
            (child, _) => IsMacroOfInterest(child, options));
        return cursors;
    }

    private static bool IsMacroOfInterest(CXCursor cursor, ParseOptions options)
    {
        var kind = clang_getCursorKind(cursor);
        if (kind != CXCursorKind.CXCursor_MacroDefinition)
        {
            return false;
        }

        var isMacroBuiltIn = clang_Cursor_isMacroBuiltin(cursor) > 0;
        if (isMacroBuiltIn)
        {
            return false;
        }

        if (!options.IsEnabledSystemDeclarations)
        {
            var locationSource = clang_getCursorLocation(cursor);
            var isMacroSystem = clang_Location_isInSystemHeader(locationSource) > 0;
            if (isMacroSystem)
            {
                return false;
            }
        }

        var isMacroFunction = clang_Cursor_isMacroFunctionLike(cursor) > 0;
        if (isMacroFunction)
        {
            return false;
        }

        var name = cursor.Name();
        if (name.StartsWith("_", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Assume that macro ending with "API_DECL" are not interesting for bindgen
        if (name.EndsWith("API_DECL", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Assume that macros starting with names of the C helper macros are not interesting for bindgen
        if (name.StartsWith("PINVOKE_TARGET_", StringComparison.InvariantCulture))
        {
            return false;
        }

        return true;
    }

    public ImmutableDictionary<string, string> GetLinkedPaths()
    {
        return _clangArgumentsBuilder.GetLinkedPaths();
    }

    public void Cleanup()
    {
        _clangArgumentsBuilder.Cleanup();
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

    [LoggerMessage(0, LogLevel.Error, "- Failed. The arguments are incorrect or invalid. Path: {FilePath} ; Clang arguments: {Arguments}")]
    private partial void LogFailureInvalidArguments(string filePath, string arguments, Exception exception);

    [LoggerMessage(1, LogLevel.Debug, "- Success. Path: {FilePath} ; Clang arguments: {Arguments} ; Diagnostics: {DiagnosticsCount}")]
    private partial void LogSuccessDiagnostics(string filePath, string arguments, int diagnosticsCount);

    [LoggerMessage(2, LogLevel.Error, "- Failed. One or more Clang diagnostics are reported when parsing that are an error or fatal. Path: {FilePath} ; Clang arguments: {Arguments} ; Diagnostics: {DiagnosticsCount}")]
    public partial void LogFailureDiagnostics(string filePath, string arguments, int diagnosticsCount);
}
