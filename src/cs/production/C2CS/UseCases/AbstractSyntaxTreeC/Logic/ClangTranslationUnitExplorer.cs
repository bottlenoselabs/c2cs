// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using DSA;
using static clang;

namespace C2CS.UseCases.AbstractSyntaxTreeC;

public class ClangTranslationUnitExplorer
{
    private readonly ArrayDeque<Node> _frontierGeneral = new();
    private readonly ArrayDeque<Node> _frontierMacros = new();

    private readonly DiagnosticsSink _diagnostics;
    private readonly ImmutableHashSet<string> _ignoredFiles;
    private readonly ImmutableHashSet<string> _opaqueTypeNames;
    private readonly ImmutableArray<string> _includeDirectories;
    private readonly Dictionary<string, bool> _validTypeNames = new();

    private readonly HashSet<string> _names = new();
    private readonly Dictionary<string, CType> _typesByName = new();
    private readonly List<CType> _types = new();
    private readonly List<CVariable> _variables = new();
    private readonly List<CFunction> _functions = new();
    private readonly List<CEnum> _enums = new();
    private readonly List<CEnum> _pseudoEnums = new();
    private readonly List<CRecord> _records = new();
    private readonly List<COpaqueType> _opaqueDataTypes = new();
    private readonly List<CTypedef> _typedefs = new();
    private readonly List<CFunctionPointer> _functionPointers = new();

    private readonly List<CMacroDefinition> _macroObjects = new();
    private readonly HashSet<string> _macroFunctionLikeNames = new();

    private readonly ImmutableHashSet<string> _whitelistFunctionNames;

    private readonly HashSet<string> _systemIgnoredTypeNames = new()
    {
        "FILE",
        "DIR",
        "size_t",
        "ssize_t",
        "int8_t",
        "uint8_t",
        "int16_t",
        "uint16_t",
        "int32_t",
        "uint32_t",
        "int64_t",
        "uint64_t",
        "uintptr_t",
        "intptr_t",
        "va_list"
    };

    private readonly HashSet<string> _ignoredMacroTokens = new()
    {
        // print conversion specifiers
        "PRId8",
        "PRId16",
        "PRId32",
        "PRId64",
        "PRIdFAST8",
        "PRIdFAST16",
        "PRIdFAST32",
        "PRIdFAST64",
        "PRIdLEAST8",
        "PRIdLEAST16",
        "PRIdLEAST32",
        "PRIdLEAST64",
        "PRIdMAX",
        "PRIdPTR",
        "PRIi8",
        "PRIi16",
        "PRIi32",
        "PRIi64",
        "PRIiFAST8",
        "PRIiFAST16",
        "PRIiFAST32",
        "PRIiFAST64",
        "PRIiLEAST8",
        "PRIiLEAST16",
        "PRIiLEAST32",
        "PRIiLEAST64",
        "PRIiMAX",
        "PRIiPTR",
        "PRIo8",
        "PRIo16",
        "PRIo32",
        "PRIo64",
        "PRIoFAST8",
        "PRIoFAST16",
        "PRIoFAST32",
        "PRIoFAST64",
        "PRIoLEAST8",
        "PRIoLEAST16",
        "PRIoLEAST32",
        "PRIoLEAST64",
        "PRIoMAX",
        "PRIoPTR",
        "PRIu8",
        "PRIu16",
        "PRIu32",
        "PRIu64",
        "PRIuFAST8",
        "PRIuFAST16",
        "PRIuFAST32",
        "PRIuFAST64",
        "PRIuLEAST8",
        "PRIuLEAST16",
        "PRIuLEAST32",
        "PRIuLEAST64",
        "PRIuMAX",
        "PRIuPTR",
        "PRIx8",
        "PRIx16",
        "PRIx32",
        "PRIx64",
        "PRIxFAST8",
        "PRIxFAST16",
        "PRIxFAST32",
        "PRIxFAST64",
        "PRIxLEAST8",
        "PRIxLEAST16",
        "PRIxLEAST32",
        "PRIxLEAST64",
        "PRIxMAX",
        "PRIxPTR",
        "PRIX8",
        "PRIX16",
        "PRIX32",
        "PRIX64",
        "PRIXFAST8",
        "PRIXFAST16",
        "PRIXFAST32",
        "PRIXFAST64",
        "PRIXLEAST8",
        "PRIXLEAST16",
        "PRIXLEAST32",
        "PRIXLEAST64",
        "PRIXMAX",
        "PRIXPTR",
    };

