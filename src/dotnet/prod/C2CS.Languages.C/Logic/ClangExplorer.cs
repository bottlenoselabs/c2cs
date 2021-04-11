// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ClangSharp.Interop;

namespace C2CS.Languages.C
{
    public class ClangExplorer
    {
        private readonly HashSet<CXCursor> _visitedCursors = new();
        private readonly HashSet<CXType> _visitedTypes = new();

        private readonly ClangMapper _mapper = new();
        private readonly List<ClangFunctionExtern> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangRecord> _records = new();
        private readonly List<ClangOpaqueDataType> _opaqueDataTypes = new();
        private readonly List<ClangOpaquePointer> _opaquePointers = new();
        private readonly List<ClangAliasDataType> _aliasDataTypes = new();
        private readonly List<ClangFunctionPointer> _functionPointers = new();

        public ClangAbstractSyntaxTree ExtractAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            ExploreAbstractSyntaxTree(translationUnit);
            return CollectExtractedData();
        }

        private void ExploreAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            CXCursor translationUnitCursor;
            unsafe
            {
                translationUnitCursor = clang.getTranslationUnitCursor(translationUnit);
            }

            var externalFunctions = GetExternFunctions(translationUnit);
            foreach (var function in externalFunctions)
            {
                VisitCursor(function, translationUnitCursor);
            }
        }

        private ClangAbstractSyntaxTree CollectExtractedData()
        {
            _functions.Sort();
            _functionPointers.Sort();
            _records.Sort();
            _enums.Sort();
            _opaqueDataTypes.Sort();
            _opaquePointers.Sort();
            _aliasDataTypes.Sort();

            var functionExterns = _functions.ToImmutableArray();
            var functionPointers = _functionPointers.ToImmutableArray();
            var records = _records.ToImmutableArray();
            var enums = _enums.ToImmutableArray();
            var opaqueDataTypes = _opaqueDataTypes.ToImmutableArray();
            var opaquePointers = _opaquePointers.ToImmutableArray();
            var aliasDataTypes = _aliasDataTypes.ToImmutableArray();

            var result = new ClangAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                records,
                enums,
                opaqueDataTypes,
                opaquePointers,
                aliasDataTypes);

            return result;
        }

        private static ImmutableArray<CXCursor> GetExternFunctions(CXTranslationUnit translationUnit)
        {
            var externFunctions = new List<CXCursor>();
            translationUnit.Cursor.VisitChildren((child, _) =>
            {
                var kind = child.kind;
                var isFunctionDeclaration = kind == CXCursorKind.CXCursor_FunctionDecl;

                if (!isFunctionDeclaration)
                {
                    return;
                }

                var linkage = clang.getCursorLinkage(child);
                var isExternallyLinked = linkage == CXLinkageKind.CXLinkage_External;
                if (!isExternallyLinked)
                {
                    return;
                }

                var isSystemCursor = child.IsSystemCursor();
                if (isSystemCursor)
                {
                    return;
                }

                externFunctions.Add(child);
            });

            return externFunctions.ToImmutableArray();
        }

        private bool IgnoreCursor(CXCursor cursor)
        {
            var canVisitCursor = CanVisitCursor(cursor);
            if (!canVisitCursor)
            {
                return true;
            }

            var isSystemCursor = cursor.IsSystemCursor();
            if (isSystemCursor)
            {
                return true;
            }

            var isCursorAttribute = clang.isAttribute(cursor.kind) > 0U;
            if (isCursorAttribute)
            {
                return true;
            }

            var cursorKind = cursor.kind;
            if (cursorKind == CXCursorKind.CXCursor_IntegerLiteral)
            {
                return true;
            }

            return false;
        }

        private bool CanVisitCursor(CXCursor cursor)
        {
            var alreadyVisitedCursor = _visitedCursors.Contains(cursor);
            if (alreadyVisitedCursor)
            {
                return false;
            }

            _visitedCursors.Add(cursor);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitCursor(CXCursor cursor, CXCursor cursorParent)
        {
            var ignoreCursor = IgnoreCursor(cursor);
            if (ignoreCursor)
            {
                return;
            }

            var cursorKind = cursor.kind;
            var isDeclaration = clang.isDeclaration(cursorKind) > 0U;
            var isReference = clang.isReference(cursorKind) > 0U;

            if (isDeclaration)
            {
                VisitDeclaration(cursor, cursorParent);
            }
            else if (isReference)
            {
                var cursorType = clang.getCursorType(cursor);
                var cursorTypeDeclaration = clang.getTypeDeclaration(cursorType);
                VisitCursor(cursorTypeDeclaration, cursorParent);
            }
            else if (cursorKind == CXCursorKind.CXCursor_DeclRefExpr)
            {
                var cursorReferenced = clang.getCursorReferenced(cursor);
                VisitCursor(cursorReferenced, cursorParent);
            }
            else
            {
                var up = new ExploreUnexpectedException();
                throw up;
            }
        }

        private bool CanVisitType(CXType type)
        {
            if (_visitedTypes.Contains(type))
            {
                return false;
            }

            _visitedTypes.Add(type);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitType(CXType type, CXCursor cursor, CXCursor parentCursor)
        {
            if (!CanVisitType(type))
            {
                return;
            }

            var typeClass = clangsharp.Type_getTypeClass(type);
            switch (typeClass)
            {
                case CX_TypeClass.CX_TypeClass_Attributed:
                    var modifiedType = clang.Type_getModifiedType(type);
                    VisitType(modifiedType, cursor, parentCursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    var namedType = clang.Type_getNamedType(type);
                    VisitType(namedType, cursor, parentCursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_LValueReference:
                case CX_TypeClass.CX_TypeClass_RValueReference:
                    var pointeeType = clang.getPointeeType(type);
                    VisitType(pointeeType, cursor, parentCursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                case CX_TypeClass.CX_TypeClass_Enum:
                case CX_TypeClass.CX_TypeClass_Typedef:
                    var declaration = clang.getTypeDeclaration(type);
                    VisitCursor(declaration, parentCursor);
                    break;
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                case CX_TypeClass.CX_TypeClass_IncompleteArray:
                    var elementType = clang.getElementType(type);
                    VisitType(elementType, cursor, parentCursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    // Ignored
                    break;
                case CX_TypeClass.CX_TypeClass_FunctionProto:
                    VisitFunctionProto(cursor, parentCursor);
                    break;
                default:
                    var up = new ExploreUnexpectedException();
                    throw up;
            }
        }

        private void VisitDeclaration(CXCursor cursor, CXCursor cursorParent)
        {
            var cursorType = clang.getCursorType(cursor);
            var sizeOf = clang.Type_getSizeOf(cursorType);
            if (sizeOf == -2)
            {
                // -2 = CXTypeLayoutError_Incomplete
                VisitOpaqueDataType(cursor);
            }
            else
            {
                var declarationKind = clangsharp.Cursor_getDeclKind(cursor);
                switch (declarationKind)
                {
                    case CX_DeclKind.CX_DeclKind_Enum:
                        VisitEnum(cursor);
                        break;
                    case CX_DeclKind.CX_DeclKind_EnumConstant:
                        VisitEnumConstant(cursor, cursorParent);
                        break;
                    case CX_DeclKind.CX_DeclKind_Record:
                        VisitRecord(cursor);
                        break;
                    case CX_DeclKind.CX_DeclKind_Field:
                        VisitField(cursor, cursorParent);
                        break;
                    case CX_DeclKind.CX_DeclKind_Typedef:
                        VisitTypedef(cursor);
                        break;
                    case CX_DeclKind.CX_DeclKind_Function:
                        VisitFunction(cursor, cursorParent);
                        break;
                    case CX_DeclKind.CX_DeclKind_ParmVar:
                        VisitParameter(cursor, cursorParent);
                        break;
                    default:
                        var up = new ExploreUnexpectedException();
                        throw up;
                }
            }
        }

        private void VisitFunction(CXCursor cursor, CXCursor cursorParent)
        {
            var resultType = clang.getCursorResultType(cursor);
            VisitType(resultType, cursor, cursorParent);

            cursor.VisitChildren(VisitCursor);

            var clangFunction = _mapper.MapFunctionExtern(cursor);
            _functions.Add(clangFunction);
        }

        private void VisitParameter(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang.getCursorType(cursor);
            VisitType(type, cursor, cursorParent);

            cursor.VisitChildren(VisitCursor);
        }

        private void VisitEnum(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang.getTypedefDeclUnderlyingType(cursor);
                var namedType = clang.Type_getNamedType(underlyingType);
                underlyingCursor = clang.getTypeDeclaration(namedType);
            }

            underlyingCursor.VisitChildren((child, parent) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                VisitCursor(child, parent);
            });

            var clangEnum = _mapper.MapEnum(cursor);
            _enums.Add(clangEnum);
        }

        private void VisitEnumConstant(CXCursor cursor, CXCursor cursorParent)
        {
            CXCursor cursorEnum;

            var cursorParentKind = cursor.kind;
            if (cursorParentKind != CXCursorKind.CXCursor_EnumDecl)
            {
                cursorEnum = clang.getCursorSemanticParent(cursor);
            }
            else
            {
                cursorEnum = cursorParent;
            }

            var integerType = clang.getEnumDeclIntegerType(cursorEnum);
            VisitType(integerType, cursor, cursorParent);
        }

        private void VisitRecord(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang.getTypedefDeclUnderlyingType(cursor);
                var namedType = clang.Type_getNamedType(underlyingType);
                underlyingCursor = clang.getTypeDeclaration(namedType);
            }

            underlyingCursor.VisitChildren((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                VisitCursor(child, cursor);
            });

            var isAnonymousCursor = clang.Cursor_isAnonymous(cursor) > 0U;
            if (isAnonymousCursor)
            {
                return;
            }

            var record = _mapper.MapRecord(cursor);
            _records.Add(record);
        }

        private void VisitField(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang.getCursorType(cursor);
            VisitType(type, cursor, cursorParent);

            cursor.VisitChildren(VisitCursor);
        }

        private void VisitTypedef(CXCursor cursor)
        {
            var underlyingType = clang.getTypedefDeclUnderlyingType(cursor);
            var underlyingTypeCanonical = clang.getCanonicalType(underlyingType);
            var typeClass = clangsharp.Type_getTypeClass(underlyingTypeCanonical);

            switch (typeClass)
            {
                case CX_TypeClass.CX_TypeClass_Pointer:
                    var pointeeType = clang.getPointeeType(underlyingTypeCanonical);
                    VisitTypedefPointer(cursor, pointeeType);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    VisitAliasDataType(cursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                    VisitRecord(cursor);
                    break;
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitEnum(cursor);
                    break;
                default:
                    var up = new ExploreUnexpectedException();
                    throw up;
            }
        }

        private void VisitTypedefPointer(CXCursor cursor, CXType pointeeType)
        {
            var kind = pointeeType.kind;
            var pointeeTypeSizeOf = pointeeType.SizeOf;

            if (kind == CXTypeKind.CXType_Void || pointeeTypeSizeOf == -2)
            {
                VisitOpaquePointer(cursor);
            }
            else
            {
                var pointeeTypeClass = clangsharp.Type_getTypeClass(pointeeType);
                switch (pointeeTypeClass)
                {
                    case CX_TypeClass.CX_TypeClass_Record:
                        VisitRecord(cursor);
                        break;
                    case CX_TypeClass.CX_TypeClass_FunctionProto:
                        VisitTypedefFunctionProto(cursor, pointeeType);
                        break;
                    default:
                        var up = new ExploreUnexpectedException();
                        throw up;
                }
            }
        }

        private void VisitTypedefFunctionProto(CXCursor cursor, CXType functionProtoType)
        {
            if (!CanVisitType(functionProtoType))
            {
                return;
            }

            var cursorParent = clang.getCursorSemanticParent(cursor);
            VisitFunctionProto(cursor, cursorParent);
        }

        private void VisitAliasDataType(CXCursor cursor)
        {
            var aliasDataType = _mapper.MapAliasDataType(cursor);
            _aliasDataTypes.Add(aliasDataType);
        }

        private void VisitOpaquePointer(CXCursor cursor)
        {
            var opaquePointerType = _mapper.MapOpaquePointer(cursor);
            _opaquePointers.Add(opaquePointerType);
        }

        private void VisitOpaqueDataType(CXCursor cursor)
        {
            var opaqueDataType = _mapper.MapOpaqueDataType(cursor);
            _opaqueDataTypes.Add(opaqueDataType);
        }

        private void VisitFunctionProto(CXCursor cursor, CXCursor cursorParent)
        {
            var resultType = GetFunctionProtoResultType(cursor);
            VisitType(resultType, cursor, cursorParent);

            cursor.VisitChildren(VisitCursor);

            var clangFunctionPointer = _mapper.MapFunctionPointer(cursor, cursorParent);
            _functionPointers.Add(clangFunctionPointer);
        }

        private static CXType GetFunctionProtoResultType(CXCursor cursor)
        {
            CXType result;

            var cursorKind = cursor.kind;
            if (cursorKind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang.getTypedefDeclUnderlyingType(cursor);
                var underlyingTypeCanonical = clang.getCanonicalType(underlyingType);
                var pointeeType = clang.getPointeeType(underlyingTypeCanonical);
                result = clang.getResultType(pointeeType);
            }
            else if (cursorKind == CXCursorKind.CXCursor_ParmDecl ||
                     cursorKind == CXCursorKind.CXCursor_FieldDecl)
            {
                var type = clang.getCursorType(cursor);
                var typeCanonical = clang.getCanonicalType(type);
                var pointeeType = clang.getPointeeType(typeCanonical);
                result = clang.getResultType(pointeeType);
            }
            else
            {
                result = clang.getCursorResultType(cursor);
            }

            // ReSharper disable once InvertIf
            if (result.kind == CXTypeKind.CXType_Invalid)
            {
                var up = new ExploreUnexpectedException();
                throw up;
            }

            return result;
        }

        private class ExploreUnexpectedException : Exception
        {
            public ExploreUnexpectedException()
                : base("The header file has unexpected scenarios when exploring. Please create an issue on GitHub with the stack trace along with the header file.")
            {
            }
        }
    }
}
