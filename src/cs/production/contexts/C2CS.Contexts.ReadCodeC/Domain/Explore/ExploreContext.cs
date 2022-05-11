// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Foundation.UseCases.Exceptions;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;

public sealed partial class ExploreContext
{
    private readonly ImmutableDictionary<CKind, ExploreHandler> _handlers;
    private readonly Action<ExploreContext, CKind, ExploreInfoNode> _tryEnqueueVisitNode;
    private readonly ImmutableDictionary<string, string> _linkedPaths;

    public ExplorerOptions Options { get; }

    public ImmutableArray<string> UserIncludeDirectories { get; }

    public TargetPlatform TargetPlatformRequested { get; }

    public TargetPlatform TargetPlatformActual { get; }

    public int PointerSize { get; }

    public string FilePath { get; }

    public ExploreContext(
        ImmutableDictionary<CKind, ExploreHandler> handlers,
        TargetPlatform targetPlatformRequested,
        CXTranslationUnit translationUnit,
        ExplorerOptions options,
        Action<ExploreContext, CKind, ExploreInfoNode> tryEnqueueVisitNode,
        ImmutableArray<string> userIncludeDirectories,
        ImmutableDictionary<string, string> linkedPaths)
    {
        var targetPlatformInfo = GetTargetPlatform(translationUnit);
        FilePath = GetFilePath(translationUnit);
        TargetPlatformRequested = targetPlatformRequested;
        TargetPlatformActual = targetPlatformInfo.TargetPlatform;
        PointerSize = targetPlatformInfo.PointerWidth / 8;
        Options = options;
        _tryEnqueueVisitNode = tryEnqueueVisitNode;
        UserIncludeDirectories = userIncludeDirectories;
        _linkedPaths = linkedPaths;
        _handlers = handlers;
    }

    private static (TargetPlatform TargetPlatform, int PointerWidth) GetTargetPlatform(CXTranslationUnit translationUnit)
    {
        var targetInfo = clang_getTranslationUnitTargetInfo(translationUnit);
        var targetInfoTriple = clang_TargetInfo_getTriple(targetInfo);
        var pointerWidth = clang_TargetInfo_getPointerWidth(targetInfo);
        string platformString = clang_getCString(targetInfoTriple);
        var platform = new TargetPlatform(platformString);
        clang_TargetInfo_dispose(targetInfo);
        return (platform, pointerWidth);
    }

    private string GetFilePath(CXTranslationUnit translationUnit)
    {
        var translationUnitSpelling = clang_getTranslationUnitSpelling(translationUnit);
        string result = clang_getCString(translationUnitSpelling);
        return result;
    }