    public ClangTranslationUnitExplorer(
        DiagnosticsSink diagnostics,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> ignoredFiles,
        ImmutableArray<string> opaqueTypes,
        ImmutableArray<string> whitelistFunctionNames)
    {
        _diagnostics = diagnostics;
        _ignoredFiles = ignoredFiles.ToImmutableHashSet();
        _includeDirectories = includeDirectories;
        _opaqueTypeNames = opaqueTypes.ToImmutableHashSet();
        _whitelistFunctionNames = whitelistFunctionNames.ToImmutableHashSet();
    }

    public CAbstractSyntaxTree AbstractSyntaxTree(CXTranslationUnit translationUnit, int bitness)
    {
        VisitTranslationUnit(translationUnit);
        Explore();

        var cursor = clang_getTranslationUnitCursor(translationUnit);
        var location = Location(cursor);

        var functions = _functions.ToImmutableArray();
        var functionPointers = _functionPointers.ToImmutableArray();
        var records = _records.ToImmutableArray();
        var enums = _enums.ToImmutableArray();
        var opaqueTypes = _opaqueDataTypes.ToImmutableArray();
        var typedefs = _typedefs.ToImmutableArray();
        var variables = _variables.ToImmutableArray();
        var constants = _macroObjects.ToImmutableArray();

        var pseudoEnums = new List<CEnum>();
        var enumNames = _enums.Select(x => x.Name).ToImmutableHashSet();
        foreach (var pseudoEnum in _pseudoEnums)
        {
            if (!enumNames.Contains(pseudoEnum.Name))
            {
                pseudoEnums.Add(pseudoEnum);
            }
        }

        return new CAbstractSyntaxTree
        {
            FileName = location.FileName,
            Bitness = bitness,
            Functions = functions,
            FunctionPointers = functionPointers,
            Records = records,
            Enums = enums,
            PseudoEnums = pseudoEnums.ToImmutableArray(),
            OpaqueTypes = opaqueTypes,
            Typedefs = typedefs,
            Variables = variables,
            Types = _types.ToImmutableArray(),
            Constants = constants
        };
    }

    private void VisitTranslationUnit(CXTranslationUnit translationUnit)
    {
        var cursor = clang_getTranslationUnitCursor(translationUnit);

        var type = clang_getCursorType(cursor);
        var location = Location(cursor);
        AddExplorerNode(
            CKind.TranslationUnit,
            location,
            null,
            cursor,
            type,
            type,
            string.Empty,
            string.Empty);
    }

    private void Explore()
    {
        while (_frontierGeneral.Count > 0)
        {
            var node = _frontierGeneral.PopFront()!;
            ExploreNode(node);
        }

        while (_frontierMacros.Count > 0)
        {
            var node = _frontierMacros.PopFront()!;
            ExploreNode(node);
        }
    }

    private void ExploreNode(Node node)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (node.Kind)
        {
            case CKind.TranslationUnit:
                ExploreTranslationUnit(node);
                break;
            case CKind.Variable:
                ExploreVariable(node.Name!, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Function:
                ExploreFunction(node.Name!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Typedef:
                ExploreTypedef(node, node.Parent!);
                break;
            case CKind.OpaqueType:
                ExploreOpaqueType(node.TypeName!, node.Location);
                break;
            case CKind.Enum:
                ExploreEnum(node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                break;
            case CKind.Record:
                ExploreRecord(node, node.Parent!);
                break;
            case CKind.FunctionPointer:
                ExploreFunctionPointer(node.TypeName!, node.Cursor, node.Type, node.OriginalType, node.Location, node.Parent!);
                break;
            case CKind.Array:
                ExploreArray(node);
                break;
            case CKind.Pointer:
                ExplorePointer(node);
                break;
            case CKind.Primitive:
                break;
            case CKind.MacroDefinition:
                ExploreMacro(node);
                break;
            default:
                var up = new ClangExplorerException($"Unexpected explorer node '{node.Kind}'.");
                throw up;
        }
    }

    private bool IsIgnored(CXType type, CXCursor cursor)
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
            return IsIgnored(elementType, cursor);
        }

        var fileLocation = kind == CKind.MacroDefinition ? cursor.FileLocation() : actualType.FileLocation(cursor);
        if (string.IsNullOrEmpty(fileLocation.FileName))
        {
            var up = new ClangExplorerException(
                "Unexpected null file path for a type/cursor combination; this is a bug.");
            throw up;
        }

        foreach (var includeDirectory in _includeDirectories)
        {
            if (!fileLocation.FileName.Contains(includeDirectory))
            {
                continue;
            }

            fileLocation.FileName = fileLocation.FileName.Replace(includeDirectory, string.Empty).Trim('/', '\\');
            break;
        }

        return _ignoredFiles.Contains(fileLocation.FileName);
    }

