// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Data.C.Model;
using C2CS.Foundation.Executors;
using C2CS.ReadCodeC.Domain.Parse;
using C2CS.ReadCodeC.Infrastructure.Clang;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain.Explore;

public sealed class ExploreContext
{
    public readonly IReaderCCode Reader;

    private readonly ImmutableDictionary<CKind, ExploreHandler> _handlers;
    private readonly ImmutableDictionary<string, string> _linkedPaths;
    private readonly Action<ExploreContext, CKind, ExploreInfoNode> _tryEnqueueVisitNode;

    public ExploreContext(
        IReaderCCode reader,
        DiagnosticCollection diagnostics,
        ImmutableDictionary<CKind, ExploreHandler> handlers,
        TargetPlatform targetPlatformRequested,
        CXTranslationUnit translationUnit,
        ExploreOptions exploreExploreOptions,
        ParseOptions parseOptions,
        Action<ExploreContext, CKind, ExploreInfoNode> tryEnqueueVisitNode,
        ImmutableDictionary<string, string> linkedPaths)
    {
        Reader = reader;
        Diagnostics = diagnostics;
        FilePath = GetFilePath(translationUnit);
        TargetPlatformRequested = targetPlatformRequested;
        var targetPlatformInfo = GetTargetPlatform(translationUnit);
        TargetPlatformActual = targetPlatformInfo.TargetPlatform;
        PointerSize = targetPlatformInfo.PointerWidth / 8;
        TranslationUnit = translationUnit;
        ExploreOptions = exploreExploreOptions;
        ParseOptions = parseOptions;
        _tryEnqueueVisitNode = tryEnqueueVisitNode;
        _linkedPaths = linkedPaths;
        _handlers = handlers;
    }

    public DiagnosticCollection Diagnostics { get; }

    public ExploreOptions ExploreOptions { get; }

    public ParseOptions ParseOptions { get; }

    public TargetPlatform TargetPlatformRequested { get; }

    public TargetPlatform TargetPlatformActual { get; }

    public int PointerSize { get; }

    public CXTranslationUnit TranslationUnit { get; }

    public string FilePath { get; }

    public CTypeInfo? VisitType(
        CXType typeCandidate,
        ExploreInfoNode? rootInfo,
        int? fieldIndex = null,
        CKind? kindHint = null)
    {
        var (kind, type) = TypeKind(typeCandidate, rootInfo?.Kind);
        if (kindHint != null && kind != kindHint)
        {
            kind = kindHint.Value;
        }

        var cursor = clang_getTypeDeclaration(type);
        var typeName = TypeName(kind, type, rootInfo?.Name, rootInfo?.Kind, fieldIndex);

        var rootInfoWithLocation = rootInfo;
        while (rootInfoWithLocation != null && rootInfoWithLocation.Location == CLocation.NoLocation)
        {
            rootInfoWithLocation = rootInfoWithLocation.Parent;
        }

        var typeInfo = VisitTypeInternal(kind, typeName, type, typeCandidate, cursor, rootInfoWithLocation, null);

        return typeInfo;
    }

    public CLocation Location(
        CXCursor cursor,
        CXType type)
    {
        return cursor.GetLocation(type, _linkedPaths, ParseOptions.UserIncludeDirectories);
    }

    public string CursorName(CXCursor cursor)
    {
        var result = clang_getCursorSpelling(cursor).String();
        return result;
    }

    public ExploreInfoNode CreateVisitInfoNode(
        CKind kind,
        string name,
        CXCursor cursor,
        CXType type,
        ExploreInfoNode? parentInfo)
    {
        var location = Location(cursor, type);
        if (location.IsNull && parentInfo?.Kind == CKind.TypeAlias)
        {
            location = parentInfo.Location;
        }

        var typeNameActual = TypeName(kind, type, parentInfo?.Name, null);
        var nameActual = !string.IsNullOrEmpty(name) ? name : typeNameActual;
        var sizeOf = SizeOf(kind, type);
        var alignOf = AlignOf(kind, type);

        var result = new ExploreInfoNode
        {
            Kind = kind,
            Name = nameActual,
            TypeName = typeNameActual,
            Type = type,
            Cursor = cursor,
            Location = location,
            Parent = parentInfo,
            SizeOf = sizeOf,
            AlignOf = alignOf
        };

        return result;
    }

    public bool CanVisit(
        CKind kind,
        ExploreInfoNode node)
    {
        var handler = GetHandler(kind);
        return handler.CanVisitInternal(this, node);
    }

    public CNode? Explore(ExploreInfoNode node)
    {
        var handler = GetHandler(node.Kind);
        return handler.ExploreInternal(this, node);
    }

