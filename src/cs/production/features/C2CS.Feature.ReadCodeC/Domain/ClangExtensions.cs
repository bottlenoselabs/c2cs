// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using C2CS.Feature.ReadCodeC.Data;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain;

public static unsafe class ClangExtensions
{
    public delegate bool VisitPredicate(CXCursor child, CXCursor parent);

    private static VisitInstance[] _visitInstances = Array.Empty<VisitInstance>();
    private static int _visitsCount;

    private static readonly CXCursorVisitor Visit;

    static ClangExtensions()
    {
        Visit.Pointer = &Visitor;
    }

    [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Justification = "Result is useless.")]
    public static ImmutableArray<CXCursor> GetDescendents(
        this CXCursor cursor, VisitPredicate predicate)
    {
        var visitData = new VisitInstance(predicate);
        var visitsCount = Interlocked.Increment(ref _visitsCount);
        if (visitsCount > _visitInstances.Length)
        {
            Array.Resize(ref _visitInstances, visitsCount * 2);
        }

        _visitInstances[visitsCount - 1] = visitData;

        var clientData = default(CXClientData);
        clientData.Data = (void*)_visitsCount;
        clang_visitChildren(cursor, Visit, clientData);

        Interlocked.Decrement(ref _visitsCount);

        var result = visitData.NodeBuilder.ToImmutable();
        return result;
    }

    [UnmanagedCallersOnly]
    private static CXChildVisitResult Visitor(CXCursor child, CXCursor parent, CXClientData clientData)
    {
        var index = (int)clientData.Data;
        var data = _visitInstances[index - 1];

        var result = data.Predicate(child, parent);
        if (!result)
        {
            return CXChildVisitResult.CXChildVisit_Continue;
        }

        data.NodeBuilder.Add(child);

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    public static bool IsSystem(this CXCursor cursor)
    {
        var location = clang_getCursorLocation(cursor);
        var isInSystemHeader = clang_Location_isInSystemHeader(location) > 0U;
        return isInSystemHeader;
    }

    public static bool IsSystem(this CXType type)
    {
        var kind = type.kind;

        if (type.IsPrimitive())
        {
            return true;
        }

        switch (kind)
        {
            case CXTypeKind.CXType_Pointer:
                var pointeeType = clang_getPointeeType(type);
                return IsSystem(pointeeType);
            case CXTypeKind.CXType_ConstantArray:
            case CXTypeKind.CXType_IncompleteArray:
                var elementType = clang_getElementType(type);
                return IsSystem(elementType);
            case CXTypeKind.CXType_Typedef:
            case CXTypeKind.CXType_Elaborated:
            case CXTypeKind.CXType_Record:
            case CXTypeKind.CXType_Enum:
            case CXTypeKind.CXType_FunctionProto:
                var declaration = clang_getTypeDeclaration(type);
                return IsSystem(declaration);
            case CXTypeKind.CXType_FunctionNoProto:
                return false;
            case CXTypeKind.CXType_Attributed:
                var modifiedType = clang_Type_getModifiedType(type);
                return IsSystem(modifiedType);
            default:
                throw new NotImplementedException();
        }
    }

    public static string Name(this CXCursor clangCursor)
    {
        var spelling = clang_getCursorSpelling(clangCursor);
        string result = clang_getCString(spelling);
        return SanitizeClangName(result);
    }

    private static string SanitizeClangName(string result)
    {
        if (result.Contains("struct ", StringComparison.InvariantCulture))
        {
            result = result.Replace("struct ", string.Empty, StringComparison.InvariantCulture);
        }

        if (result.Contains("union ", StringComparison.InvariantCulture))
        {
            result = result.Replace("union ", string.Empty, StringComparison.InvariantCulture);
        }

        if (result.Contains("enum ", StringComparison.InvariantCulture))
        {
            result = result.Replace("enum ", string.Empty, StringComparison.InvariantCulture);
        }

        if (result.Contains("const ", StringComparison.InvariantCulture))
        {
            result = result.Replace("const ", string.Empty, StringComparison.InvariantCulture);
        }

        return result;
    }

    public static bool IsPrimitive(this CXType type)
    {
        return type.kind switch
        {
            CXTypeKind.CXType_Void => true,
            CXTypeKind.CXType_Bool => true,
            CXTypeKind.CXType_Char_S => true,
            CXTypeKind.CXType_SChar => true,
            CXTypeKind.CXType_Char_U => true,
            CXTypeKind.CXType_UChar => true,
            CXTypeKind.CXType_UShort => true,
            CXTypeKind.CXType_UInt => true,
            CXTypeKind.CXType_ULong => true,
            CXTypeKind.CXType_ULongLong => true,
            CXTypeKind.CXType_Short => true,
            CXTypeKind.CXType_Int => true,
            CXTypeKind.CXType_Long => true,
            CXTypeKind.CXType_LongLong => true,
            CXTypeKind.CXType_Float => true,
            CXTypeKind.CXType_Double => true,
            _ => false
        };
    }

    public static string Name(this CXType type, CXCursor? cursor = null)
    {
        var spellingC = clang_getTypeSpelling(type);
        string spelling = clang_getCString(spellingC);
        if (string.IsNullOrEmpty(spelling))
        {
            return string.Empty;
        }

        string result;

        if (type.IsPrimitive() || type.kind == CXTypeKind.CXType_FunctionProto)
        {
            result = spelling;
        }
        else
        {
            var cursorType = cursor ?? clang_getTypeDeclaration(type);

            switch (type.kind)
            {
                case CXTypeKind.CXType_Pointer:
                    var pointeeType = clang_getPointeeType(type);
                    var pointeeCursor = clang_getTypeDeclaration(pointeeType);
                    if (pointeeCursor.kind == CXCursorKind.CXCursor_NoDeclFound &&
                        pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        // Function pointer without a declaration, this can happen when the type is field or a param
                        var functionProtoSpellingC = clang_getTypeSpelling(pointeeType);
                        string functionProtoSpelling = clang_getCString(functionProtoSpellingC);
                        result = functionProtoSpelling;
                    }
                    else
                    {
                        // Pointer to some type
                        var pointeeTypeName = Name(pointeeType, pointeeCursor);
                        result = $"{pointeeTypeName}*";
                    }

                    break;
                case CXTypeKind.CXType_Typedef:
                    // typedef always has a declaration
                    var typedef = clang_getTypeDeclaration(type);
                    result = typedef.Name();
                    break;
                case CXTypeKind.CXType_Record:
                    result = type.NameInternal();
                    break;
                case CXTypeKind.CXType_Enum:
                    result = cursorType.Name();
                    if (string.IsNullOrEmpty(result))
                    {
                        result = type.NameInternal();
                    }

                    break;
                case CXTypeKind.CXType_ConstantArray:
                    var elementTypeConstantArray = clang_getArrayElementType(type);
                    result = Name(elementTypeConstantArray, cursorType);
                    break;
                case CXTypeKind.CXType_IncompleteArray:
                    var elementTypeIncompleteArray = clang_getArrayElementType(type);
                    var elementTypeName = Name(elementTypeIncompleteArray, cursorType);
                    result = $"{elementTypeName}*";
                    break;
                case CXTypeKind.CXType_Elaborated:
                    // type has a modifier prefixed such as "struct MyStruct" or "union ABC",
                    // drill down to the type and cursor with just the name
                    var namedTyped = clang_Type_getNamedType(type);
                    var namedCursor = clang_getTypeDeclaration(namedTyped);
                    result = Name(namedTyped, namedCursor);
                    break;
                case CXTypeKind.CXType_Attributed:
                    var modifiedType = clang_Type_getModifiedType(type);
                    result = Name(modifiedType, cursorType);
                    break;
                default:
                    return string.Empty;
            }
        }

        result = SanitizeClangName(result);

        return result;
    }

    private static string NameInternal(this CXType clangType)
    {
        var spelling = clang_getTypeSpelling(clangType);

        var resultC = clang_getCString(spelling);
        var result = Runtime.CStrings.String(resultC);
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        result = SanitizeClangName(result);
        return result;
    }

    public static CLocation FileLocation(this CXType type, CXCursor cursor)
    {
        var declaration = type.IsPrimitive() ? cursor : clang_getTypeDeclaration(type);
        return FileLocation(declaration);
    }

    public static CLocation FileLocation(this CXCursor cursor)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            var filePath = cursor.Name();
            return new CLocation
            {
                FileName = Path.GetFileName(filePath),
                FilePath = filePath
            };
        }

        var location = clang_getCursorLocation(cursor);
        CXFile file;
        ulong lineNumber;
        ulong columnNumber;
        ulong offset;

        clang_getFileLocation(location, &file, &lineNumber, &columnNumber, &offset);

        var isInSystemHeader = clang_Location_isInSystemHeader(location) > 0U;
        if (isInSystemHeader)
        {
            return CLocation.System;
        }

        var isPrimitive = clang_getCursorType(cursor).IsPrimitive();
        if (isPrimitive)
        {
            return CLocation.System;
        }

        var handle = (IntPtr)file.Data;
        if (handle == IntPtr.Zero)
        {
            return new CLocation
            {
                FileName = string.Empty
            };
        }

        var fileName = clang_getFileName(file);
        string fileNamePath = clang_getCString(fileName);

        return new CLocation
        {
            FileName = Path.GetFileName(fileNamePath),
            FilePath = string.IsNullOrEmpty(fileNamePath) ? string.Empty : Path.GetFullPath(fileNamePath),
            LineNumber = (int)lineNumber,
            LineColumn = (int)columnNumber
        };
    }

    private readonly struct VisitInstance
    {
        public readonly VisitPredicate Predicate;
        public readonly ImmutableArray<CXCursor>.Builder NodeBuilder;

        public VisitInstance(VisitPredicate predicate)
        {
            Predicate = predicate;
            NodeBuilder = ImmutableArray.CreateBuilder<CXCursor>();
        }
    }
}