    private void ExploreTranslationUnit(Node node)
    {
        var interestingCursors = node.Cursor.GetDescendents(IsCursorOfInterest);
        foreach (var cursor in interestingCursors)
        {
            if (!IsWhitelisted(cursor, _whitelistFunctionNames))
            {
                continue;
            }

            VisitTranslationUnitCursor(node, cursor);
        }

        static bool IsWhitelisted(CXCursor cursor, ImmutableHashSet<string> whitelistFunctionNames)
        {
            if (whitelistFunctionNames.IsEmpty)
            {
                return true;
            }

            if (cursor.kind != CXCursorKind.CXCursor_FunctionDecl)
            {
                return true;
            }

            var functionName = cursor.Name();
            var whitelisted = whitelistFunctionNames.Contains(functionName);
            return whitelisted;
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

    private void VisitTranslationUnitCursor(Node parentNode, CXCursor cursor)
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
            var up = new ClangExplorerException(
                $"Expected 'FunctionDecl', 'VarDecl', or 'EnumDecl' but found '{cursor.kind}'.");
            throw up;
        }

        var name = cursor.Name();

        if (kind == CKind.MacroDefinition)
        {
            var location = Location(cursor);
            AddExplorerNode(kind, location, parentNode, cursor, default, default, name, string.Empty);
        }
        else
        {
            var type = clang_getCursorType(cursor);
            var location = Location(cursor, type);
            var typeName = TypeName(parentNode.TypeName!, kind, type, cursor);

            var isIgnored = IsIgnored(type, cursor);
            if (isIgnored)
            {
                return;
            }

            if (kind == CKind.Enum)
            {
                ExploreEnum(typeName, cursor, type, location, parentNode, true);
            }
            else
            {
                AddExplorerNode(kind, location, parentNode, cursor, type, type, name, typeName);
            }
        }
    }

    private void ExploreArray(Node node)
    {
        var elementType = clang_getElementType(node.Type);
        var (kind, type) = TypeKind(elementType);
        var typeCursor = clang_getTypeDeclaration(type);
        var typeName = TypeName(node.TypeName!, kind, type, typeCursor);
        VisitType(node, typeCursor, typeCursor, type, type, typeName);
    }

    private void ExplorePointer(Node node)
    {
        var pointeeType = clang_getPointeeType(node.Type);
        var (kind, type) = TypeKind(pointeeType);
        var typeCursor = clang_getTypeDeclaration(type);
        var typeName = TypeName(node.TypeName!, kind, type, typeCursor);
        VisitType(node, typeCursor, typeCursor, type, type, typeName);
    }

    private void ExploreMacro(Node node)
    {
        var name = node.Name!;
        var location = node.Location;

        // Function-like macros currently not implemented
        // https://github.com/lithiumtoast/c2cs/issues/35
        if (clang_Cursor_isMacroFunctionLike(node.Cursor) != 0)
        {
            _macroFunctionLikeNames.Add(name);
            return;
        }

        // It is assumed that macros with a name which starts with an underscore are not supposed to be exposed in the public API
        if (name.StartsWith("_", StringComparison.InvariantCulture))
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
            if (_macroFunctionLikeNames.Contains(token))
            {
                return;
            }

            if (_ignoredMacroTokens.Contains(token))
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
        if (tokens.Length == 1 && _names.Contains(tokens[0]))
        {
            return;
        }

        var macro = new CMacroDefinition
        {
            Name = name,
            Tokens = tokens.ToImmutableArray(),
            Location = location
        };

        _macroObjects.Add(macro);
    }

