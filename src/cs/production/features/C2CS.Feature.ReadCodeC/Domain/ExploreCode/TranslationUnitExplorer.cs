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
            ExploreTranslationUnit(context, translationUnit);
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
            Constants = constants
        };

        return result;
    }

    private void Explore(ExplorerContext context)
    {
        while (context.FrontierMacros.Count > 0)
        {
            var node = context.FrontierMacros.PopFront()!;
            ExploreNode(context, node);
        }

        while (context.FrontierApi.Count > 0)
        {
            var node = context.FrontierApi.PopFront()!;
            ExploreNode(context, node);
        }

        while (context.FrontierTypes.Count > 0)
        {
            var node = context.FrontierTypes.PopFront()!;
            ExploreNode(context, node);
        }
    }

    private void ExploreNode(ExplorerContext context, ExplorerNode node)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.Kind)
        {
            case CKind.Variable:
                ExploreVariable(context, node.CursorName, node.TypeName, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Function:
                ExploreFunction(context, node);
                break;
            case CKind.Typedef:
                ExploreTypedef(context, node);
                break;
            case CKind.OpaqueType:
                ExploreOpaqueType(context, node.TypeName, node.Type, node.Location);
                break;
            case CKind.Enum:
                ExploreEnum(context, node.TypeName, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Record:
                ExploreRecord(context, node);
                break;
            case CKind.FunctionPointer:
                ExploreFunctionPointer(context, node);
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
            case CKind.Primitive:
                return false;
            case CKind.Array:
            {
                if (type.kind == CXTypeKind.CXType_Attributed)
                {
                    type = clang_Type_getModifiedType(type);
                }

                var elementTypeCandidate = clang_getElementType(type);
                var (elementKind, elementType) = TypeKind(context, elementTypeCandidate);
                var elementCursor = clang_getTypeDeclaration(elementType);
                var elementLocation = Location(context, elementCursor, elementType);
                var elementTypeName = elementType.Name();
                return IsBlocked(context, elementType, elementLocation, elementKind, string.Empty, elementTypeName);
            }

            case CKind.Pointer:
            {
                if (type.kind == CXTypeKind.CXType_Attributed)
                {
                    type = clang_Type_getModifiedType(type);
                }

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
            return false;
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

    private void ExploreTranslationUnit(ExplorerContext context, CXTranslationUnit translationUnit)
    {
        var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);

        var cursors = translationUnitCursor.GetDescendents(
            (child, _) => IsTranslationUnitChildCursorOfInterest(context, child));

        foreach (var cursor in cursors)
        {
            VisitTranslationUnitChildCursor(context, cursor);
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

    private void VisitTranslationUnitChildCursor(ExplorerContext context, CXCursor cursor)
    {
        switch (cursor.kind)
        {
            case CXCursorKind.CXCursor_FunctionDecl:
                VisitFunction(context, cursor);
                break;
            case CXCursorKind.CXCursor_VarDecl:
                VisitVariable(context, cursor);
                break;
            case CXCursorKind.CXCursor_EnumDecl:
                VisitEnum(context, null, cursor);
                break;
            case CXCursorKind.CXCursor_MacroDefinition:
                VisitMacro(context, cursor);
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
        var (elementTypeKind, elementType) = TypeKind(context, elementTypeCandidate);
        var elementCursor = clang_getTypeDeclaration(elementType);
        var elementTypeName = TypeName(elementType, node.TypeName);
        var elementLocation = Location(context, elementCursor, elementType);

        VisitType(
            context,
            node,
            elementCursor,
            elementType,
            elementTypeCandidate,
            elementTypeName,
            elementLocation,
            elementTypeKind);
    }

    private void ExplorePointer(
        ExplorerContext context, ExplorerNode node)
    {
        var pointeeTypeCandidate = clang_getPointeeType(node.Type);
        var (pointeeTypeKind, pointeeType) = TypeKind(context, pointeeTypeCandidate);
        var pointeeCursor = clang_getTypeDeclaration(pointeeType);
        var pointeeTypeName = TypeName(pointeeType, node.TypeName);
        var pointeeLocation = Location(context, pointeeCursor, pointeeType);

        VisitType(
            context,
            node,
            pointeeCursor,
            pointeeType,
            pointeeTypeCandidate,
            pointeeTypeName,
            pointeeLocation,
            pointeeTypeKind);
    }

    private void ExploreMacro(ExplorerContext context, ExplorerNode node)
    {
        var macroName = node.CursorName;
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
        CXType typeCandidate,
        CLocation location,
        ExplorerNode parentNode)
    {
        if (context.Variables.ContainsKey(name))
        {
            return;
        }

        _logger.ExploreCodeVariable(name);

        var (typeKind, type) = TypeKind(context, typeCandidate);

        VisitType(
            context,
            parentNode,
            cursor,
            type,
            typeCandidate,
            typeName,
            location,
            typeKind);

        var variable = new CVariable
        {
            Location = location,
            Name = name,
            Type = typeName
        };

        context.Variables.Add(variable.Name, variable);
    }

    private void ExploreFunction(ExplorerContext context, ExplorerNode node)
    {
        var name = node.CursorName;
        _logger.ExploreCodeFunction(name);

        var callingConvention = CreateFunctionCallingConvention(node.Type);
        var resultTypeCandidate = clang_getCursorResultType(node.Cursor);
        var (resultTypeKind, resultType) = TypeKind(context, resultTypeCandidate);
        var resultTypeCursor = clang_getTypeDeclaration(resultType);
        var resultTypeName = TypeName(resultType, node.Parent?.TypeName);
        var resultLocation = Location(context, resultTypeCursor, resultType);

        VisitType(
            context,
            node.Parent,
            node.Cursor,
            resultType,
            resultTypeCandidate,
            resultTypeName,
            resultLocation,
            resultTypeKind);

        var resultTypeC = CreateType(context, resultTypeName, resultType, resultTypeCandidate, resultTypeKind);
        var functionParameters = CreateFunctionParameters(context, node.Cursor, node);

        var function = new CFunction
        {
            Name = name,
            Location = node.Location,
            CallingConvention = callingConvention,
            ReturnType = resultTypeC,
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
        ExplorerNode? parentNode)
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

        var integerTypeCandidate = clang_getEnumDeclIntegerType(typeCursor);
        var (integerTypeKind, integerType) = TypeKind(context, integerTypeCandidate);
        var integerCursor = clang_getTypeDeclaration(integerType);
        var integerTypeName = TypeName(integerType, parentNode?.TypeName!);
        var integerTypeLocation = Location(context, integerCursor, integerType);

        VisitType(
            context,
            parentNode,
            integerCursor,
            integerType,
            integerTypeCandidate,
            integerTypeName,
            integerTypeLocation,
            integerTypeKind);

        var integerTypeC = CreateType(context, integerTypeName, integerType, integerTypeCandidate, integerTypeKind);
        var enumValues = CreateEnumValues(context, typeCursor);

        var @enum = new CEnum
        {
            Name = name,
            Location = location,
            IntegerType = integerTypeC,
            Values = enumValues
        };

        context.Enums.Add(name, @enum);
    }

    private void ExploreRecord(ExplorerContext context, ExplorerNode node)
    {
        var typeName = node.TypeName;
        var location = node.Location;

        if (context.Options.OpaqueTypesNames.Contains(typeName))
        {
            ExploreOpaqueType(context, typeName, node.Type, location);
            return;
        }

        _logger.ExploreCodeRecord(typeName);

        var typeCursor = clang_getTypeDeclaration(node.Type);
        var typeUnderlyingCursor = ClangUnderlyingCursor(typeCursor);
        var isUnion = typeUnderlyingCursor.kind == CXCursorKind.CXCursor_UnionDecl;
        var sizeOf = SizeOf(context, node.ContainerType);
        var alignOf = (int)clang_Type_getAlignOf(node.ContainerType);
        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        var parentName = isAnonymous ? node.Parent?.TypeName ?? string.Empty : string.Empty;

        CRecord record;

        if (isUnion)
        {
            var fields = CreateUnionFields(context, typeUnderlyingCursor, node);
            record = new CRecord
            {
                RecordKind = CRecordKind.Union,
                Location = location,
                Name = typeName,
                ParentName = parentName,
                Fields = fields,
                SizeOf = sizeOf,
                AlignOf = alignOf
            };
        }
        else
        {
            var fields = CreateStructFields(context, typeUnderlyingCursor, node);
            record = new CRecord
            {
                RecordKind = CRecordKind.Struct,
                Location = location,
                Name = typeName,
                ParentName = parentName,
                Fields = fields,
                SizeOf = sizeOf,
                AlignOf = alignOf
            };
        }

        context.Records.Add(record.Name, record);
    }

    private void ExploreTypedef(ExplorerContext context, ExplorerNode node)
    {
        var typedefCursor = node.Cursor;
        if (typedefCursor.kind != CXCursorKind.CXCursor_TypedefDecl)
        {
            throw new ClangException($"Expected a typedef declaration but found: {typedefCursor.kind}");
        }

        var name = node.TypeName;
        var location = node.Location;

        if (context.Options.OpaqueTypesNames.Contains(name))
        {
            ExploreOpaqueType(context, name, node.Type, location);
            return;
        }

        var aliasTypeCandidate = clang_getTypedefDeclUnderlyingType(node.Cursor);
        var (aliasTypeKind, aliasType) = TypeKind(context, aliasTypeCandidate);
        var aliasCursor = clang_getTypeDeclaration(aliasType);
        var aliasTypeName = TypeName(aliasType, node.Parent?.TypeName);
        var aliasLocation = Location(context, aliasCursor, aliasType);

        switch (aliasTypeKind)
        {
            case CKind.Enum:
                ExploreEnum(context, name, aliasCursor, aliasType, location, node.Parent);
                return;
            case CKind.Record:
                ExploreRecord(context, node);
                return;
            case CKind.FunctionPointer:
                var typeName = aliasType.Name();
                VisitFunctionPointer(
                    context, name, typeName, node.Parent, node.Cursor, aliasType, aliasTypeCandidate, node.Location);
                return;
        }

        bool shouldVisitAliasType;
        string mappedAliasTypeName;
        CXType mappedAliasType;
        CKind mappedAliasTypeKind;
        if (IsBlocked(context, aliasType, aliasLocation, aliasTypeKind, string.Empty, aliasTypeName))
        {
            var isMapped = TryMapBlockedType(
                context,
                aliasTypeKind,
                aliasTypeName,
                aliasType,
                out mappedAliasTypeName,
                out mappedAliasType,
                out mappedAliasTypeKind);
            shouldVisitAliasType = !isMapped;
        }
        else
        {
            mappedAliasTypeName = aliasTypeName;
            mappedAliasType = aliasType;
            mappedAliasTypeKind = aliasTypeKind;
            shouldVisitAliasType = true;
        }

        _logger.ExploreCodeTypedef(name);

        if (shouldVisitAliasType)
        {
            VisitType(
                context,
                node.Parent,
                aliasCursor,
                aliasType,
                aliasTypeCandidate,
                aliasTypeName,
                aliasLocation,
                aliasTypeKind);
        }

        var aliasTypeC = CreateType(context, mappedAliasTypeName, mappedAliasType, aliasTypeCandidate, mappedAliasTypeKind);

        var typedef = new CTypedef
        {
            Name = name,
            Location = location,
            UnderlyingType = aliasTypeC
        };

        context.Typedefs.Add(typedef.Name, typedef);
    }

    private bool TryMapBlockedType(
        ExplorerContext context,
        CKind typeKind,
        string typeName,
        CXType type,
        out string mappedTypeName,
        out CXType mappedType,
        out CKind mappedTypeKind)
    {
        switch (typeKind)
        {
            case CKind.Primitive:
                mappedTypeName = typeName;
                mappedType = type;
                mappedTypeKind = typeKind;
                return true;

            case CKind.Pointer:
            {
                var pointerIndex = typeName.IndexOf('*', StringComparison.InvariantCulture);
                var pointerTypeName = typeName[pointerIndex..];
                mappedTypeName = "void" + pointerTypeName;
                mappedType = type;
                mappedTypeKind = typeKind;
                return true;
            }

            case CKind.Typedef:
            {
                var canonicalTypeCandidate = clang_getCanonicalType(type);
                var (canonicalTypeKind, canonicalType) = TypeKind(context, canonicalTypeCandidate);
                var canonicalTypeName = TypeName(canonicalType, typeName);
                return TryMapBlockedType(
                    context,
                    canonicalTypeKind,
                    canonicalTypeName,
                    canonicalType,
                    out mappedTypeName,
                    out mappedType,
                    out mappedTypeKind);
            }

            default:
                mappedTypeName = typeName;
                mappedType = type;
                mappedTypeKind = typeKind;
                return false;
        }
    }

    private void ExploreOpaqueType(
        ExplorerContext context,
        string typeName,
        CXType type,
        CLocation location)
    {
        _logger.ExploreCodeOpaqueType(typeName);

        var sizeOf = SizeOf(context, type);

        var opaqueDataType = new COpaqueType
        {
            Name = typeName,
            Location = location,
            SizeOf = sizeOf
        };

        context.OpaqueDataTypes.Add(opaqueDataType.Name, opaqueDataType);
    }

    private void ExploreFunctionPointer(ExplorerContext context, ExplorerNode node)
    {
        var name = node.CursorName;
        if (context.FunctionPointers.ContainsKey(name))
        {
            return;
        }

        var typeName = node.Type.Name();
        var functionPointer = CreateFunctionPointer(context, node, name, typeName, node.Cursor, node.Parent, node.Type, node.ContainerType, node.Location);
        context.FunctionPointers.Add(name, functionPointer);
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

    private bool IsAlreadyVisited(ExplorerContext context, CKind kind, string name, CXType type, CXCursor cursor)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        HashSet<string> visitedNames;
        switch (kind)
        {
            case CKind.FunctionPointer:
                name = GetFunctionPointerName(cursor, type);
                visitedNames = context.VisitedFunctionPointerNames;
                break;
            case CKind.Primitive:
                visitedNames = context.VisitedPrimitiveNames;
                break;
            case CKind.Pointer:
                visitedNames = context.VisitedPointerNames;
                break;
            case CKind.Record:
                visitedNames = context.VisitedRecordNames;
                break;
            case CKind.Typedef:
                visitedNames = context.VisitedTypedefNames;
                break;
            case CKind.Array:
                visitedNames = context.VisitedArrayNames;
                break;
            case CKind.OpaqueType:
                visitedNames = context.VisitedOpaqueTypeNames;
                break;
            default:
                throw new NotImplementedException();
        }

        var alreadyVisited = visitedNames.Contains(name);
        if (alreadyVisited)
        {
            return false;
        }

        visitedNames.Add(name);
        return true;
    }

    private void EnqueueNode(
        ExplorerContext context,
        CKind kind,
        CLocation location,
        ExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        CXType containerType,
        string cursorName,
        string typeName)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            type = clang_Type_getModifiedType(type);
        }

        if (kind != CKind.MacroDefinition && type.kind == CXTypeKind.CXType_Invalid)
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
            containerType,
            cursorName,
            typeName);

        var frontier = kind switch
        {
            CKind.MacroDefinition => context.FrontierMacros,
            CKind.Function => context.FrontierApi,
            CKind.Variable => context.FrontierApi,
            _ => context.FrontierTypes,
        };

        frontier.PushBack(node);
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
        var typeCandidate = clang_getCursorType(cursor);
        var name = cursor.Name();

        var (typeKind, type) = TypeKind(context, typeCandidate);
        var typeName = TypeName(type, parentNode.TypeName);
        var location = Location(context, cursor, type);

        VisitType(
            context,
            parentNode,
            cursor,
            type,
            typeCandidate,
            typeName,
            location,
            typeKind);

        var codeLocation = Location(context, cursor, typeCandidate);
        var typeC = CreateType(context, typeName, type, typeCandidate, typeKind);

        return new CFunctionParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeC
        };
    }

    private CFunctionPointer CreateFunctionPointer(
        ExplorerContext context,
        ExplorerNode node,
        string name,
        string typeName,
        CXCursor cursor,
        ExplorerNode? parentNode,
        CXType type,
        CXType containerType,
        CLocation location)
    {
        var functionPointerParameters = CreateFunctionPointerParameters(
            context, type, node);

        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            type = clang_Type_getModifiedType(type);
        }

        var returnTypeCandidate = clang_getResultType(type);
        var (returnTypeKind, returnType) = TypeKind(context, returnTypeCandidate);
        var returnTypeName = TypeName(returnType, parentNode?.TypeName);
        var returnTypeCursor = clang_getTypeDeclaration(returnType);
        var returnTypeLocation = Location(context, returnTypeCursor, returnType);
        VisitType(
            context,
            parentNode,
            cursor,
            returnType,
            returnTypeCandidate,
            returnTypeName,
            returnTypeLocation,
            returnTypeKind);

        if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var pointeeType = clang_getPointeeType(underlyingType);
            var functionProtoType = pointeeType.kind == CXTypeKind.CXType_Invalid ? underlyingType : pointeeType;
            typeName = TypeName(functionProtoType, parentNode?.TypeName);
        }

        var typeC = CreateType(context, typeName, type, containerType, CKind.FunctionPointer);
        var returnTypeC = CreateType(context, returnTypeName, returnTypeCandidate, returnTypeCandidate, returnTypeKind);

        var functionPointer = new CFunctionPointer
        {
            Name = name,
            Location = location,
            Type = typeC,
            ReturnType = returnTypeC,
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
        CXType typeCandidate,
        ExplorerNode parentNode)
    {
        var (typeKind, type) = TypeKind(context, typeCandidate);
        var cursor = clang_getTypeDeclaration(type);
        var typeName = TypeName(type, parentNode.TypeName);
        var location = Location(context, cursor, type);

        VisitType(
            context,
            parentNode,
            cursor,
            type,
            typeCandidate,
            typeName,
            location,
            typeKind);

        var typeC = CreateType(context, typeName, type, typeCandidate, typeKind);

        return new CFunctionPointerParameter
        {
            Type = typeC
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

    private ImmutableArray<CXCursor> RecordFieldCursors(ExplorerContext context, CXCursor recordCursor)
    {
        var recordType = clang_getCursorType(recordCursor);
        var recordSizeOf = SizeOf(context, recordType);
        if (recordSizeOf == 0)
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

        var fieldCursors = recordCursor.GetDescendents((child, _) =>
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
        var typeCandidate = clang_getCursorType(cursor);
        var (kind, type) = TypeKind(context, typeCandidate);
        var location = Location(context, cursor, type);
        var parentRecordName = parentNode.TypeName;
        var typeName = TypeName(typeCandidate, parentRecordName, fieldIndex);

        VisitType(
            context,
            parentNode,
            cursor,
            type,
            typeCandidate,
            typeName,
            location,
            kind);

        var typeC = CreateType(context, typeName, type, typeCandidate, kind);
        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            Type = typeC
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

        var typeCandidate = clang_getCursorType(cursor);
        var (kind, type) = TypeKind(context, typeCandidate);
        var location = Location(context, cursor, type);
        var parentRecordName = parentNode.TypeName;
        var typeName = TypeName(type, parentRecordName, fieldIndex);

        VisitType(
            context,
            parentNode,
            cursor,
            type,
            typeCandidate,
            typeName,
            location,
            kind);

        var typeC = CreateType(context, typeName, type, typeCandidate, kind);

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
                    offsetOf = nextField.OffsetOf!.Value - typeC.SizeOf;
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

            paddingOf = parentSize - offsetOf - typeC.SizeOf;
        }
        else
        {
            paddingOf = nextField.OffsetOf!.Value - offsetOf - typeC.SizeOf;
        }

        return new CRecordField
        {
            Name = fieldName,
            Location = location,
            Type = typeC,
            OffsetOf = offsetOf,
            PaddingOf = paddingOf,
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

    private CType CreateType(
        ExplorerContext context, string typeName, CXType type, CXType containerType, CKind kind)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            var typeCandidate = clang_Type_getModifiedType(type);
            return CreateType(context, typeName, typeCandidate, containerType, kind);
        }

        var sizeOf = SizeOf(context, containerType);
        var alignOfValue = (int)clang_Type_getAlignOf(containerType);
        int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
        var arraySizeValue = (int)clang_getArraySize(containerType);
        int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;

        int? elementSize = null;
        if (kind == CKind.Array)
        {
            var (_, arrayType) = TypeKind(context, type);
            var elementType = clang_getElementType(arrayType);
            elementSize = SizeOf(context, elementType);
        }

        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(context, locationCursor, type);
        var isAnonymous = clang_Cursor_isAnonymous(locationCursor) > 0;

        var name = typeName;
        // if (IsBlocked(context, type, location, kind, string.Empty, typeName))
        // {
        //     if (kind == CKind.Pointer)
        //     {
        //         var pointeeTypeName = typeName.TrimEnd('*');
        //         name = name.Replace(pointeeTypeName, "void", StringComparison.InvariantCulture);
        //     }
        //     else
        //     {
        //         throw new NotImplementedException();
        //     }
        // }

        CType? innerType = null;
        if (kind is CKind.Pointer)
        {
            var pointeeTypeCandidate = clang_getPointeeType(type);
            var (pointeeTypeKind, pointeeType) = TypeKind(context, pointeeTypeCandidate);
            var pointeeTypeName = pointeeType.Name();
            innerType = CreateType(context, pointeeTypeName, pointeeType, pointeeTypeCandidate, pointeeTypeKind);
        }
        else if (kind is CKind.Array)
        {
            var elementTypeCandidate = clang_getArrayElementType(type);
            var (elementTypeKind, elementType) = TypeKind(context, elementTypeCandidate);
            var elementTypeName = elementType.Name();
            innerType = CreateType(context, elementTypeName, elementType, elementTypeCandidate, elementTypeKind);
        }

        var cType = new CType
        {
            Name = name,
            Kind = kind,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ElementSize = elementSize,
            ArraySizeOf = arraySize,
            Location = location,
            IsAnonymous = isAnonymous ? true : null,
            InnerType = innerType
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
        if (type.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
        {
            var up = new UseCaseException("Unexpected kind for size.");
            throw up;
        }

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
        ExplorerNode? parentNode,
        CXCursor cursor,
        CXType type,
        CXType containerType,
        string typeName,
        CLocation location,
        CKind kind)
    {
        if (IsBlocked(context, type, location, kind, string.Empty, typeName))
        {
            return;
        }

        if (!IsAlreadyVisited(context, kind, typeName, type, cursor))
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
                VisitFunctionPointer(context, typeName, typeName, parentNode, cursor, type, containerType, location);
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
                VisitRecord(context, parentNode, typeName, type);
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
        ExplorerNode? parentNode,
        string name,
        CXCursor cursor,
        CXType type)
    {
        if (type.kind != CXTypeKind.CXType_ConstantArray &&
            type.kind != CXTypeKind.CXType_IncompleteArray)
        {
            var up = new UseCaseException($"Unexpected type for array '{type.kind}'.");
            throw up;
        }

        EnqueueNode(
            context,
            CKind.Array,
            CLocation.Null,
            parentNode,
            cursor,
            type,
            type,
            string.Empty,
            name);
    }

    private void VisitRecord(
        ExplorerContext context,
        ExplorerNode? parentNode,
        string name,
        CXType type)
    {
        if (type.kind != CXTypeKind.CXType_Record)
        {
            var up = new UseCaseException($"Unexpected type for record '{type.kind}'.");
            throw up;
        }

        var cursor = clang_getTypeDeclaration(type);
        var location = Location(context, cursor, type, true);
        EnqueueNode(
            context,
            CKind.Record,
            location,
            parentNode,
            cursor,
            type,
            type,
            string.Empty,
            name);
    }

    private void VisitOpaqueType(
        ExplorerContext context,
        ExplorerNode? parentNode,
        string name,
        CXCursor cursor,
        CXType type)
    {
        if (type.kind != CXTypeKind.CXType_Record)
        {
            var up = new UseCaseException($"Unexpected type for record '{type.kind}'.");
            throw up;
        }

        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(context, locationCursor, type, true);
        EnqueueNode(
            context,
            CKind.OpaqueType,
            location,
            parentNode,
            cursor,
            type,
            type,
            string.Empty,
            name);
    }

    private void VisitPrimitive(
        ExplorerContext context,
        ExplorerNode? parentNode,
        string name,
        CXType type)
    {
        if (!type.IsPrimitive())
        {
            var up = new UseCaseException($"Unexpected type for primitive '{type.kind}'.");
            throw up;
        }

        EnqueueNode(
            context,
            CKind.Primitive,
            CLocation.Null,
            parentNode,
            default,
            type,
            type,
            string.Empty,
            name);
    }

    private void VisitPointer(
        ExplorerContext context,
        ExplorerNode? parentNode,
        CXCursor cursor,
        CXType type)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            type = clang_Type_getModifiedType(type);
        }

        if (type.kind != CXTypeKind.CXType_Pointer)
        {
            var up = new UseCaseException($"Unexpected type for pointer '{type.kind}'.");
            throw up;
        }

        var pointeeTypeCandidate = clang_getPointeeType(type);
        var (pointeeKind, pointeeType) = TypeKind(context, pointeeTypeCandidate);
        var pointeeCursorCandidate = clang_getTypeDeclaration(pointeeType);
        var pointeeCursor = pointeeCursorCandidate.kind == CXCursorKind.CXCursor_NoDeclFound ? cursor : pointeeCursorCandidate;
        var pointeeTypeName = TypeName(pointeeType, parentNode?.TypeName!);
        var pointeeLocation = Location(context, pointeeCursor, pointeeType);
        VisitType(
            context,
            parentNode,
            pointeeCursor,
            pointeeType,
            pointeeTypeCandidate,
            pointeeTypeName,
            pointeeLocation,
            pointeeKind);
    }

    private void VisitMacro(ExplorerContext context, CXCursor cursor)
    {
        if (cursor.kind != CXCursorKind.CXCursor_MacroDefinition)
        {
            var up = new UseCaseException($"Unexpected cursor for macro '{cursor.kind}'.");
            throw up;
        }

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
            context,
            CKind.MacroDefinition,
            location,
            null,
            cursor,
            default,
            default,
            name,
            string.Empty);
    }

    private void VisitFunction(ExplorerContext context, CXCursor cursor)
    {
        if (cursor.kind != CXCursorKind.CXCursor_FunctionDecl)
        {
            var up = new UseCaseException($"Unexpected cursor for function '{cursor.kind}'.");
            throw up;
        }

        if (!context.Options.IsEnabledFunctions)
        {
            return;
        }

        var name = cursor.Name();

        if (context.VisitedFunctionNames.Contains(name))
        {
            // A header file may contain the declaration of the function and then later decide to implement it.
            return;
        }

        var isFunctionBlocked = !context.Options.FunctionNamesAllowed.IsDefaultOrEmpty &&
                                !context.Options.FunctionNamesAllowed.Contains(name);
        if (isFunctionBlocked)
        {
            return;
        }

        var type = clang_getCursorType(cursor);
        var location = Location(context, cursor, type);
        context.VisitedFunctionNames.Add(name);
        EnqueueNode(
            context,
            CKind.Function,
            location,
            null,
            cursor,
            type,
            type,
            name,
            string.Empty);
    }

    private void VisitVariable(ExplorerContext context, CXCursor cursor)
    {
        if (cursor.kind != CXCursorKind.CXCursor_VarDecl)
        {
            var up = new UseCaseException($"Unexpected cursor for variable '{cursor.kind}'.");
            throw up;
        }

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
        var typeName = TypeName(type, null);
        EnqueueNode(
            context,
            CKind.Variable,
            location,
            null,
            cursor,
            type,
            type,
            name,
            typeName);
    }

    private void VisitEnum(ExplorerContext context, ExplorerNode? parentNode, CXCursor cursor)
    {
        var type = clang_getCursorType(cursor);
        if (type.kind != CXTypeKind.CXType_Enum)
        {
            var up = new UseCaseException($"Unexpected type for enum '{type.kind}'.");
            throw up;
        }

        if (!context.Options.IsEnabledEnumsDangling && parentNode == null)
        {
            return;
        }

        var name = type.Name();
        var location = Location(context, cursor, default);
        EnqueueNode(
            context,
            CKind.Enum,
            location,
            parentNode,
            cursor,
            type,
            type,
            name,
            name);
    }

    private void VisitTypedef(
        ExplorerContext context,
        string name,
        ExplorerNode? parentNode,
        CXType type)
    {
        if (type.kind != CXTypeKind.CXType_Typedef)
        {
            var up = new UseCaseException($"Unexpected type for typedef '{type.kind}'.");
            throw up;
        }

        var typedefCursor = clang_getTypeDeclaration(type);
        var location = Location(context, typedefCursor, type);
        EnqueueNode(
            context,
            CKind.Typedef,
            location,
            parentNode,
            typedefCursor,
            type,
            type,
            string.Empty,
            name);
    }

    private void VisitFunctionPointer(
        ExplorerContext context,
        string name,
        string typeName,
        ExplorerNode? parentNode,
        CXCursor cursor,
        CXType type,
        CXType containerType,
        CLocation location)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            type = clang_Type_getModifiedType(type);
        }

        if (type.kind != CXTypeKind.CXType_FunctionProto &&
            type.kind != CXTypeKind.CXType_FunctionNoProto)
        {
            var up = new UseCaseException($"Unexpected type for function pointer '{type.kind}'.");
            throw up;
        }

        if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            var up = new UseCaseException($"Unexpected invalid cursor for function pointer.");
            throw up;
        }

        EnqueueNode(
            context,
            CKind.FunctionPointer,
            location,
            parentNode,
            cursor,
            type,
            containerType,
            name,
            typeName);
    }

    private string TypeName(CXType type, string? parentName, int index = 0)
    {
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

        if (cursor.kind != CXCursorKind.CXCursor_FunctionDecl &&
            type.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
        {
            return CLocation.Null;
        }

        if (type.kind is
            CXTypeKind.CXType_Pointer or
            CXTypeKind.CXType_ConstantArray or
            CXTypeKind.CXType_IncompleteArray)
        {
            return CLocation.Null;
        }

        if (type.IsPrimitive())
        {
            return CLocation.Null;
        }

        if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            var up = new UseCaseException("Expected a valid cursor when getting the location.");
            throw up;
        }

        if (drillDown)
        {
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
