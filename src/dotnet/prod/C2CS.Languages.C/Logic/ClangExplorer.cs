// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangExplorer
    {
        private bool _printAbstractSyntaxTree;
        private readonly HashSet<CXCursor> _visitedCursors = new();
        private readonly HashSet<string> _visitedRecordNames = new();
        private readonly HashSet<string> _visitedFunctionProtoNames = new();
        private readonly HashSet<string> _visitedOpaqueNames = new();
        private readonly ClangMapper _mapper = new();
        private readonly List<ClangFunctionExtern> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangRecord> _records = new();
        private readonly List<ClangOpaqueDataType> _opaqueDataTypes = new();
        private readonly List<ClangOpaquePointer> _opaquePointers = new();
        private readonly List<ClangTypedef> _typedefs = new();
        private readonly List<ClangFunctionPointer> _functionPointers = new();
        private readonly List<ClangVariable> _variables = new();
        private readonly StringBuilder _logBuilder = new();

        public ClangAbstractSyntaxTree ExtractAbstractSyntaxTree(
            CXTranslationUnit translationUnit,
            bool printAbstractSyntaxTree)
        {
            _printAbstractSyntaxTree = printAbstractSyntaxTree;
            ExploreAbstractSyntaxTree(translationUnit);
            return CollectExtractedData();
        }

        private void ExploreAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);

            var externs = GetExternCursors(translationUnit);
            foreach (var @extern in externs)
            {
                VisitCursor(@extern, translationUnitCursor, 1);
            }
        }

        private static ImmutableArray<CXCursor> GetExternCursors(CXTranslationUnit translationUnit)
        {
            var externFunctions = new List<CXCursor>();

            var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);

            translationUnitCursor.VisitChildren(0, (cursor, _, _) =>
            {
                var kind = clang_getCursorKind(cursor);
                if (kind != CXCursorKind.CXCursor_FunctionDecl && kind != CXCursorKind.CXCursor_VarDecl)
                {
                    return;
                }

                var linkage = clang_getCursorLinkage(cursor);
                var isExternallyLinked = linkage == CXLinkageKind.CXLinkage_External;
                if (!isExternallyLinked)
                {
                    return;
                }

                var isSystemCursor = cursor.IsSystemCursor();
                if (isSystemCursor)
                {
                    return;
                }

                externFunctions.Add(cursor);
            });

            return externFunctions.ToImmutableArray();
        }

        private ClangAbstractSyntaxTree CollectExtractedData()
        {
            _functions.Sort();
            _functionPointers.Sort();
            _records.Sort();
            _enums.Sort();
            _opaqueDataTypes.Sort();
            _opaquePointers.Sort();
            _typedefs.Sort();

            var functionExterns = _functions.ToImmutableArray();
            var functionPointers = _functionPointers.ToImmutableArray();
            var records = _records.ToImmutableArray();
            var enums = _enums.ToImmutableArray();
            var opaqueDataTypes = _opaqueDataTypes.ToImmutableArray();
            var opaquePointers = _opaquePointers.ToImmutableArray();
            var typedefs = _typedefs.ToImmutableArray();
            var variables = _variables.ToImmutableArray();

            var result = new ClangAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                records,
                enums,
                opaqueDataTypes,
                opaquePointers,
                typedefs,
                variables);

            return result;
        }

        private void VisitCursor(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var canVisitCursor = CanVisitCursor(cursor);
            if (!canVisitCursor)
            {
                return;
            }

            var cursorKind = cursor.kind;
            var isDeclaration = clang_isDeclaration(cursorKind) > 0U;
            var isReference = clang_isReference(cursorKind) > 0U;

            if (isDeclaration)
            {
                VisitDeclaration(cursor, cursorParent, depth);
            }
            else if (isReference)
            {
                var cursorType = clang_getCursorType(cursor);
                var cursorTypeDeclaration = clang_getTypeDeclaration(cursorType);
                VisitCursor(cursorTypeDeclaration, cursorParent, depth);
            }
            else
            {
                switch (cursorKind)
                {
                    case CXCursorKind.CXCursor_DeclRefExpr: // An expression that refers to some value declaration, e.g. a function, variable, or enumerator.
                        var cursorReferenced = clang_getCursorReferenced(cursor);
                        VisitCursor(cursorReferenced, cursorParent, depth);
                        break;
                    case CXCursorKind.CXCursor_BinaryOperator: // A builtin binary operation expression, e.g. "x + y"
                    case CXCursorKind.CXCursor_ParenExpr: // A parenthesized expression, e.g. "(1)"
                        // Ignore
                        break;
                    default:
                        var up = new ExploreUnexpectedException(cursor);
                        throw up;
                }
            }
        }

        private bool CanVisitCursor(CXCursor cursor)
        {
            var hasAlreadyVisitedCursor = _visitedCursors.Contains(cursor);
            if (hasAlreadyVisitedCursor)
            {
                return false;
            }

            _visitedCursors.Add(cursor);

            var isSystemCursor = cursor.IsSystemCursor();
            if (isSystemCursor)
            {
                return false;
            }

            var isCursorAttribute = clang_isAttribute(cursor.kind) > 0U;
            if (isCursorAttribute)
            {
                return false;
            }

            var cursorKind = cursor.kind;
            if (cursorKind == CXCursorKind.CXCursor_IntegerLiteral)
            {
                return false;
            }

            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitType(CXType type, CXCursor cursor, CXCursor parentCursor, int depth)
        {
            switch (type.kind)
            {
                case CXTypeKind.CXType_Attributed:
                    var modifiedType = clang_Type_getModifiedType(type);
                    VisitType(modifiedType, cursor, parentCursor, depth);
                    break;
                case CXTypeKind.CXType_Elaborated:
                    var namedType = clang_Type_getNamedType(type);
                    VisitType(namedType, cursor, parentCursor, depth);
                    break;
                case CXTypeKind.CXType_Pointer:
                    var pointeeType = clang_getPointeeType(type);
                    VisitType(pointeeType, cursor, parentCursor, depth);
                    break;
                case CXTypeKind.CXType_Record:
                case CXTypeKind.CXType_Enum:
                case CXTypeKind.CXType_Typedef:
                    var declaration = clang_getTypeDeclaration(type);
                    VisitCursor(declaration, parentCursor, depth);
                    break;
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_IncompleteArray:
                    var elementType = clang_getElementType(type);
                    VisitType(elementType, cursor, parentCursor, depth);
                    break;
                case CXTypeKind.CXType_FunctionProto:
                    VisitFunctionProto(cursor, parentCursor, depth);
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
                    // Ignore
                    break;
                default:
                    var up = new ExploreUnexpectedException(type, cursor);
                    throw up;
            }
        }

        private void VisitDeclaration(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var cursorType = clang_getCursorType(cursor);
            var sizeOf = clang_Type_getSizeOf(cursorType);
            if (sizeOf == -2)
            {
                // -2 = CXTypeLayoutError_Incomplete
                VisitOpaque(cursor, depth);
            }
            else
            {
                switch (cursor.kind)
                {
                    case CXCursorKind.CXCursor_EnumDecl:
                        VisitEnum(cursor, depth);
                        break;
                    case CXCursorKind.CXCursor_EnumConstantDecl:
                        VisitEnumConstant(cursor, cursorParent, depth);
                        break;
                    case CXCursorKind.CXCursor_UnionDecl:
                    case CXCursorKind.CXCursor_StructDecl:
                        VisitRecord(cursor, depth);
                        break;
                    case CXCursorKind.CXCursor_FieldDecl:
                        VisitField(cursor, cursorParent, depth);
                        break;
                    case CXCursorKind.CXCursor_TypedefDecl:
                        VisitTypedef(cursor, depth);
                        break;
                    case CXCursorKind.CXCursor_FunctionDecl:
                        VisitFunction(cursor, cursorParent, depth);
                        break;
                    case CXCursorKind.CXCursor_ParmDecl:
                        VisitParameter(cursor, cursorParent, depth);
                        break;
                    case CXCursorKind.CXCursor_VarDecl:
                        VisitVariable(cursor, cursorParent, depth);
                        break;
                    default:
                        var up = new ExploreUnexpectedException(cursor);
                        throw up;
                }
            }
        }

        private void VisitFunction(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, null, "Function", depth);
            }

            var resultType = clang_getCursorResultType(cursor);
            VisitType(resultType, cursor, cursorParent, depth + 1);

            cursor.VisitChildren(depth + 1, VisitCursor);

            var clangFunction = _mapper.MapFunctionExtern(cursor);
            _functions.Add(clangFunction);
        }

        private void VisitParameter(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);

            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, type, "Parameter", depth);
            }

            VisitType(type, cursor, cursorParent, depth + 1);

            cursor.VisitChildren(depth + 1, VisitCursor);
        }

        private void VisitVariable(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);

            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, type, "Variable", depth);
            }

            VisitType(type, cursor, cursorParent, depth + 1);

            var clangVariable = _mapper.MapVariable(cursor);
            _variables.Add(clangVariable);
        }

        private void VisitEnum(CXCursor cursor, int depth)
        {
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, null, "Enum", depth);
            }

            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var namedType = clang_Type_getNamedType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(namedType);
            }

            underlyingCursor.VisitChildren(depth + 1, (child, parent, d) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                VisitCursor(child, parent, d);
            });

            var clangEnum = _mapper.MapEnum(cursor);
            _enums.Add(clangEnum);
        }

        private void VisitEnumConstant(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            CXCursor cursorEnum;

            var cursorParentKind = cursor.kind;
            if (cursorParentKind != CXCursorKind.CXCursor_EnumDecl)
            {
                cursorEnum = clang_getCursorSemanticParent(cursor);
            }
            else
            {
                cursorEnum = cursorParent;
            }

            var integerType = clang_getEnumDeclIntegerType(cursorEnum);

            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, integerType, "EnumConstant", depth);
            }

            VisitType(integerType, cursor, cursorParent, depth + 1);
        }

        private bool CanVisitRecord(CXCursor cursor)
        {
            // NOTE: It's possible that a record has a field which is a pointer of type itself

            var recordName = cursor.GetName();
            if (_visitedRecordNames.Contains(recordName))
            {
                return false;
            }

            _visitedRecordNames.Add(recordName);
            return true;
        }

        private void VisitRecord(CXCursor cursor, int depth)
        {
            if (!CanVisitRecord(cursor))
            {
                return;
            }

            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, null, "Record", depth);
            }

            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var namedType = clang_Type_getNamedType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(namedType);
            }

            underlyingCursor.VisitChildren(depth + 1, (child, parent, d) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                VisitCursor(child, cursor, d);
            });

            var isAnonymousCursor = clang_Cursor_isAnonymous(cursor) > 0U;
            if (isAnonymousCursor)
            {
                return;
            }

            var record = _mapper.MapRecord(cursor);
            _records.Add(record);
        }

        private void VisitField(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, type, "Field", depth);
            }

            VisitType(type, cursor, cursorParent, depth + 1);

            cursor.VisitChildren(depth + 1, VisitCursor);
        }

        private void VisitTypedef(CXCursor cursor, int depth)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var underlyingTypeCanonical = clang_getCanonicalType(underlyingType);

            switch (underlyingTypeCanonical.kind)
            {
                case CXTypeKind.CXType_Pointer:
                    var pointeeType = clang_getPointeeType(underlyingTypeCanonical);
                    VisitTypedefPointer(cursor, pointeeType, underlyingType, depth);
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
                    VisitTypedefAlias(cursor, underlyingType, depth);
                    break;
                case CXTypeKind.CXType_Record:
                    VisitRecord(cursor, depth);
                    break;
                case CXTypeKind.CXType_Enum:
                    VisitEnum(cursor, depth);
                    break;
                default:
                    var up = new ExploreUnexpectedException(cursor);
                    throw up;
            }
        }

        private void VisitTypedefPointer(CXCursor cursor, CXType pointeeType, CXType underlyingType, int depth)
        {
            var kind = pointeeType.kind;
            var pointeeTypeSizeOf = clang_Type_getSizeOf(pointeeType);

            if (kind == CXTypeKind.CXType_Void || pointeeTypeSizeOf == -2)
            {
                VisitOpaquePointer(cursor, depth);
            }
            else
            {
                switch (pointeeType.kind)
                {
                    case CXTypeKind.CXType_Record:
                        VisitTypedefAlias(cursor, underlyingType, depth);
                        break;
                    case CXTypeKind.CXType_FunctionProto:
                        var cursorParent = clang_getCursorSemanticParent(cursor);
                        VisitFunctionProto(cursor, cursorParent, depth);
                        break;
                    default:
                        var up = new ExploreUnexpectedException(cursor);
                        throw up;
                }
            }
        }

        private void VisitTypedefAlias(CXCursor cursor, CXType underlyingType, int depth)
        {
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, underlyingType, "Typedef", depth);
            }

            var typedef = _mapper.MapTypedef(cursor);
            _typedefs.Add(typedef);
        }

        private void VisitOpaquePointer(CXCursor cursor, int depth)
        {
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, null, "OpaquePointer", depth);
            }

            var opaquePointerType = _mapper.MapOpaquePointer(cursor);
            _opaquePointers.Add(opaquePointerType);
        }

        private bool CanVisitOpaque(CXCursor cursor)
        {
            var name = cursor.GetName();
            if (_visitedOpaqueNames.Contains(name))
            {
                return false;
            }

            _visitedOpaqueNames.Add(name);
            return true;
        }

        private void VisitOpaque(CXCursor cursor, int depth)
        {
            if (!CanVisitOpaque(cursor))
            {
                return;
            }

            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, null, "Opaque", depth);
            }

            var opaqueDataType = _mapper.MapOpaqueDataType(cursor);
            _opaqueDataTypes.Add(opaqueDataType);
        }

        private bool CanVisitFunctionProto(CXCursor cursor, CXCursor parent)
        {
            var functionProtoName = cursor.GetName();
            var parentName = parent.GetName();
            var fullFunctionProtoName = $"{parentName}_{functionProtoName}";

            if (_visitedFunctionProtoNames.Contains(fullFunctionProtoName))
            {
                return false;
            }

            _visitedFunctionProtoNames.Add(fullFunctionProtoName);
            return true;
        }

        private void VisitFunctionProto(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            if (!CanVisitFunctionProto(cursor, cursorParent))
            {
                return;
            }

            var resultType = GetFunctionProtoResultType(cursor);
            if (_printAbstractSyntaxTree)
            {
                LogVisit(cursor, resultType, "FunctionProto", depth);
            }

            VisitType(resultType, cursor, cursorParent, depth + 1);

            cursor.VisitChildren(depth + 1, VisitCursor);

            var clangFunctionPointer = _mapper.MapFunctionPointer(cursor, cursorParent);
            _functionPointers.Add(clangFunctionPointer);
        }

        private static CXType GetFunctionProtoResultType(CXCursor cursor)
        {
            CXType result;

            var cursorKind = cursor.kind;
            if (cursorKind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var underlyingTypeCanonical = clang_getCanonicalType(underlyingType);
                var pointeeType = clang_getPointeeType(underlyingTypeCanonical);
                result = clang_getResultType(pointeeType);
            }
            else if (cursorKind == CXCursorKind.CXCursor_ParmDecl ||
                     cursorKind == CXCursorKind.CXCursor_FieldDecl)
            {
                var type = clang_getCursorType(cursor);
                var typeCanonical = clang_getCanonicalType(type);
                var pointeeType = clang_getPointeeType(typeCanonical);
                result = clang_getResultType(pointeeType);
            }
            else
            {
                result = clang_getCursorResultType(cursor);
            }

            // ReSharper disable once InvertIf
            if (result.kind == CXTypeKind.CXType_Invalid)
            {
                var up = new ExploreUnexpectedException(cursor);
                throw up;
            }

            return result;
        }

        private class ExploreUnexpectedException : Exception
        {
            public CXTypeKind TypeKind { get; }

            public string CursorFilePath { get; }

            public int CursorFileLineNumber { get; }

            public int CursorFileLineColumn { get; }

            public CXCursorKind CursorKind { get; }

            public ExploreUnexpectedException(CXType type, CXCursor cursor)
                : this(cursor)
            {
                TypeKind = type.kind;
                var cursorLocation = cursor.GetLocation();
                CursorFilePath = cursorLocation.FilePath;
                CursorFileLineNumber = cursorLocation.LineNumber;
                CursorFileLineColumn = cursorLocation.LineColumn;
                CursorKind = clang_getCursorKind(cursor);
            }

            public ExploreUnexpectedException(CXCursor cursor)
                : base("Unexpected error while exploring Clang header.")
            {
                TypeKind = CXTypeKind.CXType_Invalid;
                var cursorLocation = cursor.GetLocation();
                CursorFilePath = cursorLocation.FilePath;
                CursorFileLineNumber = cursorLocation.LineNumber;
                CursorFileLineColumn = cursorLocation.LineColumn;
                CursorKind = clang_getCursorKind(cursor);
            }
        }

        private void LogVisit(CXCursor cursor, CXType? type, string cursorKindString, int depth)
        {
            var name = cursor.GetName();
            if (string.IsNullOrEmpty(name))
            {
                name = "???";
            }

            var typeName = type.HasValue ? type.Value.GetName() : string.Empty;

            var (filePath, lineNumber, _) = cursor.GetLocation();
            var fileName = Path.GetFileName(filePath);
            _logBuilder.Clear();

            for (var i = 0; i < depth; i++)
            {
                _logBuilder.Append("  ");
            }

            _logBuilder.Append(cursorKindString);
            _logBuilder.Append(' ');
            _logBuilder.Append('\'');
            _logBuilder.Append(name);
            _logBuilder.Append('\'');
            if (type.HasValue)
            {
                _logBuilder.Append(':');
                _logBuilder.Append(' ');
                _logBuilder.Append(typeName);
            }

            _logBuilder.Append(" @ ");
            _logBuilder.Append(fileName);
            _logBuilder.Append(':');
            _logBuilder.Append(lineNumber);

            Console.WriteLine(_logBuilder);
        }
    }
}