    private void ExploreVariable(
        string name,
        string typeName,
        CXCursor cursor,
        CXType type,
        ClangLocation location,
        Node parentNode)
    {
        VisitType(parentNode, cursor, cursor, type, type, typeName);

        var variable = new CVariable
        {
            Location = location,
            Name = name,
            Type = typeName
        };

        _variables.Add(variable);
        _names.Add(name);
    }

    private void ExploreFunction(string name, CXCursor cursor, CXType type, ClangLocation location, Node parentNode)
    {
        var callingConvention = CreateFunctionCallingConvention(type);
        var resultType = clang_getCursorResultType(cursor);
        var (kind, actualType) = TypeKind(resultType);
        var resultTypeName = TypeName(parentNode.TypeName!, kind, actualType, cursor);

        VisitType(parentNode, cursor, cursor, resultType, resultType, resultTypeName);

        var parameters = CreateFunctionParameters(cursor, parentNode);

        var function = new CFunction
        {
            Name = name,
            Location = location,
            CallingConvention = callingConvention,
            ReturnType = resultTypeName,
            Parameters = parameters
        };

        _functions.Add(function);
        _names.Add(function.Name);
    }

    private void ExploreEnum(
        string typeName,
        CXCursor cursor,
        CXType type,
        ClangLocation location,
        Node parentNode,
        bool isPseudo = false)
    {
        var typeCursor = clang_getTypeDeclaration(type);
        if (typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            typeCursor = cursor;
        }

        var integerType = clang_getEnumDeclIntegerType(typeCursor);
        var integerTypeName = TypeName(parentNode.TypeName!, CKind.Enum, integerType, cursor);

        VisitType(parentNode, cursor, cursor, integerType, integerType, integerTypeName);

        var enumValues = CreateEnumValues(typeCursor);

        var @enum = new CEnum
        {
            Name = typeName,
            Location = location,
            Type = typeName,
            IntegerType = integerTypeName,
            Values = enumValues
        };

        if (isPseudo)
        {
            _pseudoEnums.Add(@enum);
        }
        else
        {
            _enums.Add(@enum);
        }

        _names.Add(@enum.Name);
    }

    private void ExploreRecord(Node node, Node parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (_opaqueTypeNames.Contains(typeName))
        {
            ExploreOpaqueType(typeName, location);
            return;
        }

        var cursor = node.Cursor;

        var fields = CreateRecordFields(typeName, cursor, parentNode);
        var nestedNodes = CreateNestedNodes(typeName, cursor, node);

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

        _records.Add(record);
        _names.Add(record.Name);
    }

    private void ExploreTypedef(Node node, Node parentNode)
    {
        var typeName = node.TypeName!;
        var location = node.Location;

        if (_opaqueTypeNames.Contains(typeName))
        {
            ExploreOpaqueType(typeName, location);
            return;
        }

        var cursor = node.Cursor;
        var type = node.Type;
        var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
        var (aliasKind, aliasType) = TypeKind(underlyingType);
        var aliasCursor = clang_getTypeDeclaration(aliasType);

        if (aliasKind == CKind.Enum)
        {
            ExploreEnum(typeName, aliasCursor, aliasType, location, parentNode);
            return;
        }

        if (aliasKind == CKind.Record)
        {
            ExploreRecord(node, parentNode);
            return;
        }

        if (aliasKind == CKind.FunctionPointer)
        {
            if (aliasCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                aliasCursor = cursor;
            }

            ExploreFunctionPointer(typeName, aliasCursor, aliasType, type, location, parentNode);
            return;
        }

        var aliasTypeName = TypeName(parentNode.TypeName!, aliasKind, aliasType, aliasCursor);
        VisitType(parentNode, aliasCursor, cursor, aliasType, aliasType, aliasTypeName);

        var typedef = new CTypedef
        {
            Name = typeName,
            Location = location,
            UnderlyingType = aliasTypeName
        };

        _typedefs.Add(typedef);
        _names.Add(typedef.Name);
    }

