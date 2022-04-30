// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Feature.ReadCodeC.Domain.ExploreCode.Diagnostics;
using C2CS.Foundation.UseCases.Exceptions;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

public sealed class TranslationUnitExplorer
{
    private readonly ILogger _logger;

    public TranslationUnitExplorer(ILogger logger)
    {
        _logger = logger;
    }

    public CAbstractSyntaxTree AbstractSyntaxTree(ExplorerContext context, CXTranslationUnit translationUnit)
    {
        CAbstractSyntaxTree result;

        try
        {
            VisitTranslationUnit(context, translationUnit);
            Explore(context);
            result = Result(context, translationUnit);
            _logger.ExploreCodeSuccess();
        }
        catch (Exception e)
        {
            _logger.ExploreCodeFailed(e);
            throw;
        }

        return result;
    }

    private CAbstractSyntaxTree Result(
        ExplorerContext context,
        CXTranslationUnit translationUnit)
    {
        var cursor = clang_getTranslationUnitCursor(translationUnit);
        var location = Location(context, cursor, default);

        var functions = context.Functions.ToImmutableDictionary();
        var functionPointers = context.FunctionPointers.ToImmutableDictionary();
        var records = context.Records.ToImmutableDictionary();
        var enums = context.Enums.ToImmutableDictionary();
        var opaqueTypes = context.OpaqueDataTypes.ToImmutableDictionary();
        var typedefs = context.Typedefs.ToImmutableDictionary();
        var variables = context.Variables.ToImmutableDictionary();
        var constants = context.MacroObjects.ToImmutableDictionary();
        var types = context.Types.ToImmutableDictionary();

        var result = new CAbstractSyntaxTree
        {
            FileName = location.FileName,
            Platform = context.TargetPlatform,
            Functions = functions,
            FunctionPointers = functionPointers,
            Records = records,
            Enums = enums,
            OpaqueTypes = opaqueTypes,
            Typedefs = typedefs,
            Variables = variables,
            Constants = constants,
            Types = types
        };

        return result;
    }

    private void VisitTranslationUnit(ExplorerContext context, CXTranslationUnit translationUnit)
    {
        var cursor = clang_getTranslationUnitCursor(translationUnit);

        var type = clang_getCursorType(cursor);
        var location = CLocation.Null;
        var filePath = cursor.Name(); // name of translation unit cursor is the file path

        _logger.ExploreCodeTranslationUnit(filePath);
        EnqueueNode(
            context,
            CKind.TranslationUnit,
            location,
            null,
            cursor,
            type,
            string.Empty,
            string.Empty);
    }

    private void Explore(ExplorerContext context)
    {
        while (context.FrontierGeneral.Count > 0)
        {
            var node = context.FrontierGeneral.PopFront()!;
            ExploreNode(context, node);
        }

        while (context.FrontierMacros.Count > 0)
        {
            var node = context.FrontierMacros.PopFront()!;
            ExploreNode(context, node);
        }
    }

