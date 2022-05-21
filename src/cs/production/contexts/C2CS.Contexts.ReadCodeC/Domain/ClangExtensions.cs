// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using C2CS.Contexts.ReadCodeC.Data.Model;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain;

public static unsafe class ClangExtensions
{
    public delegate bool VisitPredicate(CXCursor child, CXCursor parent);

    private static VisitInstance[] _visitInstances = Array.Empty<VisitInstance>();
    private static int _visitsCount;

    private static readonly CXCursorVisitor Visit;
    private static readonly CXCursorVisitor VisitRecursive;
    private static readonly VisitPredicate EmptyPredicate = static (_, _) => true;

    static ClangExtensions()
    {
        Visit.Pointer = &Visitor;
        VisitRecursive.Pointer = &VisitorRecursive;
    }

    [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Justification = "Result is useless.")]
    public static ImmutableArray<CXCursor> GetDescendents(
        this CXCursor cursor, VisitPredicate? predicate = null)
    {
        var predicate2 = predicate ?? EmptyPredicate;
        var visitData = new VisitInstance(predicate2);
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

    public static string String(this CXString cxString)
    {
        var cString = clang_getCString(cxString);
        var result = Marshal.PtrToStringAnsi(cString)!;
        clang_disposeString(cxString);
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

    [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Justification = "Result is useless.")]
    public static ImmutableArray<CXCursor> GetDescendentsRecursive(
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
        clang_visitChildren(cursor, VisitRecursive, clientData);

        Interlocked.Decrement(ref _visitsCount);

        var result = visitData.NodeBuilder.ToImmutable();
        return result;
    }

    [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", Justification = "Result is useless.")]
    [UnmanagedCallersOnly]
    private static CXChildVisitResult VisitorRecursive(CXCursor child, CXCursor parent, CXClientData clientData)
    {
        var index = (int)clientData.Data;
        var data = _visitInstances[index - 1];

        var result = data.Predicate(child, parent);
        if (!result)
        {
            return CXChildVisitResult.CXChildVisit_Continue;
        }

        clang_visitChildren(child, VisitRecursive, clientData);

        data.NodeBuilder.Add(child);

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    public static CLocation Location(
        this CXCursor cursor,
        CXType? type,
        ImmutableDictionary<string, string>? linkedPaths,
        ImmutableArray<string>? userIncludeDirectories)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            return CLocation.NoLocation;
        }

        if (type != null)
        {
            if (cursor.kind != CXCursorKind.CXCursor_FunctionDecl &&
                type.Value.kind is CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto)
            {
                return CLocation.NoLocation;
            }

            if (type.Value.kind is
                CXTypeKind.CXType_Pointer or
                CXTypeKind.CXType_ConstantArray or
                CXTypeKind.CXType_IncompleteArray)
            {
                return CLocation.NoLocation;
            }

            if (type.Value.IsPrimitive())
            {
                return CLocation.NoLocation;
            }
        }

        if (cursor.kind == CXCursorKind.CXCursor_NoDeclFound)
        {
            var up = new InvalidOperationException("Expected a valid cursor when getting the location.");
            throw up;
        }

        // if (drillDown)
        // {
        //     if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl && type.kind == CXTypeKind.CXType_Typedef)
        //     {
        //         var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
        //         var underlyingCursor = clang_getTypeDeclaration(underlyingType);
        //         var underlyingLocation = Location2(underlyingCursor, underlyingType, true);
        //         if (!underlyingLocation.IsNull)
        //         {
        //             return underlyingLocation;
        //         }
        //     }
        // }

        var locationSource = clang_getCursorLocation(cursor);
        CXFile file;
        uint lineNumber;
        uint columnNumber;
        uint offset;

        clang_getFileLocation(locationSource, &file, &lineNumber, &columnNumber, &offset);

        var handle = (IntPtr)file.Data;
        if (handle == IntPtr.Zero)
        {
            return LocationInTranslationUnit(cursor, (int)lineNumber, (int)columnNumber);
        }

        var fileNamePath = clang_getFileName(file).String();
        var fileName = Path.GetFileName(fileNamePath);
        var fullFilePath = string.IsNullOrEmpty(fileNamePath) ? string.Empty : Path.GetFullPath(fileNamePath);

        var location = new CLocation
        {
            FileName = fileName,
            FilePath = fullFilePath,
            FullFilePath = fullFilePath,
            LineNumber = (int)lineNumber,
            LineColumn = (int)columnNumber
        };

        if (string.IsNullOrEmpty(location.FilePath))
        {
            return location;
        }

        if (linkedPaths != null)
        {
            foreach (var (linkedDirectory, targetDirectory) in linkedPaths)
            {
                if (location.FilePath.Contains(linkedDirectory, StringComparison.InvariantCulture))
                {
                    location.FilePath = location.FilePath
                        .Replace(linkedDirectory, targetDirectory, StringComparison.InvariantCulture).Trim('/', '\\');
                    break;
                }
            }
        }

        if (userIncludeDirectories != null)
        {
            foreach (var directory in userIncludeDirectories)
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

    private static CLocation LocationInTranslationUnit(
        CXCursor declaration,
        int lineNumber,
        int columnNumber)
    {
        var translationUnit = clang_Cursor_getTranslationUnit(declaration);
        var cursor = clang_getTranslationUnitCursor(translationUnit);
        var filePath = clang_getCursorSpelling(cursor).String();
        return new CLocation
        {
            FileName = Path.GetFileName(filePath),
            FilePath = filePath,
            LineNumber = lineNumber,
            LineColumn = columnNumber
        };
    }

    public static string Name(this CXCursor clangCursor)
    {
        var result = clang_getCursorSpelling(clangCursor).String();
        return SanitizeClangName(result);
    }

    private static string SanitizeClangName(string result)
    {
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

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

        if (result.Contains("*const", StringComparison.InvariantCulture))
        {
            result = result.Replace("*const", "*", StringComparison.InvariantCulture);
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
            CXTypeKind.CXType_LongDouble => true,
            _ => false
        };
    }

    public static bool IsSignedPrimitive(this CXType type)
    {
        if (!IsPrimitive(type))
        {
            return false;
        }

        return type.kind switch
        {
            CXTypeKind.CXType_Char_S => true,
            CXTypeKind.CXType_SChar => true,
            CXTypeKind.CXType_Char_U => true,
            CXTypeKind.CXType_Short => true,
            CXTypeKind.CXType_Int => true,
            CXTypeKind.CXType_Long => true,
            CXTypeKind.CXType_LongLong => true,
            _ => false
        };
    }

    public static bool IsUnsignedPrimitive(this CXType type)
    {
        if (!IsPrimitive(type))
        {
            return false;
        }

        return type.kind switch
        {
            CXTypeKind.CXType_Char_U => true,
            CXTypeKind.CXType_UChar => true,
            CXTypeKind.CXType_UShort => true,
            CXTypeKind.CXType_UInt => true,
            CXTypeKind.CXType_ULong => true,
            CXTypeKind.CXType_ULongLong => true,
            _ => false
        };
    }

    public static string Name(this CXType type, CXCursor? cursor = null)
    {
        var spelling = clang_getTypeSpelling(type).String();
        if (string.IsNullOrEmpty(spelling))
        {
            return string.Empty;
        }

        string result;

        var isPrimitive = type.IsPrimitive();
        var isFunctionPointer = type.kind == CXTypeKind.CXType_FunctionProto;
        if (isPrimitive || isFunctionPointer)
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
                    if (pointeeType.kind == CXTypeKind.CXType_Attributed)
                    {
                        pointeeType = clang_Type_getModifiedType(pointeeType);
                    }

                    var pointeeCursor = clang_getTypeDeclaration(pointeeType);
                    if (pointeeCursor.kind == CXCursorKind.CXCursor_NoDeclFound &&
                        pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        // Function pointer without a declaration, this can happen when the type is field or a param
                        var functionProtoSpelling = clang_getTypeSpelling(pointeeType).String();
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
        var result = clang_getTypeSpelling(clangType).String();
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        result = SanitizeClangName(result);
        return result;
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