    private string TypeName(CKind kind, CXType type, string? parentName, int fieldIndex = 0)
    {
        if (kind is
            CKind.Macro or
            CKind.Function)
        {
            return string.Empty;
        }

        var name = type.Name();
        var typeCursor = clang_getTypeDeclaration(type);

        var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
        if (isAnonymous)
        {
            return $"{parentName}_ANONYMOUS_FIELD{fieldIndex}";
        }

        if (name.Contains("(unnamed at ", StringComparison.InvariantCulture))
        {
            return $"{parentName}_UNNAMED_FIELD{fieldIndex}";
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

    public CLocation Location(CXCursor cursor, CXType type)
    {
        var location = Location2(cursor, type, false);

        if (string.IsNullOrEmpty(location.FilePath))
        {
            return location;
        }

        foreach (var (linkedDirectory, targetDirectory) in _linkedPaths)
        {
            if (location.FilePath.Contains(linkedDirectory, StringComparison.InvariantCulture))
            {
                location.FilePath = location.FilePath
                    .Replace(linkedDirectory, targetDirectory, StringComparison.InvariantCulture).Trim('/', '\\');
                break;
            }
        }

        if (!Options.IsEnabledLocationFullPaths)
        {
            foreach (var directory in UserIncludeDirectories)
            {
                if (location.FilePath.Contains(directory, StringComparison.InvariantCulture))
                {
                    location.FilePath = location.FilePath
                        .Replace(directory, string.Empty, StringComparison.InvariantCulture).Trim('/', '\\');
                    break;
                }
            }
        }

        return location;
    }

    private unsafe CLocation Location2(CXCursor cursor, CXType type, bool drillDown)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            return CLocation.NoLocation;
        }

        if (cursor.kind != CXCursorKind.CXCursor_FunctionDecl &&
            type.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
        {
            return CLocation.NoLocation;
        }

        if (type.kind is
            CXTypeKind.CXType_Pointer or
            CXTypeKind.CXType_ConstantArray or
            CXTypeKind.CXType_IncompleteArray)
        {
            return CLocation.NoLocation;
        }

        if (type.IsPrimitive())
        {
            return CLocation.NoLocation;
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
                var underlyingLocation = Location2(underlyingCursor, underlyingType, true);
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

    public CTypeInfo? VisitType(CXType typeCandidate, ExploreInfoNode? parentInfo, int fieldIndex = 0)
    {
        var (kind, type) = TypeKind(typeCandidate);
        var cursor = clang_getTypeDeclaration(type);
        var name = cursor.Name();

        var info = CreateVisitInfoNode(kind, name, cursor, type, parentInfo, fieldIndex);
        var handler = GetHandler(kind);
        if (handler.IsBlocked(this, info.TypeName, info.Cursor))
        {
            return CreateTypeInfoBlocked(info);
        }

        if (Options.OpaqueTypesNames.Contains(name))
        {
            TryEnqueueVisitInfoNode(CKind.OpaqueType, info);
            var typeInfoOpaque = CreateTypeInfo(CKind.OpaqueType, info.TypeName, type, typeCandidate);
            return typeInfoOpaque;
        }

        if (kind == CKind.TypeAlias)
        {
            var underlyingTypeCandidate = clang_getTypedefDeclUnderlyingType(cursor);
            var (underlyingTypeKind, underlyingType) = TypeKind(underlyingTypeCandidate);
            if (underlyingTypeKind is
                CKind.Enum or
                CKind.Struct or
                CKind.Union or
                CKind.FunctionPointer)
            {
                var underlyingTypeInfo = VisitType(underlyingType, info)!;
                var typeAliasTypeInfo = CreateTypeInfoTypeAlias(info.TypeName, underlyingTypeInfo);
                return typeAliasTypeInfo;
            }

            if (underlyingTypeKind is CKind.Pointer)
            {
                TryEnqueueVisitInfoNode(kind, info);
                var underlyingTypeInfo = VisitType(underlyingType, info)!;
                var typeAliasTypeInfo = CreateTypeInfoTypeAlias(info.TypeName, underlyingTypeInfo);
                return typeAliasTypeInfo;
            }
        }

        TryEnqueueVisitInfoNode(kind, info);
        var typeInfo = CreateTypeInfo(kind, info.TypeName, type, typeCandidate);
        return typeInfo;
    }

    private void TryEnqueueVisitInfoNode(CKind kind, ExploreInfoNode exploreInfo)
    {
        _tryEnqueueVisitNode(this, kind, exploreInfo);
    }

    private (CKind Kind, CXType Type) TypeKind(CXType type)
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
                return TypeKindRecord(cursorType, cursor.kind);
            case CXTypeKind.CXType_Typedef:
                return TypeKindTypeAlias(cursor, cursorType);
            case CXTypeKind.CXType_FunctionNoProto or CXTypeKind.CXType_FunctionProto:
                return (CKind.Function, cursorType);
            case CXTypeKind.CXType_Pointer:
                return TypeKindPointer(cursorType);
            case CXTypeKind.CXType_Attributed:
                var modifiedType = clang_Type_getModifiedType(cursorType);
                return TypeKind(modifiedType);
            case CXTypeKind.CXType_Elaborated:
                var namedType = clang_Type_getNamedType(cursorType);
                return TypeKind(namedType);
            case CXTypeKind.CXType_ConstantArray:
            case CXTypeKind.CXType_IncompleteArray:
                return (CKind.Array, cursorType);
            case CXTypeKind.CXType_Unexposed:
                var canonicalType = clang_getCanonicalType(type);
                return TypeKind(canonicalType);
        }

        var up = new UseCaseException($"Unknown type kind '{type.kind}'.");
        throw up;
    }

    private static (CKind Kind, CXType Type) TypeKindRecord(CXType cursorType, CXCursorKind cursorKind)
    {
        var sizeOfRecord = clang_Type_getSizeOf(cursorType);
        if (sizeOfRecord == -2)
        {
            return (CKind.OpaqueType, cursorType);
        }

        var kind = cursorKind == CXCursorKind.CXCursor_StructDecl ? CKind.Struct : CKind.Union;
        return (kind, cursorType);
    }

    private (CKind Kind, CXType Type) TypeKindTypeAlias(CXCursor cursor, CXType cursorType)
    {
        var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
        if (underlyingType.kind == CXTypeKind.CXType_Pointer)
        {
            return (CKind.TypeAlias, cursorType);
        }

        var (_, aliasType) = TypeKind(underlyingType);
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

        if (pointeeType.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
        {
            return (CKind.FunctionPointer, pointeeType);
        }

        return (CKind.Pointer, cursorType);
    }

    private CTypeInfo CreateTypeInfo(CKind kind, string typeName, CXType type, CXType containerType)
    {
        if (type.kind == CXTypeKind.CXType_Attributed)
        {
            var typeCandidate = clang_Type_getModifiedType(type);
            return CreateTypeInfo(kind, typeName, typeCandidate, containerType);
        }

        var sizeOf = SizeOf(kind, containerType);
        var alignOfValue = (int)clang_Type_getAlignOf(containerType);
        int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
        var arraySizeValue = (int)clang_getArraySize(containerType);
        int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;

        int? elementSize = null;
        if (kind == CKind.Array)
        {
            var (arrayKind, arrayType) = TypeKind(type);
            var elementType = clang_getElementType(arrayType);
            elementSize = SizeOf(arrayKind, elementType);
        }

        var locationCursor = clang_getTypeDeclaration(type);
        var location = Location(locationCursor, type);
        var isAnonymous = clang_Cursor_isAnonymous(locationCursor) > 0;

        CTypeInfo? innerType = null;
        if (kind is CKind.Pointer)
        {
            var pointeeTypeCandidate = clang_getPointeeType(type);
            var (pointeeTypeKind, pointeeType) = TypeKind(pointeeTypeCandidate);
            var pointeeTypeName = pointeeType.Name();
            innerType = CreateTypeInfo(pointeeTypeKind, pointeeTypeName, pointeeType, pointeeTypeCandidate);
        }
        else if (kind is CKind.Array)
        {
            var elementTypeCandidate = clang_getArrayElementType(type);
            var (elementTypeKind, elementType) = TypeKind(elementTypeCandidate);
            var elementTypeName = elementType.Name();
            innerType = CreateTypeInfo(elementTypeKind, elementTypeName, elementType, elementTypeCandidate);
        }

        var cType = new CTypeInfo
        {
            Name = typeName,
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
        // if (context.Options.HeaderFilesBlocked.Contains(fileName))
        // {
        //     var diagnostic = new TypeFromIgnoredHeaderDiagnostic(typeName, fileName);
        //     context.Diagnostics.Add(diagnostic);
        // }

        return cType;
    }

    private CTypeInfo? CreateTypeInfoBlocked(ExploreInfoNode info)
    {
        if (info.Parent == null)
        {
            return null;
        }

        if (info.Parent.Kind == CKind.TypeAlias && info.Kind == CKind.Pointer)
        {
            return CTypeInfo.VoidPointer(PointerSize);
        }

        if (info.Kind == CKind.TypeAlias)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(info.Cursor);
            var underlyingTypeInfo = VisitType(underlyingType, info);
            if (underlyingTypeInfo == null)
            {
                return null;
            }

            return underlyingTypeInfo;
        }

        return null;
    }

    private CTypeInfo CreateTypeInfoTypeAlias(string typeName, CTypeInfo underlyingTypeInfo)
    {
        var typeInfo = new CTypeInfo
        {
            Name = typeName,
            Kind = CKind.Pointer,
            SizeOf = PointerSize,
            AlignOf = PointerSize,
            ElementSize = null,
            ArraySizeOf = null,
            Location = CLocation.NoLocation,
            IsAnonymous = null,
            InnerType = underlyingTypeInfo
        };

        return typeInfo;
    }

    private int SizeOf(CKind kind, CXType type)
    {
        var sizeOf = (int)clang_Type_getSizeOf(type);
        if (sizeOf >= 0)
        {
            return sizeOf;
        }

        switch (kind)
        {
            case CKind.Primitive:
            case CKind.OpaqueType:
                return 0;
            case CKind.Pointer:
            case CKind.Array:
                return PointerSize;
            default:
                return sizeOf;
        }
    }

    public string CursorName(CXCursor cursor)
    {
        var spelling = clang_getCursorSpelling(cursor);
        string result = clang_getCString(spelling);
        return result;
    }

    public ExploreInfoNode CreateVisitInfoNode(
        CKind kind,
        string name,
        CXCursor cursor,
        CXType type,
        ExploreInfoNode? parentInfo,
        int fieldIndex = 0)
    {
        var typeNameActual = TypeName(kind, type, parentInfo?.Name, fieldIndex);
        var nameActual = !string.IsNullOrEmpty(name) ? name : typeNameActual;
        var location = Location(cursor, type);
        var sizeOf = SizeOf(kind, type);
        var alignOf = (int)clang_Type_getAlignOf(type);

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

    public bool CanVisit(CKind kind, ExploreInfoNode node)
    {
        var handler = GetHandler(kind);
        return handler.CanVisitInternal(this, node);
    }

    public CNode Explore(ExploreInfoNode node)
    {
        var handler = GetHandler(node.Kind);
        return handler.ExploreInternal(this, node);
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