    private void ExploreOpaqueType(string typeName, ClangLocation location)
    {
        var opaqueDataType = new COpaqueType
        {
            Name = typeName,
            Location = location
        };

        _opaqueDataTypes.Add(opaqueDataType);
        _names.Add(opaqueDataType.Name);
    }

    private void ExploreFunctionPointer(
        string typeName,
        CXCursor cursor,
        CXType type,
        CXType originalType,
        ClangLocation location,
        Node parentNode)
    {
        if (type.kind == CXTypeKind.CXType_Pointer)
        {
            type = clang_getPointeeType(type);
        }

        var functionPointer = CreateFunctionPointer(typeName, cursor, parentNode, originalType, type, location);

        _functionPointers.Add(functionPointer);
        _names.Add(functionPointer.Name);
    }

    private bool TypeNameIsValid(CXType type, string typeName)
    {
        if (_validTypeNames.TryGetValue(typeName, out var value))
        {
            return value;
        }

        var isSystem = type.IsSystem();
        var isIgnored = _systemIgnoredTypeNames.Contains(typeName);
        if (isSystem && isIgnored)
        {
            value = false;
        }
        else
        {
            value = true;
        }

        _validTypeNames.Add(typeName, value);
        return value;
    }

    private bool RegisterTypeIsNew(string typeName, CXType type, CXCursor cursor)
    {
        if (typeName == string.Empty)
        {
            return true;
        }

        var alreadyVisited = _typesByName.TryGetValue(typeName, out var typeC);
        if (alreadyVisited)
        {
            // attempt to see if we have a definition for a previous opaque type, to which we should that info instead
            //  this can happen if one header file has a forward type, but another header file has the definition
            if (typeC!.Kind != CKind.OpaqueType)
            {
                return false;
            }

            var typeKind = TypeKind(type);
            if (typeKind.Kind == CKind.OpaqueType)
            {
                return false;
            }

            typeC = Type(typeName, cursor, type);
            _typesByName[typeName] = typeC;
            return true;
        }

        typeC = Type(typeName, cursor, type);

        _typesByName.Add(typeName, typeC);
        _types.Add(typeC);

        return true;
    }

    private void AddExplorerNode(
        CKind kind,
        ClangLocation location,
        Node? parent,
        CXCursor cursor,
        CXType type,
        CXType originalType,
        string name,
        string typeName)
    {
        if (kind != CKind.TranslationUnit &&
            kind != CKind.MacroDefinition &&
            type.kind == CXTypeKind.CXType_Invalid)
        {
            var up = new ClangExplorerException("Explorer node can't be invalid type kind.");
            throw up;
        }

        var isIgnored = IsIgnored(type, cursor);
        if (isIgnored)
        {
            return;
        }

        var node = new Node(
            kind,
            location,
            parent,
            cursor,
            type,
            originalType,
            name,
            typeName);

        if (kind == CKind.MacroDefinition)
        {
            _frontierMacros.PushBack(node);
        }
        else
        {
            _frontierGeneral.PushBack(node);
        }
    }

    private static CFunctionCallingConvention CreateFunctionCallingConvention(CXType type)
    {
        var callingConvention = clang_getFunctionTypeCallingConv(type);
        var result = callingConvention switch
        {
            CXCallingConv.CXCallingConv_C => CFunctionCallingConvention.C,
            _ => throw new ClangExplorerException($"Unknown calling convention '{callingConvention}'.")
        };

        return result;
    }

