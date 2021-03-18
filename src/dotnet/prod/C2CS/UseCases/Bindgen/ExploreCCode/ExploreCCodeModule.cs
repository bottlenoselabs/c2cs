// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using C2CS.Languages.C;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public class ExploreCCodeModule
    {
        private readonly HashSet<CXCursor> _visitedCursors = new();
        private readonly HashSet<CXType> _visitedTypes = new();

        private readonly ClangMapper _mapper = new();
        private readonly List<ClangFunctionExtern> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangRecord> _records = new();
        private readonly List<ClangOpaqueDataType> _opaqueDataTypes = new();
        private readonly List<ClangForwardDataType> _forwardDataTypes = new();
        private readonly List<ClangSystemDataType> _systemDataTypes = new();
        private readonly List<ClangFunctionPointer> _functionPointers = new();

        public ClangAbstractSyntaxTree ExtractAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            ExploreAbstractSyntaxTree(translationUnit);
            return CollectExtractedData();
        }

        private void ExploreAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            var externalFunctions = GetExternFunctions(translationUnit);
            foreach (var function in externalFunctions)
            {
                VisitCursor(function, function.TranslationUnit.Cursor);
            }
        }

        private ClangAbstractSyntaxTree CollectExtractedData()
        {
            var functionExterns = _functions.ToImmutableArray();
            var functionPointers = _functionPointers.ToImmutableArray();
            var records = _records.ToImmutableArray();
            var enums = _enums.ToImmutableArray();
            var opaqueDataTypes = _opaqueDataTypes.ToImmutableArray();
            var forwardDataTypes = _forwardDataTypes.ToImmutableArray();
            var systemDataTypes = _systemDataTypes.ToImmutableArray();

            var result = new ClangAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                records,
                enums,
                opaqueDataTypes,
                forwardDataTypes,
                systemDataTypes);

            return result;
        }

        private static ImmutableArray<CXCursor> GetExternFunctions(CXTranslationUnit translationUnit)
        {
            var externFunctions = new List<CXCursor>();
            translationUnit.Cursor.VisitChildren((child, _) =>
            {
                if (child.Kind != CXCursorKind.CXCursor_FunctionDecl ||
                    child.Linkage != CXLinkageKind.CXLinkage_External)
                {
                    return;
                }

                if (child.IsInSystem())
                {
                    return;
                }

                externFunctions.Add(child);
            });

            return externFunctions.ToImmutableArray();
        }

        private bool CanVisitCursor(CXCursor cursor)
        {
            if (_visitedCursors.Contains(cursor))
            {
                return false;
            }

            _visitedCursors.Add(cursor);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitCursor(CXCursor cursor, CXCursor cursorParent)
        {
            if (!CanVisitCursor(cursor))
            {
                return;
            }

            if (cursor.IsInSystem())
            {
                VisitSystemDataType(cursor);
            }
            else if (cursor.IsDeclaration)
            {
                VisitDeclaration(cursor, cursorParent);
            }
            else if (cursor.IsReference)
            {
                VisitCursor(cursor.Type.Declaration, cursorParent);
            }
            else if (cursor.IsAttribute)
            {
                // Ignore
            }
            else
            {
                switch (cursor.kind)
                {
                    case CXCursorKind.CXCursor_FirstExpr:
                    case CXCursorKind.CXCursor_IntegerLiteral:
                    case CXCursorKind.CXCursor_BinaryOperator:
                        // Ignore
                        break;
                    default:
                    {
                        var up = new ExploreUnexpectedException();
                        throw up;
                    }
                }
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
        private void VisitType(CXType type, CXCursor parent)
        {
            if (!CanVisitType(type))
            {
                return;
            }

            switch (type.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Attributed:
                    VisitType(type.ModifiedType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    VisitType(type.NamedType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_LValueReference:
                case CX_TypeClass.CX_TypeClass_RValueReference:
                    VisitType(type.PointeeType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Typedef:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_IncompleteArray:
                    VisitType(type.ElementType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                    VisitType(type.ElementType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    // Ignored
                    break;
                case CX_TypeClass.CX_TypeClass_FunctionProto:
                    VisitFunctionProto(parent, parent.SemanticParent);
                    break;
                default:
                    var up = UnsupportedType(type);
                    throw up;
            }
        }

        private void VisitDeclaration(CXCursor cursor, CXCursor cursorParent)
        {
            switch (cursor.DeclKind)
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
                    var up = UnsupportedDeclaration(cursor);
                    throw up;
            }
        }

        private void VisitFunction(CXCursor cursor, CXCursor cursorParent)
        {
            VisitType(cursor.ResultType, cursorParent);

            cursor.VisitChildren(VisitCursor);

            var clangFunction = _mapper.MapFunctionExtern(cursor);
            _functions.Add(clangFunction);
        }

        private void VisitParameter(CXCursor cursor, CXCursor cursorParent)
        {
            VisitType(cursor.Type, cursorParent);

            cursor.VisitChildren(VisitCursor);
        }

        private void VisitEnum(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (underlyingCursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingCursor = cursor.TypedefDeclUnderlyingType.NamedType.Declaration;
            }

            underlyingCursor.VisitChildren((child, cursorParent) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                VisitCursor(child, cursorParent);
            });

            var clangEnum = _mapper.MapEnum(cursor);
            _enums.Add(clangEnum);
        }

        private void VisitEnumConstant(CXCursor cursor, CXCursor cursorParent)
        {
            VisitType(cursorParent.EnumDecl_IntegerType, cursorParent);

            cursor.VisitChildren(VisitCursor);
        }

        private void VisitRecord(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (underlyingCursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingCursor = cursor.TypedefDeclUnderlyingType.NamedType.Declaration;
            }

            underlyingCursor.VisitChildren((child, cursorParent) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                VisitCursor(child, cursorParent);
            });

            if (cursor.IsAnonymous)
            {
                return;
            }

            var record = _mapper.MapRecord(cursor);
            _records.Add(record);
        }

        private void VisitField(CXCursor cursor, CXCursor cursorParent)
        {
            VisitType(cursor.Type, cursorParent);

            cursor.VisitChildren(VisitCursor);
        }

        private void VisitTypedef(CXCursor typedef)
        {
            var underlyingType = typedef.TypedefDeclUnderlyingType.CanonicalType;

            switch (underlyingType.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Pointer:
                    VisitTypedefPointer(typedef, underlyingType.PointeeType);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    VisitForwardDataType(typedef);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                    VisitRecord(typedef);
                    break;
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitEnum(typedef);
                    break;
                default:
                    var up = new ExploreUnexpectedException();
                    throw up;
            }
        }

        private void VisitTypedefPointer(CXCursor typedef, CXType pointeeType)
        {
            if (pointeeType.kind == CXTypeKind.CXType_Void)
            {
                VisitOpaqueDataType(typedef);
            }
            else
            {
                switch (pointeeType.TypeClass)
                {
                    case CX_TypeClass.CX_TypeClass_Elaborated:
                        VisitTypedefElaborated(typedef, pointeeType.NamedType);
                        break;
                    case CX_TypeClass.CX_TypeClass_FunctionProto:
                        VisitTypedefFunctionProto(typedef, pointeeType);
                        break;
                    default:
                        var up = new ExploreUnexpectedException();
                        throw up;
                }
            }
        }

        private void VisitTypedefFunctionProto(CXCursor typedef, CXType functionProtoType)
        {
            if (!CanVisitType(functionProtoType))
            {
                return;
            }

            VisitFunctionProto(typedef, typedef.SemanticParent);
        }

        private void VisitTypedefElaborated(CXCursor typedef, CXType namedType)
        {
            if (namedType.kind == CXTypeKind.CXType_Record)
            {
                var recordType = namedType;
                var childrenCount = 0;
                recordType.Declaration.VisitChildren((_, _) => childrenCount += 1);
                if (childrenCount == 0)
                {
                    VisitOpaqueDataType(typedef);
                }
            }
            else
            {
                var up = new ExploreUnexpectedException();
                throw up;
            }
        }

        private void VisitForwardDataType(CXCursor cursor)
        {
            var forwardType = _mapper.MapForwardDataType(cursor);
            _forwardDataTypes.Add(forwardType);
        }

        private void VisitOpaqueDataType(CXCursor cursor)
        {
            var opaqueDataType = _mapper.MapOpaqueDataType(cursor);
            _opaqueDataTypes.Add(opaqueDataType);
        }

        private void VisitSystemDataType(CXCursor cursor)
        {
            if (cursor.Spelling.CString == "FILE")
            {
                return;
            }

            var systemDataType = _mapper.MapSystemDataType(cursor);
            _systemDataTypes.Add(systemDataType);
        }

        private void VisitFunctionProto(CXCursor cursor, CXCursor cursorParent)
        {
            var resultType = GetFunctionProtoResultType(cursor);
            VisitType(resultType, cursorParent);

            cursor.VisitChildren(VisitCursor);

            var clangFunctionPointer = _mapper.MapFunctionPointer(cursor);
            _functionPointers.Add(clangFunctionPointer);
        }

        private static CXType GetFunctionProtoResultType(CXCursor cursor)
        {
            CXType result;

            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang.getTypedefDeclUnderlyingType(cursor);
                var underlyingTypeCanonical = clang.getCanonicalType(underlyingType);
                var pointeeType = clang.getPointeeType(underlyingTypeCanonical);
                result = clang.getResultType(pointeeType);
            }
            else
            {
                result = clang.getCursorResultType(cursor);
            }

            if (result.kind != CXTypeKind.CXType_Invalid)
            {
                return result;
            }

            var up = new ExploreUnexpectedException();
            throw up;
        }

        private static Exception UnsupportedDeclaration(CXCursor declaration)
        {
            return new NotImplementedException(
                $"Not yet supported declaration kind `{declaration.DeclKind}`: '{declaration}'.");
        }

        private static Exception UnsupportedType(CXType type)
        {
            return new NotImplementedException($"Not yet supported type class `{type.TypeClass}`: '{type}'.");
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