    internal bool IsAllowed(CXCursor cursor)
    {
        if (!ExploreOptions.IsEnabledSystemDeclarations)
        {
            var cursorLocation = clang_getCursorLocation(cursor);
            var isSystemCursor = clang_Location_isInSystemHeader(cursorLocation) > 0;
            if (isSystemCursor)
            {
                return false;
            }
        }

        return true;
    }

    private static (TargetPlatform TargetPlatform, int PointerWidth) GetTargetPlatform(
        CXTranslationUnit translationUnit)
    {
        var targetInfo = clang_getTranslationUnitTargetInfo(translationUnit);
        var targetInfoTriple = clang_TargetInfo_getTriple(targetInfo);
        var pointerWidth = clang_TargetInfo_getPointerWidth(targetInfo);
        var platformString = targetInfoTriple.String();
        var platform = new TargetPlatform(platformString);
        clang_TargetInfo_dispose(targetInfo);
        return (platform, pointerWidth);
    }

    private string GetFilePath(CXTranslationUnit translationUnit)
    {
        var translationUnitSpelling = clang_getTranslationUnitSpelling(translationUnit);
        return translationUnitSpelling.String();
    }

    private string TypeName(
        CKind kind,
        CXType type,
        string? parentName,
        CKind? parentKind,
        int? fieldIndex = null)
    {
        switch (kind)
        {
            case CKind.MacroObject or CKind.Function:
                return string.Empty;
            case CKind.FunctionPointer when !string.IsNullOrEmpty(parentName) && parentKind == CKind.TypeAlias:
                return parentName;
        }

        var isField = fieldIndex != null;
        var typeCursor = clang_getTypeDeclaration(type);
        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        if (isAnonymous)
        {
            return TypeNameAnonymous(kind, parentName, fieldIndex, isField, typeCursor);
        }

        var name = type.Name();
        if (name.Contains("(unnamed at ", StringComparison.InvariantCulture))
        {
            return $"{parentName}_UNNAMED_FIELD{fieldIndex}";
        }

        if (type.kind == CXTypeKind.CXType_ConstantArray)
        {
            var arraySize = clang_getArraySize(type);
            name = $"{name}[{arraySize}]";
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ExecutorException("Type name was not found.");
        }

        return name;
    }

    private static string TypeNameAnonymous(
        CKind kind,
        string? parentName,
        int? fieldIndex,
        bool isField,
        CXCursor typeCursor)
    {
        if (isField)
        {
            return $"{parentName}_ANONYMOUS_FIELD{fieldIndex}";
        }

        if (kind == CKind.Enum)
        {
            return TypeNameAnonymousEnum(typeCursor);
        }

        if (kind == CKind.Union)
        {
            var unionName = typeCursor.Name();
            return $"{parentName}_{unionName}";
        }

        return string.Empty;
    }

    private static string TypeNameAnonymousEnum(CXCursor typeCursor)
    {
        var enumConstants =
            typeCursor.GetDescendents(
                static (
                    cursor,
                    _) => cursor.kind == CXCursorKind.CXCursor_EnumConstantDecl,
                false);
        /* Example C code; this enum should have it's single member promoted as a macro object.
enum {
noErr = 0
};
 */
        if (enumConstants.Length <= 1)
        {
            return string.Empty;
        }

        var enumConstantNames = enumConstants.Select(x => x.Name()).ToImmutableArray();
        var enumConstantNamesBuffer = enumConstantNames.ToArray();

        while (true)
        {
            for (var i = 0; i < enumConstantNames.Length; i++)
            {
                var name2 = enumConstantNamesBuffer[i];
                /*Example C code; this enum should have every enum constant value handled as a macro object.
enum {
normal                        = 0,
bold                          = 1,
italic                        = 2,
underline                     = 4,
outline                       = 8,
shadow                        = 0x10,
condense                      = 0x20,
extend                        = 0x40
};
 */
                if (name2.Length == 0)
                {
                    return string.Empty;
                }

                enumConstantNamesBuffer[i] = name2[..^1];
            }

            var allAreSame = !enumConstantNamesBuffer.Distinct().Skip(1).Any();
            if (allAreSame)
            {
                return enumConstantNamesBuffer[0];
            }
        }
    }

    private void TryEnqueueVisitInfoNode(
        CKind kind,
        ExploreInfoNode info)
    {
        _tryEnqueueVisitNode(this, kind, info);
    }

