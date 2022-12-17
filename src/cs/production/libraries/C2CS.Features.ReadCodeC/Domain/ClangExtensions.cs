// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Text;
using C2CS.Data.C.Model;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain;

public static unsafe class ClangExtensions
{
    public delegate bool VisitChildPredicate(CXCursor child, CXCursor parent);

    private static VisitChildInstance[] _visitChildInstances = new VisitChildInstance[512];
    private static int _visitChildCount;
    private static VisitFieldsInstance[] _visitFieldsInstances = new VisitFieldsInstance[512];
    private static int _visitFieldsCount;

    private static readonly CXCursorVisitor VisitorChild;
    private static readonly CXCursorVisitor VisitorAttribute;
    private static readonly CXFieldVisitor VisitorField;
    private static readonly VisitChildPredicate EmptyPredicate = static (_, _) => true;

    static ClangExtensions()
    {
        VisitorChild.Pointer = &VisitChild;
        VisitorAttribute.Pointer = &VisitAttribute;
        VisitorField.Pointer = &VisitField;
    }

    public static ImmutableArray<CXCursor> GetDescendents(
        this CXCursor cursor, VisitChildPredicate? predicate = null, bool mustBeFromSameFile = true)
    {
        var predicate2 = predicate ?? EmptyPredicate;
        var visitData = new VisitChildInstance(predicate2, mustBeFromSameFile);
        var visitsCount = Interlocked.Increment(ref _visitChildCount);
        if (visitsCount > _visitChildInstances.Length)
        {
            Array.Resize(ref _visitChildInstances, visitsCount * 2);
        }

        _visitChildInstances[visitsCount - 1] = visitData;

        var clientData = default(CXClientData);
        clientData.Data = (void*)_visitChildCount;
        clang_visitChildren(cursor, VisitorChild, clientData);

        Interlocked.Decrement(ref _visitChildCount);
        var result = visitData.CursorBuilder.ToImmutable();
        visitData.CursorBuilder.Clear();
        return result;
    }

