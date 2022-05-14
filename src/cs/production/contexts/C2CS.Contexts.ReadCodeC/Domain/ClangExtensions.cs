// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using C2CS.Contexts.ReadCodeC.Data;
using C2CS.Contexts.ReadCodeC.Data.Model;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain;

public static unsafe class ClangExtensions
{
    public delegate bool VisitPredicate(CXCursor child, CXCursor parent);

    private static VisitInstance[] _visitInstances = Array.Empty<VisitInstance>();
    private static int _visitsCount;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate CXChildVisitResult DelegateVisit(CXCursor child, CXCursor parent, CXClientData clientData);

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

        CXCursorVisitor visitor;
        DelegateVisit visitorDelegate = Visitor;
        visitor.Pointer = Marshal.GetFunctionPointerForDelegate(visitorDelegate);
        clang_visitChildren(cursor, visitor, clientData);

        Interlocked.Decrement(ref _visitsCount);

        var result = visitData.NodeBuilder.ToImmutable();
        return result;
    }

    private static CXChildVisitResult Visitor(CXCursor child, CXCursor parent, CXClientData clientData)
    {
        try
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
        catch (Exception e)
        {
            Console.WriteLine(e);
            return CXChildVisitResult.CXChildVisit_Break;
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
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        if (result.Contains("struct "))
        {
            result = result.Replace("struct ", string.Empty);
        }

        if (result.Contains("union "))
        {
            result = result.Replace("union ", string.Empty);
        }

        if (result.Contains("enum "))
        {
            result = result.Replace("enum ", string.Empty);
        }

        if (result.Contains("const "))
        {
            result = result.Replace("const ", string.Empty);
        }

        if (result.Contains("*const"))
        {
            result = result.Replace("*const", "*");
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

    public static string Name(this CXType type, CXCursor? cursor = null)
    {
        var spellingC = clang_getTypeSpelling(type);
        string spelling = clang_getCString(spellingC);
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