    private (CKind Kind, CXType Type) TypeKind(
        CXType type,
        CKind? parentKind)
    {
        var cursor = clang_getTypeDeclaration(type);
        var cursorType = cursor.kind != CXCursorKind.CXCursor_NoDeclFound
            ? clang_getCursorType(cursor)
            : type;
        if (cursorType.IsPrimitive())
        {
            return (CKind.Primitive, type);
        }

        switch (cursorType.kind)
        {
            case CXTypeKind.CXType_Enum:
                return (CKind.Enum, cursorType);
            case CXTypeKind.CXType_Record:
                return TypeKindRecord(cursorType, cursor.kind);
            case CXTypeKind.CXType_Typedef:
                return TypeKindTypeAlias(parentKind, cursor, cursorType);
            case CXTypeKind.CXType_FunctionNoProto or CXTypeKind.CXType_FunctionProto:
                return TypeKindFunction(parentKind, cursor, cursorType);
            case CXTypeKind.CXType_Pointer:
                return TypeKindPointer(cursorType);
            case CXTypeKind.CXType_Attributed:
                var modifiedType = clang_Type_getModifiedType(cursorType);
                return TypeKind(modifiedType, parentKind);
            case CXTypeKind.CXType_Elaborated:
                var namedType = clang_Type_getNamedType(cursorType);
                return TypeKind(namedType, parentKind);
            case CXTypeKind.CXType_ConstantArray:
            case CXTypeKind.CXType_IncompleteArray:
                return (CKind.Array, cursorType);
            case CXTypeKind.CXType_Unexposed:
                var canonicalType = clang_getCanonicalType(type);
                return TypeKind(canonicalType, parentKind);
        }

        var up = new ExecutorException($"Unknown type kind '{type.kind}'.");
        throw up;
    }

    private static (CKind Kind, CXType Type) TypeKindFunction(
        CKind? parentKind,
        CXCursor cursor,
        CXType cursorType)
    {
        if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            return (CKind.FunctionPointer, cursorType);
        }