    [UnmanagedCallersOnly]
    private static CXChildVisitResult VisitChild(CXCursor child, CXCursor parent, CXClientData clientData)
    {
        var index = (int)clientData.Data;
        var data = _visitChildInstances[index - 1];

        if (data.MustBeFromSameFile)
        {
            var location = clang_getCursorLocation(child);
            var isFromMainFile = clang_Location_isFromMainFile(location) > 0;
            if (!isFromMainFile)
            {
                return CXChildVisitResult.CXChildVisit_Continue;
            }
        }

        var result = data.Predicate(child, parent);
        if (!result)
        {
            return CXChildVisitResult.CXChildVisit_Continue;
        }

        data.CursorBuilder.Add(child);

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    public static ImmutableArray<CXCursor> GetAttributes(
        this CXCursor cursor, VisitChildPredicate? predicate = null)
    {
        var hasAttributes = clang_Cursor_hasAttrs(cursor) > 0;
        if (!hasAttributes)
        {
            return ImmutableArray<CXCursor>.Empty;
        }

        var predicate2 = predicate ?? EmptyPredicate;
        var visitData = new VisitChildInstance(predicate2, false);
        var visitsCount = Interlocked.Increment(ref _visitChildCount);
        if (visitsCount > _visitChildInstances.Length)
        {
            Array.Resize(ref _visitChildInstances, visitsCount * 2);
        }

        _visitChildInstances[visitsCount - 1] = visitData;

        var clientData = default(CXClientData);
        clientData.Data = (void*)_visitChildCount;
        clang_visitChildren(cursor, VisitorAttribute, clientData);

        Interlocked.Decrement(ref _visitChildCount);
        var result = visitData.CursorBuilder.ToImmutable();
        visitData.CursorBuilder.Clear();
        return result;
    }

    [UnmanagedCallersOnly]
    private static CXChildVisitResult VisitAttribute(CXCursor child, CXCursor parent, CXClientData clientData)
    {
        var index = (int)clientData.Data;
        var data = _visitChildInstances[index - 1];

        /*var isAttribute = clang_isAttribute(child.kind) > 0;
        if (!isAttribute)
        {
            return CXChildVisitResult.CXChildVisit_Continue;
        }*/

        var result = data.Predicate(child, parent);
        if (!result)
        {
            return CXChildVisitResult.CXChildVisit_Continue;
        }

        data.CursorBuilder.Add(child);

        return CXChildVisitResult.CXChildVisit_Continue;
    }

    public static ImmutableArray<CXCursor> GetFields(this CXType type)
    {
#pragma warning disable SA1129
        var visitData = new VisitFieldsInstance();
#pragma warning restore SA1129
        var visitsCount = Interlocked.Increment(ref _visitFieldsCount);
        if (visitsCount > _visitFieldsInstances.Length)
        {
            Array.Resize(ref _visitFieldsInstances, visitsCount * 2);
        }

        _visitFieldsInstances[visitsCount - 1] = visitData;

        var clientData = default(CXClientData);
        clientData.Data = (void*)_visitFieldsCount;
        clang_Type_visitFields(type, VisitorField, clientData);

        Interlocked.Decrement(ref _visitFieldsCount);
        var result = visitData.CursorBuilder.ToImmutable();
        visitData.CursorBuilder.Clear();
        return result;
    }

    [UnmanagedCallersOnly]
    private static CXVisitorResult VisitField(CXCursor cursor, CXClientData clientData)
    {
        var index = (int)clientData.Data;
        var data = _visitFieldsInstances[index - 1];
        data.CursorBuilder.Add(cursor);
        return CXVisitorResult.CXVisit_Continue;
    }

    public static string String(this CXString cxString)
    {
        var cString = clang_getCString(cxString);
        var result = Marshal.PtrToStringAnsi(cString)!;
        clang_disposeString(cxString);
        return result;
    }

    public static CLocation GetLocation(
        this CXCursor cursor,
        CXType? type = null,
        ImmutableDictionary<string, string>? linkedFileDirectoryPaths = null,
        ImmutableArray<string>? userIncludeDirectories = null)
    {
        if (cursor.kind == CXCursorKind.CXCursor_TranslationUnit)
        {
            return CLocation.NoLocation;
        }

        if (cursor.kind != CXCursorKind.CXCursor_FieldDecl && type != null)
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

        var locationSource = clang_getCursorLocation(cursor);
        var translationUnit = clang_Cursor_getTranslationUnit(cursor);
        var location = GetLocation(locationSource, translationUnit, linkedFileDirectoryPaths, userIncludeDirectories);
        return location;
    }

    private static CLocation GetLocation(
        CXSourceLocation locationSource,
        CXTranslationUnit? translationUnit = null,
        ImmutableDictionary<string, string>? linkedFileDirectoryPaths = null,
        ImmutableArray<string>? userIncludeDirectories = null)
    {
        CXFile file;
        uint lineNumber;
        uint columnNumber;
        uint offset;

        clang_getFileLocation(locationSource, &file, &lineNumber, &columnNumber, &offset);

        var handle = (IntPtr)file.Data;
        if (handle == IntPtr.Zero)
        {
            if (!translationUnit.HasValue)
            {
                return CLocation.NoLocation;
            }

            return LocationInTranslationUnit(translationUnit.Value, (int)lineNumber, (int)columnNumber);
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

        if (linkedFileDirectoryPaths != null)
        {
            foreach (var (linkedDirectory, targetDirectory) in linkedFileDirectoryPaths)
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
        CXTranslationUnit translationUnit,
        int lineNumber,
        int columnNumber)
    {
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

    public static string GetCode(
        this CXCursor cursor,
        StringBuilder? stringBuilder = null)
    {
        if (stringBuilder == null)
        {
            stringBuilder = new StringBuilder();
        }
        else
        {
            stringBuilder.Clear();
        }

        var translationUnit = clang_Cursor_getTranslationUnit(cursor);
        var cursorExtent = clang_getCursorExtent(cursor);
        var tokens = (CXToken*)0;
        uint tokensCount = 0;
        clang_tokenize(translationUnit, cursorExtent, &tokens, &tokensCount);
        for (var i = 0; i < tokensCount; i++)
        {
            var tokenString = clang_getTokenSpelling(translationUnit, tokens[i]).String();
            stringBuilder.Append(tokenString);
        }

        clang_disposeTokens(translationUnit, tokens, tokensCount);
        var result = stringBuilder.ToString();
        stringBuilder.Clear();
        return result;
    }

    public static bool IsConst(this CXCursor cursor)
    {
        var type = clang_getCursorType(cursor);
        return IsConst(type);
    }

    public static bool IsConst(this CXType type)
    {
        var isConstQualifiedType = clang_isConstQualifiedType(type) > 0;
        return isConstQualifiedType;
    }

    private readonly struct VisitChildInstance
    {
        public readonly VisitChildPredicate Predicate;
        public readonly ImmutableArray<CXCursor>.Builder CursorBuilder;
        public readonly bool MustBeFromSameFile;

        public VisitChildInstance(VisitChildPredicate predicate, bool mustBeFromSameFile)
        {
            Predicate = predicate;
            CursorBuilder = ImmutableArray.CreateBuilder<CXCursor>();
            MustBeFromSameFile = mustBeFromSameFile;
        }
    }

    private readonly struct VisitFieldsInstance
    {
        public readonly ImmutableArray<CXCursor>.Builder CursorBuilder;

        public VisitFieldsInstance()
        {
            CursorBuilder = ImmutableArray.CreateBuilder<CXCursor>();
        }
    }
}
