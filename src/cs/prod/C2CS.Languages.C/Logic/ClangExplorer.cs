// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangExplorer
    {
        private readonly ClangMapper _mapper = new();
        private readonly HashSet<string> _visitedTypeNames = new();
        private ImmutableHashSet<string> _opaqueTypeNames = null!;
        private bool _printAbstractSyntaxTree;

        private readonly List<ClangFunctionExtern> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangRecord> _records = new();
        private readonly List<ClangOpaqueType> _opaqueDataTypes = new();
        private readonly List<ClangTypedef> _typedefs = new();
        private readonly List<ClangFunctionPointer> _functionPointers = new();
        private readonly List<ClangVariable> _variables = new();

        public ClangAbstractSyntaxTree VisitTranslationUnit(
            CXTranslationUnit translationUnit,
            bool printAbstractSyntaxTree,
            IEnumerable<string> opaqueTypeNames)
        {
            _printAbstractSyntaxTree = printAbstractSyntaxTree;
            _opaqueTypeNames = opaqueTypeNames.ToImmutableHashSet();
            var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);
            var externs = GetExterns(translationUnitCursor);
            VisitExterns(externs);
            return CollectResults();
        }

        private ClangAbstractSyntaxTree CollectResults()
        {
            var functionExterns = _functions.ToImmutableArray();
            var functionPointers = _functionPointers.ToImmutableArray();
            var records = _records.ToImmutableArray();
            var enums = _enums.ToImmutableArray();
            var opaqueDataTypes = _opaqueDataTypes.ToImmutableArray();
            var typedefs = _typedefs.ToImmutableArray();
            var variables = _variables.ToImmutableArray();

            var result = new ClangAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                records,
                enums,
                opaqueDataTypes,
                typedefs,
                variables);

            return result;
        }

        private static ImmutableArray<ClangExtensions.ClangVisitNode> GetExterns(CXCursor translationUnitCursor)
        {
            var nodes = translationUnitCursor.GetDescendents(IsExternCursor);
            return nodes;
        }

        private static bool IsExternCursor(CXCursor cursor, CXCursor cursorParent)
        {
            var kind = clang_getCursorKind(cursor);
            if (kind != CXCursorKind.CXCursor_FunctionDecl && kind != CXCursorKind.CXCursor_VarDecl)
            {
                return false;
            }

            var linkage = clang_getCursorLinkage(cursor);
            var isExternallyLinked = linkage == CXLinkageKind.CXLinkage_External;
            if (!isExternallyLinked)
            {
                return false;
            }

            var isSystemCursor = cursor.IsSystem();
            return !isSystemCursor;
        }

        private void VisitExterns(ImmutableArray<ClangExtensions.ClangVisitNode> nodes)
        {
            foreach (var node in nodes)
            {
                var type = clang_getCursorType(node.Cursor);

                switch (node.Cursor.kind)
                {
                    case CXCursorKind.CXCursor_FunctionDecl:
                        VisitFunctionExtern(node.Cursor, node.CursorParent, type, 1);
                        break;
                    case CXCursorKind.CXCursor_VarDecl:
                        VisitVariableExtern(node.Cursor, node.CursorParent, type, 1);
                        break;
                    default:
                        var up = new ClangExplorerException($"Unexpected extern kind '{node.Cursor.kind}'.");
                        throw up;
                }
            }
        }

        private void VisitVariableExtern(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            VisitType(ClangNodeKind.VariableExtern, type, cursor, cursorParent, depth + 1);

            var variable = _mapper.MapVariableExtern(cursor, cursorParent, type);
            _variables.Add(variable);
            LogVisit(variable);
        }

        private void VisitFunctionExtern(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var resultType = clang_getCursorResultType(cursor);
            VisitFunctionExternResult(cursor, cursorParent, resultType, depth + 1);
            VisitFunctionExternParameters(cursor, depth + 1);

            var functionExtern = _mapper.MapFunctionExtern(cursor, cursorParent, type);
            _functions.Add(functionExtern);
            LogVisit(functionExtern);
        }

        private void VisitFunctionExternResult(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var typeCursor = clang_getTypeDeclaration(type);

            CXCursor typeCursorParent;
            if (typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                typeCursor = cursor;
                typeCursorParent = cursorParent;
            }
            else
            {
                typeCursorParent = clang_getCursorSemanticParent(typeCursor);
            }

            VisitType(ClangNodeKind.FunctionExternResult, type, typeCursor, typeCursorParent, depth + 1);
        }

        private void VisitFunctionExternParameters(CXCursor cursor, int depth)
        {
            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var type = clang_getCursorType(cursor);
                VisitFunctionExternParameter(node.Cursor, node.CursorParent, type, depth);
            }
        }

        private void VisitFunctionExternParameter(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            VisitType(ClangNodeKind.FunctionExternParameter, type, cursor, cursorParent, depth + 1);
        }

        private void VisitEnum(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var cursorEnum = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var namedType = clang_Type_getNamedType(underlyingType);
                cursorEnum = clang_getTypeDeclaration(namedType);
            }

            var integerType = clang_getEnumDeclIntegerType(cursorEnum);

            var @enum = _mapper.MapEnum(cursor, cursorParent, integerType);
            _enums.Add(@enum);
            LogVisit(@enum);
        }

        private void VisitRecord(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);

            var underlyingType = type;
            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var namedType = clang_Type_getNamedType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(namedType);
            }

            var typeDeclaration = clang_getTypeDeclaration(type);
            var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;

            if (isAnonymous)
            {
                underlyingCursor = typeDeclaration;
            }

            VisitRecordFields(underlyingCursor, cursor, depth + 1);

            if (isAnonymous)
            {
                return;
            }

            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var typedef = _mapper.MapTypedef(cursor, cursorParent, underlyingType);
                _typedefs.Add(typedef);
                LogVisit(typedef);
            }
            else
            {
                var record = _mapper.MapRecord(cursor, cursorParent);
                _records.Add(record);
                LogVisit(record);
            }
        }

        private void VisitRecordFields(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_FieldDecl);

            foreach (var node in nodes)
            {
                VisitRecordField(node.Cursor, cursorParent, depth);
            }
        }

        private void VisitRecordField(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);
            VisitType(ClangNodeKind.RecordField, type, cursor, cursorParent, depth + 1);
        }

        private void VisitOpaqueType(CXCursor cursor, CXCursor cursorParent)
        {
            var opaqueType = _mapper.MapOpaqueDataType(cursor, cursorParent);
            _opaqueDataTypes.Add(opaqueType);
            LogVisit(opaqueType);
        }

        private void VisitFunctionPointer(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var resultType = clang_getResultType(type);
            VisitType(ClangNodeKind.FunctionPointerResult, resultType, cursor, cursorParent, depth);

            VisitFunctionPointerParameters(cursor, depth + 1);

            var functionPointer = _mapper.MapFunctionPointer(cursor, cursorParent);
            _functionPointers.Add(functionPointer);
            LogVisit(functionPointer);
        }

        private void VisitFunctionPointerParameters(CXCursor cursor, int depth)
        {
            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                VisitFunctionPointerParameter(node.Cursor, node.CursorParent, depth);
            }
        }

        private void VisitFunctionPointerParameter(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);
            VisitType(ClangNodeKind.FunctionPointerParameter, type, cursor, cursorParent, depth + 1);
        }

        private void VisitType(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var typeName = ClangMapper.GetTypeName(cursor, type);
            var alreadyVisited = TypeIsAlreadyVisited(typeName);
            if (alreadyVisited)
            {
                return;
            }

            var isOverrideOpaqueType = TypeIsUserOverrideOpaqueType(typeName);
            if (isOverrideOpaqueType)
            {
                VisitOpaqueType(cursor, cursorParent);
                return;
            }

            var systemIgnored = TypeIsSystemIgnored(cursor, type, typeName);
            if (systemIgnored)
            {
                if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
                {
                    VisitRecord(cursor, cursorParent, depth);
                }

                return;
            }

            switch (type.kind)
            {
                case CXTypeKind.CXType_Record:
                    VisitRecord(cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Enum:
                    VisitEnum(cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_FunctionProto:
                    VisitFunctionPointer(cursor, cursorParent, type, depth);
                    break;
                case CXTypeKind.CXType_Typedef:
                    VisitTypeTypedef(nodeKind, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Attributed:
                    VisitTypeAttributed(nodeKind, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Elaborated:
                    VisitTypeElaborated(nodeKind, type, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Pointer:
                    VisitTypePointer(nodeKind, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_IncompleteArray:
                    VisitTypeArray(nodeKind, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Void:
                case CXTypeKind.CXType_Bool:
                case CXTypeKind.CXType_Char_S:
                case CXTypeKind.CXType_Char_U:
                case CXTypeKind.CXType_UChar:
                case CXTypeKind.CXType_UShort:
                case CXTypeKind.CXType_UInt:
                case CXTypeKind.CXType_ULong:
                case CXTypeKind.CXType_ULongLong:
                case CXTypeKind.CXType_Short:
                case CXTypeKind.CXType_Int:
                case CXTypeKind.CXType_Long:
                case CXTypeKind.CXType_LongLong:
                case CXTypeKind.CXType_Float:
                case CXTypeKind.CXType_Double:
                    VisitTypeBuiltin(nodeKind, type, cursor, cursorParent, depth);
                    break;
                default:
                    var up = new ClangExplorerException($"Unexpected type kind '{type.kind}'.");
                    throw up;
            }
        }

        private void VisitTypeAttributed(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var modifiedType = clang_Type_getModifiedType(type);
            VisitType(nodeKind, modifiedType, cursor, cursorParent, depth);
        }

        private bool TypeIsAlreadyVisited(string typeName)
        {
            var result = _visitedTypeNames.Contains(typeName);
            if (!result)
            {
                _visitedTypeNames.Add(typeName);
            }

            return result;
        }

        private void VisitTypeElaborated(
            ClangNodeKind nodeKind, CXType type, CXCursor cursorParent, int depth)
        {
            var namedType = clang_Type_getNamedType(type);
            var namedCursor = clang_getTypeDeclaration(namedType);
            VisitType(nodeKind, namedType, namedCursor, cursorParent, depth);
        }

        private void VisitTypeArray(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var elementType = clang_getElementType(type);
            VisitType(nodeKind, elementType, cursor, cursorParent, depth);
        }

        private void VisitTypePointer(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var pointeeType = clang_getPointeeType(type);
            var pointeeCursor = clang_getTypeDeclaration(pointeeType);
            var pointeeCursorParent = cursor;
            if (pointeeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                pointeeCursor = cursor;
                pointeeCursorParent = cursorParent;
            }

            if (pointeeType.kind == CXTypeKind.CXType_Void)
            {
                return;
            }

            var pointeeTypeSizeOf = clang_Type_getSizeOf(pointeeType);
            if (pointeeTypeSizeOf == -2)
            {
                throw new NotImplementedException();
            }

            VisitType(nodeKind, pointeeType, pointeeCursor, pointeeCursorParent, depth);
        }

        private void VisitTypeTypedef(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var typedef = clang_getTypeDeclaration(type);
            var underlyingType = clang_getTypedefDeclUnderlyingType(typedef);

            if (underlyingType.kind == CXTypeKind.CXType_Elaborated)
            {
                underlyingType = clang_Type_getNamedType(underlyingType);
            }

            VisitType(nodeKind, underlyingType, cursor, cursorParent, depth + 1);
        }

        private void VisitTypeBuiltin(
            ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            if (nodeKind == ClangNodeKind.FunctionExternResult ||
                nodeKind == ClangNodeKind.FunctionExternParameter ||
                nodeKind == ClangNodeKind.FunctionPointerResult ||
                nodeKind == ClangNodeKind.FunctionPointerParameter ||
                nodeKind == ClangNodeKind.RecordField ||
                nodeKind == ClangNodeKind.EnumValue)
            {
                return;
            }

            VisitRecord(cursor, cursorParent, depth);
        }

        private bool TypeIsUserOverrideOpaqueType(string typeName)
        {
            return _opaqueTypeNames.Contains(typeName);
        }

        private static bool TypeIsSystemIgnored(CXCursor cursor, CXType? type, string typeIdentifier)
        {
            var isSystem = type?.IsSystem() ?? cursor.IsSystem();
            if (!isSystem)
            {
                return false;
            }

            return typeIdentifier switch
            {
                "int8_t" => true,
                "uint8_t" => true,
                "int16_t" => true,
                "uint16_t" => true,
                "int32_t" => true,
                "uint32_t" => true,
                "int64_t" => true,
                "uint64_t" => true,
                "pid_t" => true,
                "sockaddr" => true,
                "sockaddr_in" => true,
                "sockaddr_in6" => true,
                _ => false
            };
        }

        private void LogVisit(ClangNode node)
        {
            if (!_printAbstractSyntaxTree)
            {
                return;
            }

            Console.WriteLine(node);
        }
    }
}