        return parentKind == CKind.TypeAlias
            ? (CKind.FunctionPointer, cursorType)
            : (CKind.Function, cursorType);
    }

    private static (CKind Kind, CXType Type) TypeKindRecord(
        CXType cursorType,
        CXCursorKind cursorKind)
    {
        var sizeOfRecord = clang_Type_getSizeOf(cursorType);
        if (sizeOfRecord == -2)
        {
            return (CKind.OpaqueType, cursorType);
        }

        var kind = cursorKind == CXCursorKind.CXCursor_StructDecl ? CKind.Struct : CKind.Union;
        return (kind, cursorType);
    }

    private (CKind Kind, CXType Type) TypeKindTypeAlias(
        CKind? parentKind,
        CXCursor cursor,
        CXType cursorType)
    {
        var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
        if (underlyingType.kind == CXTypeKind.CXType_Pointer)
        {
            return (CKind.TypeAlias, cursorType);
        }

        var (_, aliasType) = TypeKind(underlyingType, parentKind);
        var sizeOfAlias = clang_Type_getSizeOf(aliasType);
        var kind = sizeOfAlias == -2 ? CKind.OpaqueType : CKind.TypeAlias;
        return (kind, cursorType);
    }

    private static (CKind Kind, CXType Type) TypeKindPointer(CXType cursorType)
    {
        var pointeeType = clang_getPointeeType(cursorType);
        if (pointeeType.kind == CXTypeKind.CXType_Attributed)
        {
            pointeeType = clang_Type_getModifiedType(pointeeType);
        }

        if (pointeeType.kind is CXTypeKind.CXType_FunctionProto
            or CXTypeKind.CXType_FunctionNoProto)
        {
            return (CKind.FunctionPointer, pointeeType);
        }

        return (CKind.Pointer, cursorType);
    }

    private CTypeInfo? VisitTypeInternal(
        CKind kind,
        string typeName,
        CXType type,
        CXType containerType,
        CXCursor cursor,
        ExploreInfoNode? rootNode,
        bool? parentIsFromBlockedHeader)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            var typeCandidate = clang_Type_getModifiedType(type);
            var typeCandidateCursor = clang_getTypeDeclaration(typeCandidate);
            return VisitTypeInternal(
                kind,
                typeName,
                typeCandidate,
                containerType,
                typeCandidateCursor,
                rootNode,
                parentIsFromBlockedHeader);
        }

        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(locationCursor, type);

        var isFromBlockedHeader = false;
        if (!IsAllowed(cursor))
        {
            if (parentIsFromBlockedHeader.HasValue && parentIsFromBlockedHeader.Value)
            {
                return null;
            }

            if (typeName == "va_list")
            {
                return CTypeInfo.VoidPointer(PointerSize);
            }

            isFromBlockedHeader = true;
        }

        int sizeOf;
        int? alignOf;
        CTypeInfo? innerType = null;
        if (kind is CKind.Pointer)
        {
            var pointeeTypeCandidate = clang_getPointeeType(type);
            var (pointeeTypeKind, pointeeType) = TypeKind(pointeeTypeCandidate, kind);
            var pointeeTypeName = pointeeType.Name();
            var pointeeTypeCursor = clang_getTypeDeclaration(pointeeType);

            innerType = VisitTypeInternal(
                pointeeTypeKind,
                pointeeTypeName,
                pointeeType,
                pointeeTypeCandidate,
                pointeeTypeCursor,
                rootNode,
                isFromBlockedHeader);
            sizeOf = PointerSize;
            alignOf = PointerSize;
        }
        else if (kind is CKind.Array)
        {
            var elementTypeCandidate = clang_getArrayElementType(type);
            var (elementTypeKind, elementType) = TypeKind(elementTypeCandidate, kind);
            var elementTypeName = elementType.Name();
            var elementTypeCursor = clang_getTypeDeclaration(elementType);

            innerType = VisitTypeInternal(
                elementTypeKind,
                elementTypeName,
                elementType,
                elementTypeCandidate,
                elementTypeCursor,
                rootNode,
                isFromBlockedHeader);
            sizeOf = PointerSize;
            alignOf = PointerSize;
        }
        else if (kind is CKind.TypeAlias)
        {
            var aliasTypeCandidate = clang_getTypedefDeclUnderlyingType(cursor);
            var (aliasTypeKind, aliasType) = TypeKind(aliasTypeCandidate, kind);
            var aliasTypeName = aliasType.Name();
            var aliasTypeCursor = clang_getTypeDeclaration(aliasType);

            innerType = VisitTypeInternal(
                aliasTypeKind,
                aliasTypeName,
                aliasType,
                aliasTypeCandidate,
                aliasTypeCursor,
                rootNode,
                isFromBlockedHeader);
            if (innerType != null)
            {
                if (innerType.Kind == CKind.OpaqueType)
                {
                    return innerType;
                }

                sizeOf = innerType.SizeOf;
                alignOf = innerType.AlignOf;
            }
            else
            {
                sizeOf = SizeOf(kind, aliasType);
                alignOf = AlignOf(kind, aliasType);
            }
        }
        else
        {
            sizeOf = SizeOf(kind, containerType);
            alignOf = AlignOf(kind, containerType);
        }

        var arraySizeValue = (int)clang_getArraySize(containerType);
        int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;

        int? elementSize = null;
        if (kind == CKind.Array)
        {
            var (arrayKind, arrayType) = TypeKind(type, kind);
            var elementType = clang_getElementType(arrayType);
            elementSize = SizeOf(arrayKind, elementType);

            if (type.kind == CXTypeKind.CXType_ConstantArray)
            {
                sizeOf = arraySize!.Value * elementSize.Value;
            }
        }

        var isAnonymous = clang_Cursor_isAnonymous(locationCursor) > 0;
        var isConst = containerType.IsConst();

        var typeInfo = new CTypeInfo
        {
            Name = typeName,
            Kind = kind,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ElementSize = elementSize,
            ArraySizeOf = arraySize,
            Location = location,
            IsAnonymous = isAnonymous ? true : null,
            IsConst = isConst,
            InnerTypeInfo = innerType
        };

        if (typeInfo.Kind == CKind.TypeAlias && typeInfo.InnerTypeInfo != null &&
            typeInfo.Name == typeInfo.InnerTypeInfo.Name)
        {
            return typeInfo;
        }

        var visitInfo = CreateVisitInfoNode(typeInfo, cursor, type, rootNode);
        TryEnqueueVisitInfoNode(visitInfo.Kind, visitInfo);
        return typeInfo;
    }

    private int SizeOf(
        CKind kind,
        CXType type)
    {
        if (kind == CKind.OpaqueType)
        {
            return 0;
        }

        var sizeOf = (int)clang_Type_getSizeOf(type);
        if (sizeOf >= 0)
        {
            return sizeOf;
        }

        switch (kind)
        {
            case CKind.Primitive:
                return 0;
            case CKind.Pointer:
            case CKind.Array:
                return PointerSize;
            default:
                return sizeOf;
        }
    }

    private int? AlignOf(
        CKind kind,
        CXType containerType)
    {
        if (kind == CKind.OpaqueType)
        {
            return null;
        }

        var alignOfValue = (int)clang_Type_getAlignOf(containerType);
        int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
        return alignOf;
    }

    private static ExploreInfoNode CreateVisitInfoNode(
        CTypeInfo typeInfo,
        CXCursor cursor,
        CXType type,
        ExploreInfoNode? parentInfo)
    {
        var result = new ExploreInfoNode
        {
            Kind = typeInfo.Kind,
            Name = typeInfo.Name,
            TypeName = typeInfo.Name,
            Type = type,
            Cursor = cursor,
            Location = typeInfo.Location,
            Parent = parentInfo,
            SizeOf = typeInfo.SizeOf,
            AlignOf = typeInfo.AlignOf
        };

        return result;
    }

    private ExploreHandler GetHandler(CKind kind)
    {
        var handlerExists = _handlers.TryGetValue(kind, out var handler);
        if (handlerExists && handler != null)
        {
            return handler;
        }

        var up = new NotImplementedException($"The handler for kind of '{kind}' was not found.");
        throw up;
    }
}