    private ImmutableArray<CFunctionParameter> CreateFunctionParameters(CXCursor cursor, Node parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

        var parameterCursors = cursor.GetDescendents((child, _) =>
            child.kind == CXCursorKind.CXCursor_ParmDecl);

        foreach (var parameterCursor in parameterCursors)
        {
            var functionExternParameter = FunctionParameter(parameterCursor, parentNode);
            builder.Add(functionExternParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionParameter FunctionParameter(CXCursor cursor, Node parentNode)
    {
        var type = clang_getCursorType(cursor);
        var name = cursor.Name();

        var (kind, typeActual) = TypeKind(type);
        var typeName = TypeName(parentNode.TypeName!, kind, typeActual, cursor);

        VisitType(parentNode, cursor, cursor, type, type, typeName);
        var codeLocation = Location(cursor, type);

        return new CFunctionParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeName
        };
    }

    private CFunctionPointer CreateFunctionPointer(
        string typeName, CXCursor cursor, Node parentNode, CXType originalType, CXType type, ClangLocation location)
    {
        var parameters = CreateFunctionPointerParameters(cursor, parentNode);

        var returnType = clang_getResultType(type);
        var (kind, actualReturnType) = TypeKind(returnType);
        var returnTypeName = TypeName(parentNode.TypeName!, kind, actualReturnType, cursor);
        VisitType(parentNode, cursor, cursor, returnType, returnType, returnTypeName);

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
            Parameters = parameters
        };

        return functionPointer;
    }

    private ImmutableArray<CFunctionPointerParameter> CreateFunctionPointerParameters(
        CXCursor cursor, Node parentNode)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionPointerParameter>();

        var parameterCursors = cursor.GetDescendents((child, _) =>
            child.kind == CXCursorKind.CXCursor_ParmDecl);

        foreach (var parameterCursor in parameterCursors)
        {
            var functionPointerParameter = CreateFunctionPointerParameter(parameterCursor, parentNode);
            builder.Add(functionPointerParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CFunctionPointerParameter CreateFunctionPointerParameter(CXCursor cursor, Node parentNode)
    {
        var type = clang_getCursorType(cursor);
        var codeLocation = Location(cursor, type);
        var name = cursor.Name();

        var (kind, actualType) = TypeKind(type);
        var typeName = TypeName(parentNode.TypeName!, kind, actualType, cursor);

        VisitType(parentNode, cursor, cursor, type, type, typeName);

        return new CFunctionPointerParameter
        {
            Name = name,
            Location = codeLocation,
            Type = typeName
        };
    }

    private ImmutableArray<CRecordField> CreateRecordFields(string recordName, CXCursor cursor, Node parentNode)
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
            var recordField = CreateRecordField(recordName, fieldCursor, parentNode);
            builder.Add(recordField);
        }

        CalculatePaddingForStructFields(cursor, builder);

        var result = builder.ToImmutable();
        return result;
    }

    private void CalculatePaddingForStructFields(
        CXCursor cursor,
        ImmutableArray<CRecordField>.Builder builder)
    {
        for (var i = 1; i < builder.Count; i++)
        {
            var recordField = builder[i];
            var fieldPrevious = builder[i - 1];
            var typeName = Value(fieldPrevious.Type);
            var type = _typesByName[typeName];

            var fieldPreviousTypeSizeOf = type!.SizeOf!.Value;
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
            var type = _typesByName[typeName];
            var fieldLastTypeSize = type.SizeOf!.Value;
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
            return typeName.Replace(pointeeTypeName, result2);
        }

        return typeName;
    }

    private CRecordField CreateRecordField(string recordName, CXCursor cursor, Node parentNode)
    {
        var name = cursor.Name();
        var type = clang_getCursorType(cursor);
        var codeLocation = Location(cursor, type);
        var (kind, actualType) = TypeKind(type);
        var typeName = TypeName(recordName, kind, actualType, cursor);

        VisitType(parentNode, cursor, cursor, type, type, typeName);

        var offset = (int)(clang_Cursor_getOffsetOfField(cursor) / 8);

        return new CRecordField
        {
            Name = name,
            Location = codeLocation,
            Type = typeName,
            Offset = offset
        };
    }

    private ImmutableArray<CNode> CreateNestedNodes(string parentTypeName, CXCursor cursor, Node parentNode)
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
            var record = CreateNestedNode(parentTypeName, nestedCursor, parentNode);
            builder.Add(record);
        }

        if (builder.Count == 0)
        {
            return ImmutableArray<CNode>.Empty;
        }

