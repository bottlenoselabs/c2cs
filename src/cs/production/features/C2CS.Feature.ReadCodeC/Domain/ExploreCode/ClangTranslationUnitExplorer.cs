// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Domain.ExploreCode.Diagnostics;
using C2CS.Foundation.UseCases.Exceptions;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

public sealed class ClangTranslationUnitExplorer
{
    private readonly ILogger _logger;

    public ClangTranslationUnitExplorer(ILogger logger)
    {
        _logger = logger;
    }

    public CAbstractSyntaxTree AbstractSyntaxTree(
        ClangTranslationUnitExplorerContext context, CXTranslationUnit translationUnit)
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
        ClangTranslationUnitExplorerContext context,
        CXTranslationUnit translationUnit)
    {
        var cursor = clang_getTranslationUnitCursor(translationUnit);
        var location = Location(context, cursor);

        var functions = context.Functions.ToImmutableArray();
        var functionPointers = context.FunctionPointers.ToImmutableArray();
        var records = context.Records.ToImmutableArray();
        var enums = context.Enums.ToImmutableArray();
        var opaqueTypes = context.OpaqueDataTypes.ToImmutableArray();
        var typedefs = context.Typedefs.ToImmutableArray();
        var variables = context.Variables.ToImmutableArray();
        var constants = context.MacroObjects.ToImmutableArray();

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
            Types = context.Types.ToImmutableArray(),
            Constants = constants
        };

        return result;
    }

    private void VisitTranslationUnit(ClangTranslationUnitExplorerContext context, CXTranslationUnit translationUnit)
    {
        var cursor = clang_getTranslationUnitCursor(translationUnit);

        var type = clang_getCursorType(cursor);
        var location = Location(context, cursor);

        _logger.ExploreCodeTranslationUnit(location.FileName);
        AddExplorerNode(
            context,
            CKind.TranslationUnit,
            location,
            null,
            cursor,
            type,
            string.Empty,
            string.Empty);
    }

    private void Explore(ClangTranslationUnitExplorerContext context)
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

    private void ExploreNode(ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.Kind)
        {
            case CKind.TranslationUnit:
                ExploreTranslationUnit(context, node);
                break;
            case CKind.Variable:
                ExploreVariable(context, node.Name!, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Function:
                ExploreFunction(context, node.Name!, node.Cursor, node.Type, node.Location, node.Parent!);
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
                    context, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Array:
                VisitArray(context, node);
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

    private bool IsIgnored(ClangTranslationUnitExplorerContext context, CXType type, CXCursor cursor)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            return false;
        }

        var (kind, actualType) = cursor.kind != CXCursorKind.CXCursor_MacroDefinition
            ? TypeKind(type)
            : (CKind.MacroDefinition, default);
        if (kind == CKind.Primitive)
        {
            return false;
        }

        if (kind == CKind.Array)
        {
            var elementType = clang_getElementType(actualType);
            return IsIgnored(context, elementType, cursor);
        }

        var fileLocation = kind == CKind.MacroDefinition ? Location(context, cursor) : Location(context, cursor, actualType);
        if (string.IsNullOrEmpty(fileLocation.FileName))
        {
            var up = new UseCaseException(
                "Unexpected null file path for a type/cursor combination; this is a bug.");
            throw up;
        }

        foreach (var includeDirectory in context.IncludeDirectories)
        {
            if (!fileLocation.FileName.Contains(includeDirectory, StringComparison.InvariantCulture))
            {
                continue;
            }

            fileLocation.FileName = fileLocation.FileName
                .Replace(includeDirectory, string.Empty, StringComparison.InvariantCulture).Trim('/', '\\');
            break;
        }

        return context.IgnoredFiles.Contains(fileLocation.FileName);
    }

    private void ExploreTranslationUnit(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node)
    {
        var interestingCursors = node.Cursor.GetDescendents(IsCursorOfInterest);
        foreach (var cursor in interestingCursors)
        {
            VisitTranslationUnitCursor(context, node, cursor);
        }

        static bool IsCursorOfInterest(CXCursor cursor, CXCursor cursorParent)
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
                if (clang_Cursor_isMacroBuiltin(cursor) != 0)
                {
                    return false;
                }

                var location = cursor.FileLocation();
                if (string.IsNullOrEmpty(location.FileName))
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

            var isSystemCursor = cursor.IsSystem();
            return !isSystemCursor;
        }
    }

    private void VisitTranslationUnitCursor(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode parentNode, CXCursor cursor)
    {
        var kind = cursor.kind switch
        {
            CXCursorKind.CXCursor_FunctionDecl => CKind.Function,
            CXCursorKind.CXCursor_VarDecl => CKind.Variable,
            CXCursorKind.CXCursor_EnumDecl => CKind.Enum,
            CXCursorKind.CXCursor_MacroDefinition => CKind.MacroDefinition,
            _ => CKind.Unknown
        };

        if (kind == CKind.Unknown)
        {
            var up = new UseCaseException(
                $"Expected 'FunctionDecl', 'VarDecl', or 'EnumDecl' but found '{cursor.kind}'.");
            throw up;
        }

        var name = cursor.Name();

        if (kind == CKind.MacroDefinition)
        {
            var location = Location(context, cursor);
            AddExplorerNode(context, kind, location, parentNode, cursor, default, name, string.Empty);
        }
        else
        {
            var type = clang_getCursorType(cursor);
            var location = Location(context, cursor, type);
            var typeName = TypeName(parentNode.TypeName!, kind, type, cursor);

            var isIgnored = IsIgnored(context, type, cursor);
            if (isIgnored)
            {
                return;
            }

            if (kind == CKind.Enum)
            {
                ExploreEnum(context, typeName, cursor, type, location, parentNode);
            }
            else
            {
                AddExplorerNode(context, kind, location, parentNode, cursor, type, name, typeName);
            }
        }
    }

    private void VisitArray(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node)
    {
        var elementType = clang_getElementType(node.Type);
        var (kind, type) = TypeKind(elementType);
        var typeCursor = clang_getTypeDeclaration(type);
        var cursor = typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound ? node.Cursor : typeCursor;
        var typeName = TypeName(node.TypeName!, kind, type, typeCursor);
        VisitType(context, node, cursor, node.Cursor, type, typeName);
    }

    private void ExplorePointer(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node)
    {
        var pointeeType = clang_getPointeeType(node.Type);
        var (kind, type) = TypeKind(pointeeType);
        var typeCursor = clang_getTypeDeclaration(type);
        var typeName = TypeName(node.TypeName!, kind, type, typeCursor);
        VisitType(context, node, typeCursor, typeCursor, type, typeName);
    }

    private void ExploreMacro(ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node)
    {
        var name = node.Name!;
        if (context.Names.Contains(name))
        {
            var diagnostic = new MacroAlreadyExistsDiagnostic(name);
            context.Diagnostics.Add(diagnostic);
            return;
        }

        var location = node.Location;

        // Function-like macros currently not implemented
        // https://github.com/lithiumtoast/c2cs/issues/35
        if (clang_Cursor_isMacroFunctionLike(node.Cursor) != 0)
        {
            context.MacroFunctionLikeNames.Add(name);
            return;
        }

        // Assume that macros with a name which starts with an underscore are not supposed to be exposed in the public API
        if (name.StartsWith("_", StringComparison.InvariantCulture))
        {
            return;
        }

        // Assume that macro ending with "API_DECL" are not interesting for bindgen
        if (name.EndsWith("API_DECL", StringComparison.InvariantCulture))
        {
            return;
        }

        // libclang doesn't have a thing where we can easily get a value of a macro
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
            ulong tokensCount = 0;

            clang_tokenize(translationUnit, range, &tokensC, &tokensCount);

            var macroIsFlag = tokensCount is 0 or 1;
            if (macroIsFlag)
            {
                clang_disposeTokens(translationUnit, tokensC, (uint)tokensCount);
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

            clang_disposeTokens(translationUnit, tokensC, (uint)tokensCount);
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
        if (tokens.Length == 1 && context.Names.Contains(tokens[0]))
        {
            return;
        }

        if (name == "C2CS_RUNTIME_TARGET_PLATFORM_NAME")
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

        var macro = new CMacroDefinition
        {
            Name = name,
            Tokens = tokens.ToImmutableArray(),
            Location = location
        };

        context.Names.Add(name);
        context.MacroObjects.Add(macro);
        _logger.ExploreCodeMacro(name);
    }

    private void ExploreVariable(
        ClangTranslationUnitExplorerContext context,
        string name,
        string typeName,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ClangTranslationUnitExplorerNode parentNode)
    {
        _logger.ExploreCodeVariable(name);

        VisitType(context, parentNode, cursor, cursor, type, typeName);

        var variable = new CVariable
        {
            Location = location,
            Name = name,
            Type = typeName
        };

        context.Variables.Add(variable);
        context.Names.Add(name);
    }

    private void ExploreFunction(
        ClangTranslationUnitExplorerContext context,
        string name,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ClangTranslationUnitExplorerNode parentNode)
    {
        if (!context.FunctionNamesWhitelist.IsEmpty && !context.FunctionNamesWhitelist.Contains(name))
        {
            return;
        }

        _logger.ExploreCodeFunction(name);

        var callingConvention = CreateFunctionCallingConvention(type);
        var resultType = clang_getCursorResultType(cursor);
        var (kind, actualType) = TypeKind(resultType);
        var resultTypeName = TypeName(parentNode.TypeName!, kind, actualType, cursor);

        VisitType(context, parentNode, cursor, cursor, resultType, resultTypeName);

        var functionParameters = CreateFunctionParameters(context, cursor, parentNode);

        var function = new CFunction
        {
            Name = name,
            Location = location,
            CallingConvention = callingConvention,
            ReturnType = resultTypeName,
            Parameters = functionParameters
        };

        context.Functions.Add(function);
        context.Names.Add(function.Name);
    }

    private void ExploreEnum(
        ClangTranslationUnitExplorerContext context,
        string typeName,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ClangTranslationUnitExplorerNode parentNode)
    {
        if (context.Names.Contains(typeName))
        {
            return;
        }

        _logger.ExploreCodeEnum(typeName);

        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            typeCursor = cursor;
        }

        var integerType = clang_getEnumDeclIntegerType(typeCursor);
        var integerTypeName = TypeName(parentNode.TypeName!, CKind.Enum, integerType, cursor);

        VisitType(context, parentNode, cursor, cursor, integerType, integerTypeName);

        var enumValues = CreateEnumValues(context, typeCursor);

        var @enum = new CEnum
        {
            Name = typeName,
            Location = location,
            Type = typeName,
            IntegerType = integerTypeName,
            Values = enumValues
        };

        context.Enums.Add(@enum);
        context.Names.Add(@enum.Name);
    }

    private void ExploreRecord(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode node, ClangTranslationUnitExplorerNode parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (context.OpaqueTypesNames.Contains(typeName))
        {
            ExploreOpaqueType(context, typeName, location);
            return;
        }

        _logger.ExploreCodeRecord(typeName);

        var cursor = node.Cursor;

        var fields = CreateRecordFields(context, typeName, cursor, parentNode);
        var nestedNodes = CreateNestedNodes(context, typeName, cursor, node);

        var nestedRecords = nestedNodes.Where(x => x is CRecord).Cast<CRecord>().ToImmutableArray();

        var typeCursor = clang_getTypeDeclaration(node.Type);
        var isUnion = typeCursor.kind == CXCursorKind.CXCursor_UnionDecl;

        var record = new CRecord
        {
            Location = location,
            IsUnion = isUnion,
            Name = typeName,
            Fields = fields,
            NestedRecords = nestedRecords
        };

        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        if (isAnonymous)
        {
            // if it's the case that the record's type is anonymous then it must be a struct/union without a name
            //  which is already inside another struct; thus, it will be properly stored in that parent struct and
            //  should not be added directly here
            return;
        }

        context.Records.Add(record);
        context.Names.Add(record.Name);
    }

    private void ExploreTypedef(
        ClangTranslationUnitExplorerContext context,
        ClangTranslationUnitExplorerNode node,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (context.OpaqueTypesNames.Contains(typeName))
        {
            ExploreOpaqueType(context, typeName, location);
            return;
        }

        var underlyingType = clang_getTypedefDeclUnderlyingType(node.Cursor);
        var (aliasKind, aliasType) = TypeKind(underlyingType);
        var aliasCursor = clang_getTypeDeclaration(aliasType);
        var cursor = aliasCursor.kind == CXCursorKind.CXCursor_NoDeclFound ? node.Cursor : aliasCursor;

        switch (aliasKind)
        {
            case CKind.Enum:
                ExploreEnum(context, typeName, cursor, aliasType, location, parentNode);
                return;
            case CKind.Record:
                ExploreRecord(context, node, parentNode);
                return;
            case CKind.FunctionPointer:
                ExploreFunctionPointer(context, typeName, cursor, aliasType, location, parentNode);
                return;
        }

        _logger.ExploreCodeTypedef(typeName);

        var aliasTypeName = TypeName(parentNode.TypeName!, aliasKind, aliasType, cursor);
        VisitType(context, parentNode, cursor, node.Cursor, aliasType, aliasTypeName);

        var typedef = new CTypedef
        {
            Name = typeName,
            Location = location,
            UnderlyingType = aliasTypeName
        };

        context.Typedefs.Add(typedef);
        context.Names.Add(typedef.Name);
    }

    private void ExploreOpaqueType(ClangTranslationUnitExplorerContext context, string typeName, CLocation location)
    {
        _logger.ExploreCodeOpaqueType(typeName);

        var opaqueDataType = new COpaqueType
        {
            Name = typeName,
            Location = location
        };

        context.OpaqueDataTypes.Add(opaqueDataType);
        context.Names.Add(opaqueDataType.Name);
    }

    private void ExploreFunctionPointer(
        ClangTranslationUnitExplorerContext context,
        string typeName,
        CXCursor cursor,
        CXType type,
        CLocation location,
        ClangTranslationUnitExplorerNode parentNode)
    {
        _logger.ExploreCodeFunctionPointer(typeName);

        if (type.kind == CXTypeKind.CXType_Pointer)
        {
            type = clang_getPointeeType(type);
        }

        var functionPointer = CreateFunctionPointer(context, typeName, cursor, parentNode, type, location);

        context.FunctionPointers.Add(functionPointer);
        context.Names.Add(functionPointer.Name);
    }

    private bool TypeNameIsValid(ClangTranslationUnitExplorerContext context, string typeName)
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

    private bool IsNewType(ClangTranslationUnitExplorerContext context, string typeName, CXType type, CXCursor cursor)
    {
        if (string.IsNullOrEmpty(typeName))
        {
            return false;
        }

        var alreadyVisited = context.TypesByName.TryGetValue(typeName, out var typeC);
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

            var typeKind = TypeKind(type);
            if (typeKind.Kind == CKind.OpaqueType)
            {
                return false;
            }

            typeC = Type(context, typeName, cursor, type);
            context.TypesByName[typeName] = typeC;
            return true;
        }

        typeC = Type(context, typeName, cursor, type);
        context.TypesByName.Add(typeName, typeC);
        context.Types.Add(typeC);

        return true;
    }

    private void AddExplorerNode(
        ClangTranslationUnitExplorerContext context,
        CKind kind,
        CLocation location,
        ClangTranslationUnitExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        string name,
        string typeName)
    {
        if (kind != CKind.TranslationUnit &&
            kind != CKind.MacroDefinition &&
            type.kind == CXTypeKind.CXType_Invalid)
        {
            var up = new UseCaseException("Explorer node can't be invalid type kind.");
            throw up;
        }

        var isIgnored = IsIgnored(context, type, cursor);
        if (isIgnored)
        {
            return;
        }

        var node = new ClangTranslationUnitExplorerNode(
            kind,
            location,
            parent,
            cursor,
            type,
            name,
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
            _ => throw new UseCaseException($"Unknown calling convention '{callingConvention}'.")
        };

        return result;
    }

    private ImmutableArray<CFunctionParameter> CreateFunctionParameters(
        ClangTranslationUnitExplorerContext context,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

        var parameterCursors = cursor.GetDescendents((child, _) =>
            child.kind == CXCursorKind.CXCursor_ParmDecl);

        foreach (var parameterCursor in parameterCursors)
        {
            var functionExternParameter = FunctionParameter(context, parameterCursor, parentNode);
            builder.Add(functionExternParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionParameter FunctionParameter(
        ClangTranslationUnitExplorerContext context, CXCursor cursor, ClangTranslationUnitExplorerNode parentNode)
    {
        var type = clang_getCursorType(cursor);
        var name = cursor.Name();

        var (kind, typeActual) = TypeKind(type);
        var typeName = TypeName(parentNode.TypeName!, kind, typeActual, cursor);

        VisitType(context, parentNode, cursor, cursor, type, typeName);
        var codeLocation = Location(context, cursor, type);

        return new CFunctionParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeName
        };
    }

    private CFunctionPointer CreateFunctionPointer(
        ClangTranslationUnitExplorerContext context,
        string typeName,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode,
        CXType type,
        CLocation location)
    {
        var functionPointerParameters = CreateFunctionPointerParameters(context, cursor, parentNode);

        var returnType = clang_getResultType(type);
        var (kind, actualReturnType) = TypeKind(returnType);
        var returnTypeName = TypeName(parentNode.TypeName!, kind, actualReturnType, cursor);
        VisitType(context, parentNode, cursor, cursor, returnType, returnTypeName);

        var name = string.Empty;
        if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
        {
            name = cursor.Name();
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var pointeeType = clang_getPointeeType(underlyingType);
            var functionProtoType = pointeeType.kind == CXTypeKind.CXType_Invalid ? underlyingType : pointeeType;
            typeName = TypeName(parentNode.TypeName!, CKind.FunctionPointer, functionProtoType, cursor);
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
        ClangTranslationUnitExplorerContext context,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionPointerParameter>();

        var parameterCursors = cursor.GetDescendents((child, _) =>
            child.kind == CXCursorKind.CXCursor_ParmDecl);

        foreach (var parameterCursor in parameterCursors)
        {
            var functionPointerParameter = CreateFunctionPointerParameter(context, parameterCursor, parentNode);
            builder.Add(functionPointerParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionPointerParameter CreateFunctionPointerParameter(
        ClangTranslationUnitExplorerContext context,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var type = clang_getCursorType(cursor);
        var codeLocation = Location(context, cursor, type);
        var name = cursor.Name();

        var (kind, actualType) = TypeKind(type);
        var typeName = TypeName(parentNode.TypeName!, kind, actualType, cursor);

        VisitType(context, parentNode, cursor, cursor, type, typeName);

        return new CFunctionPointerParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeName
        };
    }

    private ImmutableArray<CRecordField> CreateRecordFields(
        ClangTranslationUnitExplorerContext context,
        string recordName,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CRecordField>();

        var underlyingCursor = ClangUnderlyingCursor(cursor);

        var type = clang_getCursorType(underlyingCursor);
        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind == CXCursorKind.CXCursor_UnionDecl ||
            typeCursor.kind == CXCursorKind.CXCursor_StructDecl)
        {
            underlyingCursor = typeCursor;
        }

        var fieldCursors = underlyingCursor.GetDescendents((child, _) =>
            child.kind == CXCursorKind.CXCursor_FieldDecl);

        foreach (var fieldCursor in fieldCursors)
        {
            var recordField = CreateRecordField(context, recordName, fieldCursor, parentNode);
            builder.Add(recordField);
        }

        CalculatePaddingForStructFields(context, cursor, builder);

        var result = builder.ToImmutable();
        return result;
    }

    private void CalculatePaddingForStructFields(
        ClangTranslationUnitExplorerContext context,
        CXCursor cursor,
        ImmutableArray<CRecordField>.Builder builder)
    {
        for (var i = 1; i < builder.Count; i++)
        {
            var recordField = builder[i];
            var fieldPrevious = builder[i - 1];
            var typeName = Value(fieldPrevious.Type);
            var type = context.TypesByName[typeName];
            var fieldPreviousTypeSizeOf = type.SizeOf;
            var expectedFieldOffset = fieldPrevious.Offset + fieldPreviousTypeSizeOf;
            var hasPadding = recordField.Offset != 0 && recordField.Offset != expectedFieldOffset;
            if (!hasPadding)
            {
                continue;
            }

            var padding = recordField.Offset - expectedFieldOffset;
            builder[i - 1].Padding = padding;
        }

        if (builder.Count >= 1)
        {
            var fieldLast = builder[^1];
            var cursorType = clang_getCursorType(cursor);
            var recordSize = (int)clang_Type_getSizeOf(cursorType);
            var typeName = Value(fieldLast.Type);
            var type = context.TypesByName[typeName];
            var fieldLastTypeSize = type.SizeOf;
            var expectedLastFieldOffset = recordSize - fieldLastTypeSize;
            if (fieldLast.Offset != expectedLastFieldOffset)
            {
                var padding = expectedLastFieldOffset - fieldLast.Offset;
                builder[^1].Padding = padding;
            }
        }
    }

    private string Value(string typeName)
    {
        if (typeName.EndsWith("*", StringComparison.InvariantCulture))
        {
            var pointeeTypeName = typeName.TrimEnd('*');
            var result2 = Value(pointeeTypeName);
            return typeName.Replace(pointeeTypeName, result2, StringComparison.InvariantCulture);
        }

        return typeName;
    }

    private CRecordField CreateRecordField(
        ClangTranslationUnitExplorerContext context,
        string recordName,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var name = cursor.Name();
        var type = clang_getCursorType(cursor);
        var codeLocation = Location(context, cursor, type);
        var (kind, actualType) = TypeKind(type);
        var typeName = TypeName(recordName, kind, actualType, cursor);

        VisitType(context, parentNode, cursor, cursor, type, typeName);

        var offset = (int)(clang_Cursor_getOffsetOfField(cursor) / 8);

        return new CRecordField
        {
            Name = name,
            Location = codeLocation,
            Type = typeName,
            Offset = offset
        };
    }

    private ImmutableArray<CNode> CreateNestedNodes(
        ClangTranslationUnitExplorerContext context,
        string parentTypeName,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CNode>();

        var underlyingCursor = ClangUnderlyingCursor(cursor);
        var type = clang_getCursorType(underlyingCursor);
        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind == CXCursorKind.CXCursor_UnionDecl ||
            typeCursor.kind == CXCursorKind.CXCursor_StructDecl)
        {
            underlyingCursor = typeCursor;
        }

        var nestedCursors = underlyingCursor.GetDescendents((child, _) =>
        {
            if (child.kind != CXCursorKind.CXCursor_FieldDecl)
            {
                return false;
            }

            var type = clang_getCursorType(child);
            var typeDeclaration = clang_getTypeDeclaration(type);
            var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;
            if (isAnonymous)
            {
                return true;
            }

            return false;
        });

        foreach (var nestedCursor in nestedCursors)
        {
            var record = CreateNestedNode(context, parentTypeName, nestedCursor, parentNode);
            builder.Add(record);
        }

        if (builder.Count == 0)
        {
            return ImmutableArray<CNode>.Empty;
        }

        return builder.ToImmutable();
    }

    private CNode CreateNestedNode(
        ClangTranslationUnitExplorerContext context,
        string parentTypeName,
        CXCursor cursor,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var type = clang_getCursorType(cursor);
        var isPointer = type.kind == CXTypeKind.CXType_Pointer;
        if (!isPointer)
        {
            return CreateNestedStruct(context, parentTypeName, cursor, type, parentNode);
        }

        var pointeeType = clang_getPointeeType(type);
        if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
        {
            var typeName = TypeName(parentTypeName, CKind.FunctionPointer, pointeeType, cursor);
            var location = Location(context, cursor, type);
            return CreateFunctionPointer(context, typeName, cursor, parentNode, pointeeType, location);
        }

        var up = new UseCaseException("Unknown mapping for nested node.");
        throw up;
    }

    private CNode CreateNestedStruct(
        ClangTranslationUnitExplorerContext context,
        string parentTypeName,
        CXCursor cursor,
        CXType type,
        ClangTranslationUnitExplorerNode parentNode)
    {
        var location = Location(context, cursor, type);
        var typeName = TypeName(parentTypeName, CKind.Record, type, cursor);

        var recordFields = CreateRecordFields(context, typeName, cursor, parentNode);
        var nestedNodes = CreateNestedNodes(context, typeName, cursor, parentNode);
        var nestedRecords = nestedNodes.Where(x => x is CRecord).Cast<CRecord>().ToImmutableArray();

        var typeCursor = clang_getTypeDeclaration(type);
        var isUnion = typeCursor.kind == CXCursorKind.CXCursor_UnionDecl;

        return new CRecord
        {
            Location = location,
            IsUnion = isUnion,
            Name = typeName,
            Fields = recordFields,
            NestedRecords = nestedRecords
        };
    }

    private ImmutableArray<CEnumValue> CreateEnumValues(ClangTranslationUnitExplorerContext context, CXCursor cursor)
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

    private CType Type(ClangTranslationUnitExplorerContext context, string typeName, CXCursor cursor, CXType clangType)
    {
        var type = clangType;
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
        var isSystemType = type.IsSystem();

        var (kind, kindType) = TypeKind(type);

        int? elementSize = null;
        if (kind == CKind.Array)
        {
            var (_, arrayType) = TypeKind(kindType);
            var elementType = clang_getElementType(arrayType);
            elementSize = (int)clang_Type_getSizeOf(elementType);
        }

        var location = Location(context, declaration, type);

        var cType = new CType
        {
            Name = typeName,
            Kind = kind,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ElementSize = elementSize,
            ArraySize = arraySize,
            Location = location
        };

        var fileName = location.FileName;
        if (context.IgnoredFiles.Contains(fileName))
        {
            var diagnostic = new TypeFromIgnoredHeaderDiagnostic(typeName, fileName);
            context.Diagnostics.Add(diagnostic);
        }

        return cType;
    }

    private int SizeOf(ClangTranslationUnitExplorerContext context, CXType type)
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

        var (kind, underlyingType) = TypeKind(type);
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

    private static (CKind Kind, CXType Type) TypeKind(CXType type)
    {
        CXType cursorType;
        var cursor = clang_getTypeDeclaration(type);
        if (cursor.kind != CXCursorKind.CXCursor_NoDeclFound)
        {
            cursorType = clang_getCursorType(cursor);
        }
        else
        {
            cursorType = type;
        }

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
                else
                {
                    var alias = TypeKind(underlyingType);
                    var sizeOfAlias = clang_Type_getSizeOf(alias.Type);
                    return sizeOfAlias == -2 ? (CKind.OpaqueType, cursorType) : (CKind.Typedef, cursorType);
                }

            case CXTypeKind.CXType_FunctionNoProto:
                return (CKind.Function, cursorType);
            case CXTypeKind.CXType_FunctionProto:
                return (CKind.FunctionPointer, cursorType);
            case CXTypeKind.CXType_Pointer:
                var pointeeType = clang_getPointeeType(cursorType);
                if (pointeeType.kind == CXTypeKind.CXType_Attributed)
                {
                    pointeeType = clang_Type_getModifiedType(pointeeType);
                }

                if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                {
                    return (CKind.FunctionPointer, pointeeType);
                }

                return (CKind.Pointer, cursorType);

            case CXTypeKind.CXType_Attributed:
                var modifiedType = clang_Type_getModifiedType(cursorType);
                return TypeKind(modifiedType);
            case CXTypeKind.CXType_Elaborated:
                var namedType = clang_Type_getNamedType(cursorType);
                return TypeKind(namedType);
            case CXTypeKind.CXType_ConstantArray:
            case CXTypeKind.CXType_IncompleteArray:
                return (CKind.Array, cursorType);
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
        ClangTranslationUnitExplorerContext context,
        ClangTranslationUnitExplorerNode parentNode,
        CXCursor cursor,
        CXCursor originalCursor,
        CXType type,
        string typeName)
    {
        if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            throw new UseCaseException("NoDecl cursor.");
        }

        if (!IsNewType(context, typeName, type, cursor))
        {
            return;
        }

        var isValidTypeName = TypeNameIsValid(context, typeName);
        if (!isValidTypeName)
        {
            return;
        }

        var (kind, kindType) = TypeKind(type);
        if (kind == CKind.Typedef)
        {
            VisitTypedef(context, parentNode, kindType, typeName);
            return;
        }

        _logger.ExploreCodeVisitType(typeName);

        if (kind == CKind.Pointer)
        {
            var pointeeType = clang_getPointeeType(kindType);
            var (pointeeKind, pointeeKindType) = TypeKind(pointeeType);
            var pointeeCursor = clang_getTypeDeclaration(pointeeType);
            var pointeeCursor2 = pointeeCursor.kind == CXCursorKind.CXCursor_NoDeclFound ? cursor : pointeeCursor;
            var pointeeTypeName = TypeName(parentNode.TypeName!, pointeeKind, pointeeKindType, pointeeCursor2);
            VisitType(
                context,
                parentNode,
                pointeeCursor2,
                originalCursor,
                pointeeKindType,
                pointeeTypeName);
        }
        else
        {
            var locationType = type.kind switch
            {
                CXTypeKind.CXType_IncompleteArray or CXTypeKind.CXType_ConstantArray => clang_getElementType(type),
                CXTypeKind.CXType_Pointer => clang_getPointeeType(type),
                _ => type
            };

            var locationCursor = clang_getTypeDeclaration(locationType);
            if (locationCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                locationCursor = cursor;
            }

            if (locationCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                locationCursor = originalCursor;
            }

            var location = Location(context, locationCursor);
            AddExplorerNode(
                context,
                kind,
                location,
                parentNode,
                cursor,
                kindType,
                string.Empty,
                typeName);
        }
    }

    private void VisitTypedef(
        ClangTranslationUnitExplorerContext context, ClangTranslationUnitExplorerNode parentNode, CXType type, string typeName)
    {
        var typedefCursor = clang_getTypeDeclaration(type);
        var location = Location(context, typedefCursor);
        AddExplorerNode(context, CKind.Typedef, location, parentNode, typedefCursor, type, string.Empty, typeName);
    }

    private string TypeName(string? parentTypeName, CKind kind, CXType type, CXCursor cursor)
    {
        var typeCursor = clang_getTypeDeclaration(type);
        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) != 0;
        if (isAnonymous && !string.IsNullOrEmpty(parentTypeName))
        {
            var anonymousCursor = clang_getTypeDeclaration(type);
            var cursorName = cursor.Name();

            switch (anonymousCursor.kind)
            {
                case CXCursorKind.CXCursor_UnionDecl:
                    return $"{parentTypeName}_{cursorName}";
                case CXCursorKind.CXCursor_StructDecl:
                    return $"{parentTypeName}_{cursorName}";
                default:
                {
                    // pretty sure this case is not possible, but it's better safe than sorry!
                    var up = new UseCaseException($"Unknown anonymous cursor kind '{anonymousCursor.kind}'");
                    throw up;
                }
            }
        }

        var typeName = kind switch
        {
            CKind.Primitive => type.Name(),
            CKind.Pointer => type.Name(),
            CKind.Array => type.Name(),
            CKind.Variable => type.Name(),
            CKind.Function => cursor.Name(),
            CKind.FunctionPointer => type.Name(),
            CKind.Typedef => type.Name(),
            CKind.Record => type.Name(),
            CKind.Enum => type.Name(),
            CKind.OpaqueType => type.Name(),
            _ => throw new UseCaseException($"Unexpected node kind '{kind}'.")
        };

        if (type.kind == CXTypeKind.CXType_ConstantArray)
        {
            var arraySize = clang_getArraySize(type);
            typeName = $"{typeName}[{arraySize}]";
        }

        if (string.IsNullOrEmpty(typeName))
        {
            throw new UseCaseException($"Type name was not found for '{kind}'.");
        }

        return typeName;
    }

    private CLocation Location(ClangTranslationUnitExplorerContext context, CXCursor cursor, CXType? type = null)
    {
        CLocation location;
        if (type == null)
        {
            location = cursor.FileLocation();
        }
        else
        {
            location = type.Value.FileLocation(cursor);
            if (string.IsNullOrEmpty(location.FileName))
            {
                location = cursor.FileLocation();
            }
        }

        if (!string.IsNullOrEmpty(location.FilePath))
        {
            foreach (var includeDirectory in context.IncludeDirectories)
            {
                if (location.FilePath.Contains(includeDirectory, StringComparison.InvariantCulture))
                {
                    location.FilePath = location.FilePath
                        .Replace(includeDirectory, string.Empty, StringComparison.InvariantCulture).Trim('/', '\\');
                    break;
                }
            }
        }

        return location;
    }
}