    private void ExploreNode(ExplorerContext context, ExplorerNode node)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.Kind)
        {
            case CKind.TranslationUnit:
                ExploreTranslationUnit(context, node);
                break;
            case CKind.Variable:
                ExploreVariable(context, node.CursorName!, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Function:
                ExploreFunction(context, node.CursorName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Typedef:
                ExploreTypedef(context, node, node.Parent!);
                break;
            case CKind.OpaqueType:
                ExploreOpaqueType(context, node.TypeName!, node.Location);
                break;
            case CKind.Enum:
                ExploreEnum(context, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Record:
                ExploreRecord(context, node, node.Parent!);
                break;
            case CKind.FunctionPointer:
                ExploreFunctionPointer(
                    context, node.CursorName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Array:
                ExploreArray(context, node);
                break;
            case CKind.Pointer:
                ExplorePointer(context, node);
                break;
            case CKind.Primitive:
                break;
            case CKind.MacroDefinition:
                ExploreMacro(context, node);
                break;
            default:
                var up = new UseCaseException($"Unexpected explorer node '{node.Kind}'.");
                throw up;
        }
    }

    private bool IsBlocked(
        ExplorerContext context,
        CXType type,
        CLocation location,
        CKind kind,
        string cursorName,
        string typeName)
    {
        switch (kind)
        {
            case CKind.TranslationUnit:
            case CKind.Primitive:
                return false;
            case CKind.Array:
            {
                var elementTypeCandidate = clang_getElementType(type);
                var (elementKind, elementType) = TypeKind(context, elementTypeCandidate);
                var elementCursor = clang_getTypeDeclaration(elementType);
                var elementLocation = Location(context, elementCursor, elementType);
                var elementTypeName = elementType.Name();
                return IsBlocked(context, elementType, elementLocation, elementKind, string.Empty, elementTypeName);
            }

            case CKind.Pointer:
            {
                var pointerTypeCandidate = clang_getPointeeType(type);
                var (pointeeKind, pointeeType) = TypeKind(context, pointerTypeCandidate);
                var pointerCursor = clang_getTypeDeclaration(pointeeType);
                var pointeeLocation = Location(context, pointerCursor, pointeeType);
                var pointeeTypeName = pointeeType.Name();
                return IsBlocked(context, pointeeType, pointeeLocation, pointeeKind, string.Empty, pointeeTypeName);
            }
        }

        if (!context.Options.IsEnabledAllowNamesWithPrefixedUnderscore)
        {
            if (IsBlockedNamed(cursorName, typeName))
            {
                return true;
            }
        }

        return IsBlockedLocation(context, location);
    }

    private static bool IsBlockedNamed(string cursorName, string typeName)
    {
        if (cursorName.StartsWith("_", StringComparison.InvariantCulture))
        {
            return true;
        }

        if (typeName.StartsWith("_", StringComparison.InvariantCulture))
        {
            return true;
        }

        return false;
    }

    private static bool IsBlockedLocation(ExplorerContext context, CLocation location)
    {
        if (string.IsNullOrEmpty(location.FileName))
        {
            var up = new UseCaseException(
                "Unexpected null or empty file path.");
            throw up;
        }

        foreach (var includeDirectory in context.UserIncludeDirectories)
        {
            if (!location.FileName.Contains(includeDirectory, StringComparison.InvariantCulture))
            {
                continue;
            }

            location.FileName = location.FileName
                .Replace(includeDirectory, string.Empty, StringComparison.InvariantCulture).Trim('/', '\\');
            break;
        }

        return context.Options.HeaderFilesBlocked.Contains(location.FileName);
    }

    private void ExploreTranslationUnit(
        ExplorerContext context, ExplorerNode node)
    {
        var cursors = node.Cursor.GetDescendents(
            (child, _) => IsTranslationUnitChildCursorOfInterest(context, child));

        foreach (var cursor in cursors)
        {
            VisitTranslationUnitChildCursor(context, node, cursor);
        }
    }

    private bool IsTranslationUnitChildCursorOfInterest(ExplorerContext context, CXCursor cursor)
    {
        var kind = clang_getCursorKind(cursor);
        if (kind != CXCursorKind.CXCursor_FunctionDecl &&
            kind != CXCursorKind.CXCursor_VarDecl &&
            kind != CXCursorKind.CXCursor_EnumDecl &&
            kind != CXCursorKind.CXCursor_MacroDefinition)
        {
            return false;
        }

        if (kind == CXCursorKind.CXCursor_MacroDefinition)
        {
            var isMacroBuiltIn = clang_Cursor_isMacroBuiltin(cursor) > 0;
            if (isMacroBuiltIn)
            {
                return false;
            }
        }
        else
        {
            var linkage = clang_getCursorLinkage(cursor);
            var isExternallyLinked = linkage == CXLinkageKind.CXLinkage_External;
            if (!isExternallyLinked)
            {
                return false;
            }
        }

        if (!context.Options.IsEnabledSystemDeclarations)
        {
            var cursorLocation = clang_getCursorLocation(cursor);
            var isSystemCursor = clang_Location_isInSystemHeader(cursorLocation) > 0;
            return !isSystemCursor;
        }

        return true;
    }

    private void VisitTranslationUnitChildCursor(
        ExplorerContext context, ExplorerNode parentNode, CXCursor cursor)
    {
        switch (cursor.kind)
        {
            case CXCursorKind.CXCursor_FunctionDecl:
                VisitFunction(context, parentNode, cursor);
                break;
            case CXCursorKind.CXCursor_VarDecl:
                VisitVariable(context, parentNode, cursor);
                break;
            case CXCursorKind.CXCursor_EnumDecl:
                VisitEnum(context, parentNode, cursor);
                break;
            case CXCursorKind.CXCursor_MacroDefinition:
                VisitMacro(context, parentNode, cursor);
                break;
            default:
                var up = new UseCaseException(
                    $"Expected function, variable, enum, or macro but found '{cursor.kind}'.");
                throw up;
        }
    }

    private void ExploreArray(
        ExplorerContext context, ExplorerNode node)
    {
        var elementTypeCandidate = clang_getElementType(node.Type);
        var (_, elementType) = TypeKind(context, elementTypeCandidate);
        var elementCursor = clang_getTypeDeclaration(elementType);
        var elementTypeName = Name(node.TypeName!, elementType, elementCursor);
        var elementLocation = Location(context, elementCursor, elementType);
        VisitType(context, node, elementCursor, elementType, elementTypeName, elementLocation);
    }

    private void ExplorePointer(
        ExplorerContext context, ExplorerNode node)
    {
        var pointeeTypeCandidate = clang_getPointeeType(node.Type);
        var (_, pointeeType) = TypeKind(context, pointeeTypeCandidate);
        var pointeeCursor = clang_getTypeDeclaration(pointeeType);
        var pointeeTypeName = Name(node.TypeName!, pointeeType, pointeeCursor);
        var pointeeLocation = Location(context, pointeeCursor, pointeeType);
        VisitType(context, node, pointeeCursor, pointeeType, pointeeTypeName, pointeeLocation);
    }

    private void ExploreMacro(ExplorerContext context, ExplorerNode node)
    {
        var macroName = node.CursorName!;
        if (context.MacroObjects.ContainsKey(macroName))
        {
            var diagnostic = new MacroAlreadyExistsDiagnostic(macroName);
            context.Diagnostics.Add(diagnostic);
            return;
        }

        var location = node.Location;

        // Assume that macros with a name which starts with an underscore are not supposed to be exposed in the public API
        if (macroName.StartsWith("_", StringComparison.InvariantCulture))
        {
            return;
        }

        // Assume that macro ending with "API_DECL" are not interesting for bindgen
        if (macroName.EndsWith("API_DECL", StringComparison.InvariantCulture))
        {
            return;
        }

        // clang doesn't have a thing where we can easily get a value of a macro
        // we need to:
        //  1. get the text range of the cursor
        //  2. get the tokens over said text range
        //  3. go through the tokens to parse the value
        // this means we get to do token parsing ourselves, yay!
        // NOTE: The first token will always be the name of the macro
        var translationUnit = clang_Cursor_getTranslationUnit(node.Cursor);
        string[] tokens;
        unsafe
        {
            var range = clang_getCursorExtent(node.Cursor);
            var tokensC = (CXToken*)0;
            uint tokensCount = 0;

            clang_tokenize(translationUnit, range, &tokensC, &tokensCount);

            var macroIsFlag = tokensCount is 0 or 1;
            if (macroIsFlag)
            {
                clang_disposeTokens(translationUnit, tokensC, tokensCount);
                return;
            }

            tokens = new string[tokensCount - 1];
            for (var i = 1; i < (int)tokensCount; i++)
            {
                var spelling = clang_getTokenSpelling(translationUnit, tokensC[i]);
                var cString = (string)clang_getCString(spelling);

                // CLANG BUG?: https://github.com/FNA-XNA/FAudio/blob/b84599a5e6d7811b02329709a166a337de158c5e/include/FAPOBase.h#L90
                if (cString.StartsWith('\\'))
                {
                    cString = cString.TrimStart('\\');
                }

                tokens[i - 1] = cString.Trim();
            }

            clang_disposeTokens(translationUnit, tokensC, tokensCount);
        }

        // Ignore macros with certain tokens
        foreach (var token in tokens)
        {
            if (context.MacroFunctionLikeNames.Contains(token))
            {
                return;
            }

            // cinttypes.h
            if (token.StartsWith("PRI", StringComparison.InvariantCulture))
            {
                return;
            }
        }

        // Ignore macros which are definitions for C extension keywords such as __attribute__ or use such keywords
        if (tokens.Any(x =>
                x.StartsWith("__", StringComparison.InvariantCulture) &&
                x.EndsWith("__", StringComparison.InvariantCulture)))
        {
            return;
        }

        // Remove redundant parenthesis
        if (tokens.Length > 2)
        {
            if (tokens[0] == "(" && tokens[^1] == ")")
            {
                var newTokens = new string[tokens.Length - 2];
                Array.Copy(tokens, 1, newTokens, 0, tokens.Length - 2);
                tokens = newTokens;
            }
        }

        // Ignore macros which are forward declarations
        if (tokens.Length == 1 && context.MacroObjects.ContainsKey(tokens[0]))
        {
            return;
        }

        if (macroName == "PINVOKE_TARGET_PLATFORM_NAME")
        {
            var actualPlatformName = tokens.Length != 1 ? string.Empty : tokens[0].Replace("\"", string.Empty, StringComparison.InvariantCulture);
            var actualPlatform = new TargetPlatform(actualPlatformName);
            var expectedPlatform = context.TargetPlatform;
            if (actualPlatform != expectedPlatform)
            {
                var diagnostic = new PlatformMismatchDiagnostic(actualPlatform, expectedPlatform);
                context.Diagnostics.Add(diagnostic);
            }

            return;
        }

        if (macroName.StartsWith("PINVOKE_TARGET", StringComparison.InvariantCulture))
        {
            return;
        }

        var macro = new CMacroDefinition
        {
            Name = macroName,
            Tokens = tokens.ToImmutableArray(),
            Location = location
        };

        context.MacroObjects.Add(macroName, macro);
        _logger.ExploreCodeMacro(macroName);
    }

    private void ExploreVariable(
        ExplorerContext context,
        string name,
        string typeName,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ExplorerNode parentNode)
    {
        if (context.Variables.ContainsKey(name))
        {
            return;
        }

        _logger.ExploreCodeVariable(name);

        VisitType(context, parentNode, cursor, type, typeName, location);

        var variable = new CVariable
        {
            Location = location,
            Name = name,
            Type = typeName
        };

        context.Variables.Add(variable.Name, variable);
    }

    private void ExploreFunction(
        ExplorerContext context,
        string name,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ExplorerNode parentNode)
    {
        var isFunctionAllowed = context.Options.FunctionNamesAllowed.IsDefaultOrEmpty ||
                                context.Options.FunctionNamesAllowed.Contains(name);
        if (!isFunctionAllowed)
        {
            return;
        }

        _logger.ExploreCodeFunction(name);

        var callingConvention = CreateFunctionCallingConvention(type);
        var resultType = clang_getCursorResultType(cursor);
        var resultCursor = clang_getTypeDeclaration(resultType);
        var resultTypeName = Name(parentNode.TypeName!, resultType, resultCursor);
        var resultLocation = Location(context, resultCursor, resultType);

        VisitType(context, parentNode, cursor, resultType, resultTypeName, resultLocation);

        var functionParameters = CreateFunctionParameters(context, cursor, parentNode);

        var function = new CFunction
        {
            Name = name,
            Location = location,
            CallingConvention = callingConvention,
            ReturnType = resultTypeName,
            Parameters = functionParameters
        };

        context.Functions.Add(function.Name, function);
    }

    private void ExploreEnum(
        ExplorerContext context,
        string name,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ExplorerNode parentNode)
    {
        if (context.Enums.ContainsKey(name))
        {
            return;
        }

        _logger.ExploreCodeEnum(name);

        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            typeCursor = cursor;
        }

        var integerType = clang_getEnumDeclIntegerType(typeCursor);
        var integerCursor = clang_getTypeDeclaration(integerType);
        var integerTypeName = Name(parentNode.TypeName!, integerType, cursor);
        var integerTypeLocation = Location(context, integerCursor, integerType);

        VisitType(context, parentNode, integerCursor, integerType, integerTypeName, integerTypeLocation);

        var enumValues = CreateEnumValues(context, typeCursor);

        var @enum = new CEnum
        {
            Name = name,
            Location = location,
            Type = name,
            IntegerType = integerTypeName,
            Values = enumValues
        };

        context.Enums.Add(name, @enum);
    }

    private void ExploreRecord(
        ExplorerContext context,
        ExplorerNode node,
        ExplorerNode parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (context.Options.OpaqueTypesNames.Contains(typeName))
        {
            ExploreOpaqueType(context, typeName, location);
            return;
        }

        _logger.ExploreCodeRecord(typeName);

        var cursor = node.Cursor;
        var type = clang_getCursorType(cursor);

        var typeCursor = clang_getTypeDeclaration(node.Type);
        var typeUnderlyingCursor = ClangUnderlyingCursor(typeCursor);
        var isUnion = typeUnderlyingCursor.kind == CXCursorKind.CXCursor_UnionDecl;
        var sizeOf = SizeOf(context, type);
        var alignOf = (int)clang_Type_getAlignOf(type);
        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        var parentName = isAnonymous ? parentNode.TypeName ?? string.Empty : string.Empty;

        if (isUnion)
        {
            var fields = CreateUnionFields(context, typeUnderlyingCursor, node);

            var union = new CRecord
            {
                RecordKind = CRecordKind.Union,
                Location = location,
                Name = typeName,
                ParentName = parentName,
                Fields = fields,
                SizeOf = sizeOf,
                AlignOf = alignOf
            };

            context.Records.Add(union.Name, union);
        }
        else
        {
            var fields = CreateStructFields(context, typeUnderlyingCursor, node);

            var @struct = new CRecord
            {
                RecordKind = CRecordKind.Struct,
                Location = location,
                Name = typeName,
                ParentName = parentName,
                Fields = fields,
                SizeOf = sizeOf,
                AlignOf = alignOf
            };

            context.Records.Add(@struct.Name, @struct);
        }
    }

    private void ExploreTypedef(
        ExplorerContext context,
        ExplorerNode node,
        ExplorerNode parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (context.Options.OpaqueTypesNames.Contains(typeName))
        {
            ExploreOpaqueType(context, typeName, location);
            return;
        }

        var typedefCursor = node.Cursor;
        if (typedefCursor.kind != CXCursorKind.CXCursor_TypedefDecl)
        {
            throw new ClangException($"Expected a typedef declaration but found: {typedefCursor.kind}");
        }

        var aliasTypeCandidate = clang_getTypedefDeclUnderlyingType(node.Cursor);
        var (aliasKind, aliasType) = TypeKind(context, aliasTypeCandidate);
        var aliasCursor = clang_getTypeDeclaration(aliasType);

        switch (aliasKind)
        {
            case CKind.Enum:
                ExploreEnum(context, typeName, aliasCursor, aliasType, location, parentNode);
                return;
            case CKind.Record:
                ExploreRecord(context, node, parentNode);
                return;
            case CKind.FunctionPointer:
                var functionPointerTypeName = aliasType.Name();
                ExploreFunctionPointer(
                    context, functionPointerTypeName, aliasCursor, aliasType, location, parentNode);
                return;
        }

        _logger.ExploreCodeTypedef(typeName);

        var aliasTypeName = Name(parentNode.TypeName!, aliasType, aliasCursor);
        var aliasLocation = Location(context, aliasCursor, aliasType);
        VisitType(context, parentNode, aliasCursor, aliasType, aliasTypeName, aliasLocation);

        var aliasSizeOf = SizeOf(context, aliasType);
        var aliasAlignOf = (int)clang_Type_getAlignOf(aliasType);

        var typedef = new CTypedef
        {
            Name = typeName,
            Location = location,
            UnderlyingTypeName = aliasTypeName,
            UnderlyingTypeKind = aliasKind,
            UnderlyingTypeSizeOf = aliasSizeOf,
            UnderlyingTypeAlignOf = aliasAlignOf
        };

        context.Typedefs.Add(typedef.Name, typedef);
    }

    private void ExploreOpaqueType(ExplorerContext context, string typeName, CLocation location)
    {
        _logger.ExploreCodeOpaqueType(typeName);

        var opaqueDataType = new COpaqueType
        {
            Name = typeName,
            Location = location
        };

        context.OpaqueDataTypes.Add(opaqueDataType.Name, opaqueDataType);
    }

    private void ExploreFunctionPointer(
        ExplorerContext context,
        string name,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ExplorerNode parentNode)
    {
        if (context.FunctionPointers.ContainsKey(name))
        {
            return;
        }

        var typeName = type.Name();
        var functionPointer = CreateFunctionPointer(context, name, typeName, cursor, parentNode, type, location);
        context.FunctionPointers.Add(name, functionPointer);

        if (!context.Types.ContainsKey(typeName))
        {
            var typeC = Type(context, typeName, cursor, type, CKind.FunctionPointer);
            context.Types.Add(typeName, typeC);
        }
    }

    private string GetFunctionPointerName(CXCursor cursor, CXType type)
    {
        return cursor.kind switch
        {
            CXCursorKind.CXCursor_TypedefDecl => cursor.Name(),
            _ => type.Name()
        };
    }

    private bool TypeNameIsValid(ExplorerContext context, string typeName)
    {
        if (context.ValidTypeNames.TryGetValue(typeName, out var isValid))
        {
            return isValid;
        }

        var isIgnored = context.SystemIgnoredTypeNames.Contains(typeName);
        isValid = !isIgnored;

        context.ValidTypeNames.Add(typeName, isValid);
        return isValid;
    }

    private bool IsNewType(ExplorerContext context, CKind kind, string typeName, CXType type, CXCursor cursor)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        if (kind == CKind.FunctionPointer)
        {
            var functionPointerName = GetFunctionPointerName(cursor, type);
            var alreadyVisited = context.VisitedFunctionPointerNames.Contains(functionPointerName);
            if (alreadyVisited)
            {
                return false;
            }

            context.VisitedFunctionPointerNames.Add(functionPointerName);
            return true;
        }
        else
        {
            var alreadyVisited = context.Types.TryGetValue(typeName, out var typeC);
            if (alreadyVisited)
            {
                if (typeC == null)
                {
                    return false;
                }

                // attempt to see if we have a definition for a previous opaque type, to which we should that info instead
                //  this can happen if one header file has a forward type, but another header file has the definition
                if (typeC.Kind != CKind.OpaqueType)
                {
                    return false;
                }

                var typeKind = TypeKind(context, type);
                if (typeKind.Kind == CKind.OpaqueType)
                {
                    return false;
                }

                typeC = Type(context, typeName, cursor, type, kind);
                context.Types[typeName] = typeC;
                return true;
            }

            typeC = Type(context, typeName, cursor, type, kind);
            context.Types.Add(typeName, typeC);

            return true;
        }
    }

    private void EnqueueNode(
        ExplorerContext context,
        CKind kind,
        CLocation location,
        ExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        string cursorName,
        string typeName)
    {
        if (kind != CKind.TranslationUnit &&
            kind != CKind.MacroDefinition &&
            type.kind == CXTypeKind.CXType_Invalid)
        {
            var up = new UseCaseException("Explorer node can't be invalid type kind.");
            throw up;
        }

        if (IsBlocked(context, type, location, kind, cursorName, typeName))
        {
            return;
        }

        var node = new ExplorerNode(
            kind,
            location,
            parent,
            cursor,
            type,
            cursorName,
            typeName);

        if (kind == CKind.MacroDefinition)
        {
            context.FrontierMacros.PushBack(node);
        }
        else
        {
            context.FrontierGeneral.PushBack(node);
        }
    }

    private static CFunctionCallingConvention CreateFunctionCallingConvention(CXType type)
    {
        var callingConvention = clang_getFunctionTypeCallingConv(type);
        var result = callingConvention switch
        {
            CXCallingConv.CXCallingConv_C => CFunctionCallingConvention.Cdecl,
            CXCallingConv.CXCallingConv_X86StdCall => CFunctionCallingConvention.StdCall,
            CXCallingConv.CXCallingConv_X86FastCall => CFunctionCallingConvention.FastCall,
            _ => throw new UseCaseException($"Unknown calling convention '{callingConvention}'.")
        };

        return result;
    }

    private ImmutableArray<CFunctionParameter> CreateFunctionParameters(
        ExplorerContext context,
        CXCursor cursor,
        ExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

        var count = clang_Cursor_getNumArguments(cursor);
        for (uint i = 0; i < count; i++)
        {
            var parameterCursor = clang_Cursor_getArgument(cursor, i);
            var functionExternParameter = FunctionParameter(context, parameterCursor, parentNode);
            builder.Add(functionExternParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionParameter FunctionParameter(
        ExplorerContext context, CXCursor cursor, ExplorerNode parentNode)
    {
        var type = clang_getCursorType(cursor);
        var name = cursor.Name();

        var typeName = Name(parentNode.TypeName!, type, cursor);
        var location = Location(context, cursor, type);

        VisitType(context, parentNode, cursor, type, typeName, location);

        var codeLocation = Location(context, cursor, type);

        return new CFunctionParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeName
        };
    }

    private CFunctionPointer CreateFunctionPointer(
        ExplorerContext context,
        string name,
        string typeName,
        CXCursor cursor,
        ExplorerNode parentNode,
        CXType type,
        CLocation location)
    {
        var functionPointerParameters = CreateFunctionPointerParameters(
            context, type, parentNode);

        var returnType = clang_getResultType(type);
        var returnTypeName = Name(parentNode.TypeName!, returnType, cursor);
        var returnTypeCursor = clang_getTypeDeclaration(returnType);
        var returnTypeLocation = Location(context, returnTypeCursor, returnType);
        VisitType(context, parentNode, cursor, returnType, returnTypeName, returnTypeLocation);

        if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var pointeeType = clang_getPointeeType(underlyingType);
            var functionProtoType = pointeeType.kind == CXTypeKind.CXType_Invalid ? underlyingType : pointeeType;
            typeName = Name(parentNode.TypeName!, functionProtoType, cursor);
        }

        var functionPointer = new CFunctionPointer
        {
            Name = name,
            Location = location,
            Type = typeName,
            ReturnType = returnTypeName,
            Parameters = functionPointerParameters
        };

        return functionPointer;
    }

    private ImmutableArray<CFunctionPointerParameter> CreateFunctionPointerParameters(
        ExplorerContext context,
        CXType type,
        ExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionPointerParameter>();

        var count = clang_getNumArgTypes(type);
        for (uint i = 0; i < count; i++)
        {
            var parameterType = clang_getArgType(type, i);
            var functionPointerParameter = CreateFunctionPointerParameter(context, parameterType, parentNode);
            builder.Add(functionPointerParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionPointerParameter CreateFunctionPointerParameter(
        ExplorerContext context,
        CXType type,
        ExplorerNode parentNode)
    {
        var cursor = clang_getTypeDeclaration(type);
        var typeSizeOf = SizeOf(context, type);
        var typeName = Name(parentNode.TypeName!, type, cursor);
        var location = Location(context, cursor, type);

        VisitType(context, parentNode, cursor, type, typeName, location);

        return new CFunctionPointerParameter
        {
            Type = typeName,
            TypeSizeOf = typeSizeOf
        };
    }

    private ImmutableArray<CRecordField> CreateUnionFields(
        ExplorerContext context,
        CXCursor unionCursor,
        ExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();

        if (unionCursor.kind != CXCursorKind.CXCursor_UnionDecl)
        {
            throw new ClangException($"Expected a union cursor but found: {unionCursor.kind}");
        }

        var fieldCursors = RecordFieldCursors(context, unionCursor);
        for (var i = 0; i < fieldCursors.Length; i++)
        {
            var fieldCursor = fieldCursors[i];
            var nextRecordField = CreateUnionField(context, parentNode, fieldCursor, i);
            builder.Add(nextRecordField);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private ImmutableArray<CRecordField> CreateStructFields(
        ExplorerContext context,
        CXCursor structCursor,
        ExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();

        var underlyingRecordCursor = ClangUnderlyingCursor(structCursor);
        var type = clang_getCursorType(underlyingRecordCursor);
        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind is CXCursorKind.CXCursor_UnionDecl or CXCursorKind.CXCursor_StructDecl)
        {
            underlyingRecordCursor = typeCursor;
        }

        if (underlyingRecordCursor.kind != CXCursorKind.CXCursor_StructDecl)
        {
            throw new ClangException($"Expected a struct cursor but found: {underlyingRecordCursor.kind}");
        }

        var fieldCursors = RecordFieldCursors(context, underlyingRecordCursor);
        var fieldCursorsLength = fieldCursors.Length;
        if (fieldCursorsLength > 0)
        {
            // Clang does not provide a way to get the padding of a field; we need to do it ourselves.
            // To calculate the padding of a field, work backwards from the last field to the first field using the offsets and sizes reported by Clang.

            var lastFieldCursor = fieldCursors[^1];
            var lastRecordField = CreateStructField(
                context, parentNode, lastFieldCursor, underlyingRecordCursor, fieldCursorsLength - 1, null);
            builder.Add(lastRecordField);

            for (var i = fieldCursors.Length - 2; i >= 0; i--)
            {
                var nextField = builder[^1];
                var fieldCursor = fieldCursors[i];
                var field = CreateStructField(
                    context, parentNode, fieldCursor, underlyingRecordCursor, i, nextField);
                builder.Add(field);
            }
        }

        builder.Reverse();
        var result = builder.ToImmutable();
        return result;
    }

    private ImmutableArray<CXCursor> RecordFieldCursors(ExplorerContext context, CXCursor record)
    {
        var type = clang_getCursorType(record);
        var sizeOf = SizeOf(context, type);
        if (sizeOf == 0)
        {
            return ImmutableArray<CXCursor>.Empty;
        }

        // We need to consider unions because they could be anonymous.
        //  Case 1: If the union has no tag (identifier) and has no member name (field name), the union should be promoted to an anonymous field.
        //  Case 2: If the union has no tag (identifier) and has a member name (field name), it should be included as a normal field.
        //  Case 3: If the union has a tag (identifier) and has no member name (field name), it should not be included at all as a field. (Dangling union.)
        //  Case 4: If the union has a tag (identifier) and has a member name (field name), it should be included as a normal field.
        // The problem is that C allows unions or structs to be declared inside the body of the union or struct.
        // This makes matching type identifiers to field names slightly difficult as Clang reports back the fields, unions, and structs for a given struct or union.
        // However, the unions and structs reported are always before the field for the matching union or struct, if there is one.
        // Thus, the solution here is to filter out the unions or structs that match to a field, leaving behind the anonymous structs or unions that need to get promoted.
        //  I.e. return only cursors which are fields, except for case 1.

        var fieldCursors = record.GetDescendents((child, _) =>
        {
            var isField = child.kind == CXCursorKind.CXCursor_FieldDecl;
            var isUnion = child.kind == CXCursorKind.CXCursor_UnionDecl;
            return isField || isUnion;
        });

        if (fieldCursors.IsDefaultOrEmpty)
        {
            return ImmutableArray<CXCursor>.Empty;
        }

        var filteredFieldCursors = ImmutableArray.CreateBuilder<CXCursor>();
        filteredFieldCursors.Add(fieldCursors[^1]);

        for (var index = fieldCursors.Length - 2; index >= 0; index--)
        {
            var current = fieldCursors[index];
            var next = fieldCursors[index + 1];

            if (current.kind == CXCursorKind.CXCursor_UnionDecl && next.kind == CXCursorKind.CXCursor_FieldDecl)
            {
                var typeNext = clang_getCursorType(next);
                var typeCurrent = clang_getCursorType(current);

                var typeNextCursor = clang_getTypeDeclaration(typeNext);
                var typeCurrentCursor = clang_getTypeDeclaration(typeCurrent);

                var nextData = typeNextCursor.data.ToArray();
                var previousData = typeCurrentCursor.data.ToArray();

                var cursorsAreEqual = nextData.SequenceEqual(previousData);
                if (cursorsAreEqual)
                {
                    // union has a tag and a member name
                    continue;
                }
            }

            filteredFieldCursors.Add(current);
        }

        if (filteredFieldCursors.Count > 1)
        {
            filteredFieldCursors.Reverse();
        }

        return filteredFieldCursors.ToImmutableArray();
    }

    private CRecordField CreateUnionField(
        ExplorerContext context,
        ExplorerNode parentNode,
        CXCursor cursor,
        int fieldIndex)
    {
        var fieldName = cursor.Name();
        var type = clang_getCursorType(cursor);
        var location = Location(context, cursor, type);
        var parentRecordName = parentNode.TypeName;
        var typeName = Name(parentRecordName, type, cursor, fieldIndex);

        VisitType(context, parentNode, cursor, type, typeName, location);

        var sizeOf = SizeOf(context, type);

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            Type = typeName,
            SizeOf = sizeOf
        };
    }

    private CRecordField CreateStructField(
        ExplorerContext context,
        ExplorerNode parentNode,
        CXCursor cursor,
        CXCursor parentCursor,
        int fieldIndex,
        CRecordField? nextField)
    {
        var fieldName = cursor.Name();

        var type = clang_getCursorType(cursor);
        var location = Location(context, cursor, type);
        var parentRecordName = parentNode.TypeName;
        var typeName = Name(parentRecordName, type, cursor, fieldIndex);

        VisitType(context, parentNode, cursor, type, typeName, location);

        var sizeOf = SizeOf(context, type);

        var offsetOfBits = (int)clang_Cursor_getOffsetOfField(cursor);
        int offsetOf;
        if (cursor.kind == CXCursorKind.CXCursor_UnionDecl)
        {
            offsetOf = 0;
        }
        else
        {
            if (offsetOfBits < 0 || (fieldIndex != 0 && offsetOfBits == 0))
            {
                if (nextField == null)
                {
                    offsetOf = 0;
                }
                else
                {
                    offsetOf = nextField.OffsetOf!.Value - sizeOf;
                }
            }
            else
            {
                offsetOf = offsetOfBits / 8;
            }
        }

        int paddingOf;
        if (nextField == null)
        {
            var parentType = clang_getCursorType(parentCursor);
            var parentSize = SizeOf(context, parentType);

            paddingOf = parentSize - offsetOf - sizeOf;
        }
        else
        {
            paddingOf = nextField.OffsetOf!.Value - offsetOf - sizeOf;
        }

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            Type = typeName,
            OffsetOf = offsetOf,
            PaddingOf = paddingOf,
            SizeOf = sizeOf
        };
    }

    private ImmutableArray<CEnumValue> CreateEnumValues(ExplorerContext context, CXCursor cursor)
    {
        var builder = ImmutableArray.CreateBuilder<CEnumValue>();

        var underlyingCursor = ClangUnderlyingCursor(cursor);

        var enumValuesCursors = underlyingCursor.GetDescendents((child, _) =>
        {
            if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
            {
                return false;
            }

            return true;
        });

        foreach (var enumValueCursor in enumValuesCursors)
        {
            var enumValue = CreateEnumValue(enumValueCursor);
            builder.Add(enumValue);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CEnumValue CreateEnumValue(CXCursor cursor, string? name = null)
    {
        var value = clang_getEnumConstantDeclValue(cursor);
        name ??= cursor.Name();

        return new CEnumValue
        {
            Name = name,
            Value = value
        };
    }

    private CType Type(ExplorerContext context, string typeName, CXCursor cursor, CXType type, CKind kind)
    {
        var declaration = clang_getTypeDeclaration(type);
        if (declaration.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            declaration = cursor;
        }

        var sizeOf = SizeOf(context, type);
        var alignOfValue = (int)clang_Type_getAlignOf(type);
        int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
        var arraySizeValue = (int)clang_getArraySize(type);
        int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;

        int? elementSize = null;
        if (kind == CKind.Array)
        {
            var (_, arrayType) = TypeKind(context, type);
            var elementType = clang_getElementType(arrayType);
            elementSize = SizeOf(context, elementType);
        }

        var location = Location(context, declaration, type, true);
        var isAnonymous = clang_Cursor_isAnonymous(declaration) > 0;

        var name = typeName;
        if (IsBlocked(context, type, location, kind, string.Empty, typeName))
        {
            if (kind == CKind.Pointer)
            {
                var pointeeTypeName = typeName.TrimEnd('*');
                name = name.Replace(pointeeTypeName, "void", StringComparison.InvariantCulture);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        var cType = new CType
        {
            Name = name,
            Kind = kind,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ElementSize = elementSize,
            ArraySize = arraySize,
            Location = location,
            IsAnonymous = isAnonymous
        };

        var fileName = location.FileName;
        if (context.Options.HeaderFilesBlocked.Contains(fileName))
        {
            var diagnostic = new TypeFromIgnoredHeaderDiagnostic(typeName, fileName);
            context.Diagnostics.Add(diagnostic);
        }

        return cType;
    }

    private int SizeOf(ExplorerContext context, CXType type)
    {
        var sizeOf = (int)clang_Type_getSizeOf(type);
        if (sizeOf >= 0)
        {
            return sizeOf;
        }

        if (sizeOf != -2)
        {
            throw new UseCaseException("Unexpected size for Clang type. Please submit an issue on GitHub!");
        }

        var (kind, underlyingType) = TypeKind(context, type);
        switch (kind)
        {
            case CKind.Array:
                if (type.kind != CXTypeKind.CXType_IncompleteArray)
                {
                    throw new UseCaseException(
                        "Unexpected case when determining size for Clang type. Please submit an issue on GitHub!");
                }

                return (int)clang_Type_getAlignOf(type);
            case CKind.Primitive:
            case CKind.OpaqueType:
                return 0;
            case CKind.Pointer:
                return (int)clang_Type_getSizeOf(underlyingType);
            default:
                var location = Location(context, clang_getTypeDeclaration(type), type);
                throw new UseCaseException(
                    $"Unexpected case when determining size for Clang type: {location}. Please submit an issue on GitHub!");
        }
    }

    private (CKind Kind, CXType Type) TypeKind(ExplorerContext context, CXType type)
    {
        var cursor = clang_getTypeDeclaration(type);
        var cursorType = cursor.kind != CXCursorKind.CXCursor_NoDeclFound ? clang_getCursorType(cursor) : type;
        if (cursorType.IsPrimitive())
        {
            return (CKind.Primitive, type);
        }

        switch (cursorType.kind)
        {
            case CXTypeKind.CXType_Enum:
                return (CKind.Enum, cursorType);
            case CXTypeKind.CXType_Record:
                var sizeOfRecord = clang_Type_getSizeOf(cursorType);
                return sizeOfRecord == -2 ? (CKind.OpaqueType, cursorType) : (CKind.Record, cursorType);
            case CXTypeKind.CXType_Typedef:
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                if (underlyingType.kind == CXTypeKind.CXType_Pointer)
                {
                    return (CKind.Typedef, cursorType);
                }

                var alias = TypeKind(context, underlyingType);
                var sizeOfAlias = SizeOf(context, alias.Type);
                return sizeOfAlias == -2 ? (CKind.OpaqueType, cursorType) : (CKind.Typedef, cursorType);

            case CXTypeKind.CXType_FunctionNoProto or CXTypeKind.CXType_FunctionProto:
                return (CKind.Function, cursorType);
            case CXTypeKind.CXType_Pointer:
                var pointeeType = clang_getPointeeType(cursorType);
                if (pointeeType.kind == CXTypeKind.CXType_Attributed)
                {
                    pointeeType = clang_Type_getModifiedType(pointeeType);
                }

                if (pointeeType.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
                {
                    return (CKind.FunctionPointer, pointeeType);
                }

                return (CKind.Pointer, cursorType);

            case CXTypeKind.CXType_Attributed:
                var modifiedType = clang_Type_getModifiedType(cursorType);
                return TypeKind(context, modifiedType);
            case CXTypeKind.CXType_Elaborated:
                var namedType = clang_Type_getNamedType(cursorType);
                return TypeKind(context, namedType);
            case CXTypeKind.CXType_ConstantArray:
            case CXTypeKind.CXType_IncompleteArray:
                return (CKind.Array, cursorType);
            case CXTypeKind.CXType_Unexposed:
                var canonicalType = clang_getCanonicalType(type);
                return TypeKind(context, canonicalType);
        }

        var up = new UseCaseException($"Unknown type kind '{type.kind}'.");
        throw up;
    }

    private static CXCursor ClangUnderlyingCursor(CXCursor cursor)
    {
        var underlyingCursor = cursor;
        if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            if (underlyingType.kind == CXTypeKind.CXType_Elaborated)
            {
                var namedType = clang_Type_getNamedType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(namedType);
            }
            else if (underlyingType.kind == CXTypeKind.CXType_Pointer)
            {
                var pointeeType = clang_getPointeeType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(pointeeType);
            }
            else
            {
                underlyingCursor = clang_getTypeDeclaration(underlyingType);
            }

            if (!underlyingType.IsPrimitive())
            {
                Debug.Assert(underlyingCursor.kind != CXCursorKind.CXCursor_NoDeclFound, "Expected declaration.");
            }
        }

        return underlyingCursor;
    }

    private void VisitType(
        ExplorerContext context,
        ExplorerNode parentNode,
        CXCursor cursor,
        CXType typeCandidate,
        string typeName,
        CLocation location)
    {
        var (kind, type) = TypeKind(context, typeCandidate);
        if (IsBlocked(context, type, location, kind, string.Empty, typeName))
        {
            return;
        }

        if (!IsNewType(context, kind, typeName, typeCandidate, cursor))
        {
            return;
        }

        var isValidTypeName = TypeNameIsValid(context, typeName);
        if (!isValidTypeName)
        {
            return;
        }

        _logger.ExploreCodeVisitType(typeName);

        switch (kind)
        {
            case CKind.Typedef:
                VisitTypedef(context, typeName, parentNode, type);
                break;
            case CKind.FunctionPointer:
                VisitFunctionPointer(context, typeName, parentNode, cursor, type);
                break;
            case CKind.Pointer:
                VisitPointer(context, parentNode, cursor, type);
                break;
            case CKind.Primitive:
                VisitPrimitive(context, parentNode, typeName, type);
                break;
            case CKind.OpaqueType:
                VisitOpaqueType(context, parentNode, typeName, cursor, type);
                break;
            case CKind.Record:
                VisitRecord(context, parentNode, typeName, cursor, type);
                break;
            case CKind.Array:
                VisitArray(context, parentNode, typeName, cursor, type);
                break;
            default:
                var up = new UseCaseException($"Unexpected visit type '{kind}'.");
                throw up;
        }
    }

    private void VisitArray(
        ExplorerContext context,
        ExplorerNode parentNode,
        string name,
        CXCursor cursor,
        CXType type)
    {
        EnqueueNode(
            context,
            CKind.Array,
            CLocation.Null,
            parentNode,
            cursor,
            type,
            string.Empty,
            name);
    }

    private void VisitRecord(
        ExplorerContext context,
        ExplorerNode parentNode,
        string name,
        CXCursor cursor,
        CXType type)
    {
        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(context, locationCursor, type, true);
        EnqueueNode(
            context,
            CKind.Record,
            location,
            parentNode,
            cursor,
            type,
            string.Empty,
            name);
    }

    private void VisitOpaqueType(
        ExplorerContext context,
        ExplorerNode parentNode,
        string name,
        CXCursor cursor,
        CXType type)
    {
        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(context, locationCursor, type, true);
        EnqueueNode(
            context,
            CKind.OpaqueType,
            location,
            parentNode,
            cursor,
            type,
            string.Empty,
            name);
    }

    private void VisitPrimitive(
        ExplorerContext context,
        ExplorerNode parentNode,
        string name,
        CXType type)
    {
        EnqueueNode(
            context,
            CKind.Primitive,
            CLocation.Null,
            parentNode,
            default,
            type,
            string.Empty,
            name);
    }

    private void VisitPointer(
        ExplorerContext context,
        ExplorerNode parentNode,
        CXCursor cursor,
        CXType type)
    {
        var pointeeTypeCandidate = clang_getPointeeType(type);
        var (_, pointeeType) = TypeKind(context, pointeeTypeCandidate);
        var pointeeCursorCandidate = clang_getTypeDeclaration(pointeeType);
        var pointeeCursor = pointeeCursorCandidate.kind == CXCursorKind.CXCursor_NoDeclFound ? cursor : pointeeCursorCandidate;
        var pointeeTypeName = Name(parentNode.TypeName!, pointeeType, pointeeCursor);
        var pointeeLocation = Location(context, pointeeCursor, pointeeType);
        VisitType(
            context,
            parentNode,
            pointeeCursor,
            pointeeType,
            pointeeTypeName,
            pointeeLocation);
    }

    private void VisitMacro(
        ExplorerContext context,
        ExplorerNode parentNode,
        CXCursor cursor)
    {
        var name = cursor.Name();

        // Function-like macros currently not implemented
        // https://github.com/lithiumtoast/c2cs/issues/35
        if (clang_Cursor_isMacroFunctionLike(cursor) != 0)
        {
            context.MacroFunctionLikeNames.Add(name);
            return;
        }

        if (!context.Options.IsEnabledMacroObjects)
        {
            return;
        }

        var location = Location(context, cursor, default);
        EnqueueNode(
            context, CKind.MacroDefinition, location, parentNode, cursor, default, name, string.Empty);
    }

    private void VisitFunction(ExplorerContext context, ExplorerNode parentNode, CXCursor cursor)
    {
        var name = cursor.Name();

        if (context.VisitedFunctionNames.Contains(name))
        {
            // A header file may contain the declaration of the function and then later decide to implement it.
            return;
        }

        var type = clang_getCursorType(cursor);
        var location = Location(context, cursor, type);
        context.VisitedFunctionNames.Add(name);
        EnqueueNode(context, CKind.Function, location, parentNode, cursor, type, name, string.Empty);
    }

    private void VisitVariable(ExplorerContext context, ExplorerNode parentNode, CXCursor cursor)
    {
        if (!context.Options.IsEnabledVariables)
        {
            return;
        }

        var type = clang_getCursorType(cursor);
        if (type.kind == CXTypeKind.CXType_Unexposed)
        {
            type = clang_getCanonicalType(type);
        }

        var name = cursor.Name();
        var location = Location(context, cursor, type);
        var typeName = Name(parentNode.TypeName!, type, cursor);
        EnqueueNode(context, CKind.Variable, location, parentNode, cursor, type, name, typeName);
    }

    private void VisitEnum(ExplorerContext context, ExplorerNode parentNode, CXCursor cursor)
    {
        var type = clang_getCursorType(cursor);
        var name = type.Name();
        var location = Location(context, cursor, default);
        EnqueueNode(
            context, CKind.Enum, location, parentNode, cursor, type, name, name);
    }

    private void VisitTypedef(
        ExplorerContext context,
        string name,
        ExplorerNode parentNode,
        CXType type)
    {
        var typedefCursor = clang_getTypeDeclaration(type);
        var location = Location(context, typedefCursor, type);
        EnqueueNode(context, CKind.Typedef, location, parentNode, typedefCursor, type, string.Empty, name);
    }

    private void VisitFunctionPointer(
        ExplorerContext context,
        string typeName,
        ExplorerNode parentNode,
        CXCursor cursor,
        CXType type)
    {
        var name = GetFunctionPointerName(cursor, type);
        var location = Location(context, cursor, type);
        EnqueueNode(
            context,
            CKind.FunctionPointer,
            location,
            parentNode,
            cursor,
            type,
            name,
            typeName);
    }

    private string Name(
        string? parentName,
        CXType type,
        CXCursor cursor,
        int index = 0)
    {
        if (type.kind is CXTypeKind.CXType_FunctionNoProto or CXTypeKind.CXType_FunctionProto)
        {
            return cursor.Name();
        }

        var name = type.Name();
        var typeCursor = clang_getTypeDeclaration(type);

        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        if (isAnonymous)
        {
            return $"{parentName}_ANONYMOUS_FIELD{index}";
        }

        if (name.Contains("(unnamed at ", StringComparison.InvariantCulture))
        {
            return $"{parentName}_UNNAMED_FIELD{index}";
        }

        var isUnion = typeCursor.kind == CXCursorKind.CXCursor_UnionDecl;
        if (isUnion)
        {
            var unionName = typeCursor.Name();
            if (string.IsNullOrEmpty(unionName))
            {
                switch (typeCursor.kind)
                {
                    case CXCursorKind.CXCursor_UnionDecl:
                        return $"{parentName}_{unionName}";
                    case CXCursorKind.CXCursor_StructDecl:
                        return $"{parentName}_{unionName}";
                    default:
                    {
                        // pretty sure this case is not possible, but it's better safe than sorry!
                        var up = new UseCaseException($"Unknown anonymous cursor kind '{typeCursor.kind}'");
                        throw up;
                    }
                }
            }
        }

        if (type.kind == CXTypeKind.CXType_ConstantArray)
        {
            var arraySize = clang_getArraySize(type);
            name = $"{name}[{arraySize}]";
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new UseCaseException($"Type name was not found.");
        }

        return name;
    }

    private CLocation Location(ExplorerContext context, CXCursor cursor, CXType type, bool drillDown = false)
    {
        var location = Location(cursor, type, drillDown);

        if (string.IsNullOrEmpty(location.FilePath))
        {
            return location;
        }

        foreach (var directory in context.UserIncludeDirectories)
        {
            if (context.Options.IsEnabledLocationFullPaths)
            {
                location.FilePath = location.FilePath;
            }
            else if (location.FilePath.Contains(directory, StringComparison.InvariantCulture))
            {
                location.FilePath = location.FilePath.Replace(directory, string.Empty, StringComparison.InvariantCulture).Trim('/', '\\');
                break;
            }
        }

        return location;
    }

    private static CLocation LocationInTranslationUnit(
        CXCursor declaration,
        int lineNumber,
        int columnNumber)
    {
        var translationUnit = clang_Cursor_getTranslationUnit(declaration);
        var cursor = clang_getTranslationUnitCursor(translationUnit);
        var spelling = clang_getCursorSpelling(cursor);
        string filePath = clang_getCString(spelling);
        return new CLocation
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            LineNumber = lineNumber,
            LineColumn = columnNumber
        };
    }

    private static unsafe CLocation Location(CXCursor cursor, CXType type, bool drillDown)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            return CLocation.Null;
        }

        if (drillDown)
        {
            if (type.kind == CXTypeKind.CXType_Pointer)
            {
                return CLocation.Null;
            }

            var isPrimitive = type.IsPrimitive();
            if (isPrimitive)
            {
                return CLocation.Null;
            }

            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl && type.kind == CXTypeKind.CXType_Typedef)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var underlyingCursor = clang_getTypeDeclaration(underlyingType);
                var underlyingLocation = Location(underlyingCursor, underlyingType, true);
                if (!underlyingLocation.IsNull)
                {
                    return underlyingLocation;
                }
            }
        }

        var location = clang_getCursorLocation(cursor);
        CXFile file;
        uint lineNumber;
        uint columnNumber;
        uint offset;

        clang_getFileLocation(location, &file, &lineNumber, &columnNumber, &offset);

        var handle = (IntPtr)file.Data;
        if (handle == IntPtr.Zero)
        {
            return LocationInTranslationUnit(cursor, (int)lineNumber, (int)columnNumber);
        }

        var clangFileName = clang_getFileName(file);
        string fileNamePath = clang_getCString(clangFileName);
        var fileName = Path.GetFileName(fileNamePath);
        var fullFilePath = string.IsNullOrEmpty(fileNamePath) ? string.Empty : Path.GetFullPath(fileNamePath);

        return new CLocation
        {
            FileName = fileName,
            FilePath = fullFilePath,
            LineNumber = (int)lineNumber,
            LineColumn = (int)columnNumber
        };
    }
}