        return builder.ToImmutable();
    }

    private CNode CreateNestedNode(string parentTypeName, CXCursor cursor, Node parentNode)
    {
        var type = clang_getCursorType(cursor);
        var isPointer = type.kind == CXTypeKind.CXType_Pointer;
        if (!isPointer)
        {
            return CreateNestedStruct(parentTypeName, cursor, type, parentNode);
        }

        var pointeeType = clang_getPointeeType(type);
        if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
        {
            var typeName = TypeName(parentTypeName, CKind.FunctionPointer, pointeeType, cursor);
            var location = Location(cursor, type);
            return CreateFunctionPointer(typeName, cursor, parentNode, type, pointeeType, location);
        }

        var up = new ClangExplorerException("Unknown mapping for nested node.");
        throw up;
    }

    private CNode CreateNestedStruct(string parentTypeName, CXCursor cursor, CXType type, Node parentNode)
    {
        var location = Location(cursor, type);
        var typeName = TypeName(parentTypeName, CKind.Record, type, cursor);

        var recordFields = CreateRecordFields(typeName, cursor, parentNode);
        var nestedNodes = CreateNestedNodes(typeName, cursor, parentNode);
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

    private ImmutableArray<CEnumValue> CreateEnumValues(CXCursor cursor)
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
        var location = Location(cursor);
        name ??= cursor.Name();

        return new CEnumValue
        {
            Location = location,
            Name = name,
            Value = value
        };
    }

    private CType Type(string typeName, CXCursor cursor, CXType clangType)
    {
        var type = clangType;
        var declaration = clang_getTypeDeclaration(type);
        if (declaration.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            declaration = cursor;
        }

        var sizeOfValue = (int)clang_Type_getSizeOf(type);
        if (sizeOfValue == -2)
        {
            if (type.kind == CXTypeKind.CXType_IncompleteArray)
            {
                var elementType = clang_getElementType(type);
                if (elementType.kind == CXTypeKind.CXType_Pointer)
                {
                    sizeOfValue = (int)clang_Type_getSizeOf(elementType);
                }
            }
        }

        int? sizeOf = sizeOfValue >= 0 ? sizeOfValue : null;
        var alignOfValue = (int)clang_Type_getAlignOf(type);
        int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
        var arraySizeValue = (int)clang_getArraySize(type);
        int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;
        var isSystemType = type.IsSystem();

        int? elementSize = null;
        if (type.kind == CXTypeKind.CXType_ConstantArray)
        {
            var elementType = clang_getElementType(type);
            elementSize = (int)clang_Type_getSizeOf(elementType);
        }

        ClangLocation? location = null;

        var typeKind = TypeKind(type);
        if (typeKind.Kind != CKind.Primitive && !isSystemType)
        {
            location = Location(declaration, type);
        }

        var cType = new CType
        {
            Name = typeName,
            Kind = typeKind.Kind,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ElementSize = elementSize,
            ArraySize = arraySize,
            IsSystem = isSystemType,
            Location = location
        };

        if (location != null)
        {
            var fileName = location.Value.FileName;
            if (_ignoredFiles.Contains(fileName))
            {
                var diagnostic = new DiagnosticTypeFromIgnoredFile(typeName, fileName);
                _diagnostics.Add(diagnostic);
            }
        }

        return cType;
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

        var up = new ClangExplorerException($"Unknown type kind '{type.kind}'.");
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
        Node parentNode,
        CXCursor cursor,
        CXCursor originalCursor,
        CXType type,
        CXType originalType,
        string typeName)
    {
        if (!RegisterTypeIsNew(typeName, type, cursor))
        {
            return;
        }

        var isValidTypeName = TypeNameIsValid(type, typeName);
        if (!isValidTypeName)
        {
            return;
        }

        var typeKind = TypeKind(type);
        if (typeKind.Kind == CKind.Pointer)
        {
            var pointeeType = clang_getPointeeType(typeKind.Type);
            var pointeeKind = TypeKind(pointeeType);
            var pointeeCursor = clang_getTypeDeclaration(pointeeType);
            var pointeeTypeName = TypeName(parentNode.TypeName!, pointeeKind.Kind, pointeeKind.Type, pointeeCursor);
            VisitType(parentNode, pointeeCursor, originalCursor, pointeeKind.Type, type, pointeeTypeName);
            return;
        }

        if (typeKind.Kind == CKind.Typedef)
        {
            VisitTypedef(parentNode, typeKind.Type, typeName);
        }
        else
        {
            CXType locationType;
            if (type.kind == CXTypeKind.CXType_IncompleteArray ||
                type.kind == CXTypeKind.CXType_ConstantArray)
            {
                locationType = clang_getElementType(type);
            }
            else if (type.kind == CXTypeKind.CXType_Pointer)
            {
                locationType = clang_getPointeeType(type);
            }
            else
            {
                locationType = type;
            }

            var locationCursor = clang_getTypeDeclaration(locationType);
            if (locationCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                locationCursor = cursor;
            }

            if (locationCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                locationCursor = originalCursor;
            }

            var location = Location(locationCursor);
            AddExplorerNode(
                typeKind.Kind,
                location,
                parentNode,
                cursor,
                typeKind.Type,
                originalType,
                string.Empty,
                typeName);
        }
    }

    private void VisitTypedef(Node parentNode, CXType type, string typeName)
    {
        var typedefCursor = clang_getTypeDeclaration(type);
        var location = typedefCursor.FileLocation();
        AddExplorerNode(CKind.Typedef, location, parentNode, typedefCursor, type, type, string.Empty, typeName);
    }

    private string TypeName(string parentTypeName, CKind kind, CXType type, CXCursor cursor)
    {
        var typeCursor = clang_getTypeDeclaration(type);
        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) != 0;
        if (isAnonymous)
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
                    var up = new ClangExplorerException($"Unknown anonymous cursor kind '{anonymousCursor.kind}'");
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
            _ => throw new ClangExplorerException($"Unexpected node kind '{kind}'.")
        };

        if (type.kind == CXTypeKind.CXType_IncompleteArray)
        {
            typeName = $"{typeName}";
        }
        else if (type.kind == CXTypeKind.CXType_ConstantArray)
        {
            var arraySize = clang_getArraySize(type);
            typeName = $"{typeName}[{arraySize}]";
        }

        if (string.IsNullOrEmpty(typeName))
        {
            throw new ClangExplorerException($"Type name was not found for '{kind}'.");
        }

        if (kind == CKind.Enum)
        {
            typeName = typeName switch
            {
                // TRICK: Force signed integer; enums in C could be signed or unsigned depending the platform architecture.
                //  This makes for a slightly different bindings between Windows/macOS/Linux where the enum is different type
                "unsigned char" or "char" => "signed char",
                "short" or "unsigned short" => "signed short",
                "short int" or "unsigned short int" => "signed short int",
                "unsigned" => "signed",
                "int" or "unsigned int" => "signed int",
                "long" or "unsigned long" => "signed long",
                "long int" or "unsigned long int" => "signed long int",
                "long long" or "unsigned long long" => "signed long long",
                "long long int" or "unsigned long long int" => "signed long long int",
                _ => typeName
            };
        }

        return typeName;
    }

    private ClangLocation Location(CXCursor cursor, CXType? type = null)
    {
        ClangLocation location;
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

        foreach (var includeDirectory in _includeDirectories)
        {
            if (location.FileName.Contains(includeDirectory))
            {
                location.FileName = location.FileName.Replace(includeDirectory, string.Empty).Trim('/', '\\');
                break;
            }
        }

        return location;
    }

    public class Node
    {
        public readonly ClangLocation Location;
        public readonly CXCursor Cursor;
        public readonly CKind Kind;
        public readonly string? Name;
        public readonly string? TypeName;
        public readonly CXType OriginalType;
        public readonly Node? Parent;
        public readonly CXType Type;

        public Node(
            CKind kind,
            ClangLocation location,
            Node? parent,
            CXCursor cursor,
            CXType type,
            CXType originalType,
            string? name,
            string? typeName)
        {
            Kind = kind;

            if (string.IsNullOrEmpty(location.FileName))
            {
                if (type.IsPrimitive())
                {
                    // Primitives don't have a location
                    Location = new ClangLocation
                    {
                        FilePath = string.Empty,
                        FileName = "Builtin",
                        LineColumn = 0,
                        LineNumber = 0,
                        IsBuiltin = true
                    };
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                Location = location;
            }

            Parent = parent;
            Cursor = cursor;
            Type = type;
            OriginalType = originalType;
            Name = name;
            TypeName = typeName;
        }

        public override string ToString()
        {
            return Location.ToString();
        }
    }
}
