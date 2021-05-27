// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangExplorer
    {
        private readonly DiagnosticsSink _diagnostics;
        private readonly ClangMapper _mapper = new();
        private readonly HashSet<string> _visitedTypeNames = new();
        private readonly HashSet<string> _knownInvalidTypeNames = new();
        private ImmutableHashSet<string> _overrideOpaqueTypeNames = null!;
        private readonly List<ClangFunction> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangRecord> _records = new();
        private readonly List<ClangOpaqueType> _opaqueDataTypes = new();
        private readonly List<ClangTypedef> _typedefs = new();
        private readonly List<ClangPointerFunction> _functionPointers = new();
        private readonly List<ClangVariable> _variables = new();
        private bool _printAbstractSyntaxTree;
        private readonly StringBuilder _logBuilder = new();

        public ClangExplorer(DiagnosticsSink diagnostics)
        {
            _diagnostics = diagnostics;
        }

        public ClangAbstractSyntaxTree AbstractSyntaxTree(
            CXTranslationUnit translationUnit,
            bool printAbstractSyntaxTree,
            IEnumerable<string> opaqueTypeNames)
        {
            _printAbstractSyntaxTree = printAbstractSyntaxTree;
            _overrideOpaqueTypeNames = opaqueTypeNames.ToImmutableHashSet();
            var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);

            var externs = translationUnitCursor.GetDescendents(IsExternCursor);

            foreach (var node in externs)
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

        private void VisitVariableExtern(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var name = cursor.GetName();
            var typeName = type.GetName();
            LogVisit(ClangNodeKind.Variable, name, typeName, cursor, depth);

            VisitType(type, cursor, cursorParent, depth + 1);

            var variable = _mapper.ClangVariableExtern(cursor, cursorParent, type);
            _variables.Add(variable);
        }

        private void VisitFunctionExtern(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var resultType = clang_getCursorResultType(cursor);

            var name = cursor.GetName();
            var typeName = type.GetName();
            LogVisit(ClangNodeKind.Function, name, typeName, cursor, depth);

            VisitFunctionExternResult(cursor, cursorParent, resultType, depth + 1);
            VisitFunctionExternParameters(cursor, depth + 1);

            var functionExtern = _mapper.ClangFunctionExtern(cursor, cursorParent, type);
            _functions.Add(functionExtern);
        }

        private void VisitEnum(CXType originalType, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var typeName = originalType.kind == CXTypeKind.CXType_Typedef ? originalType.GetName() : type.GetName();
            var isValid = IsValidTypeName(type, cursor, depth, typeName);
            if (!isValid)
            {
                return;
            }

            var typeAlreadyVisited = TypeIsAlreadyVisited(typeName);
            if (typeAlreadyVisited)
            {
                return;
            }

            LogVisit(ClangNodeKind.Enum, string.Empty, typeName, cursor, depth);

            var @enum = _mapper.ClangEnum(cursor, cursorParent, depth);
            _enums.Add(@enum);
        }

        private void VisitRecord(CXType originalType, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var typeName = originalType.kind == CXTypeKind.CXType_Typedef ? originalType.GetName() : type.GetName();
            var isValid = IsValidTypeName(type, cursor, depth, typeName);
            if (!isValid)
            {
                return;
            }

            var typeAlreadyVisited = TypeIsAlreadyVisited(typeName);
            if (typeAlreadyVisited)
            {
                return;
            }

            LogVisit(ClangNodeKind.Record, string.Empty, typeName, cursor, depth);

            VisitRecordFields(cursor, depth + 1);

            var isAnonymous = clang_Cursor_isAnonymous(cursor) > 0;
            if (isAnonymous)
            {
                return;
            }

            var record = _mapper.ClangRecord(cursor, cursorParent, type, typeName);
            _records.Add(record);
        }

        private void VisitTypedef(
            CXType originalType, CXType type, CXCursor cursorParent, int depth)
        {
            var cursor = clang_getTypeDeclaration(type);
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);

            if (underlyingType.kind == CXTypeKind.CXType_Elaborated)
            {
                underlyingType = clang_Type_getNamedType(underlyingType);
            }

            if (underlyingType.kind == CXTypeKind.CXType_Pointer)
            {
                var pointeeType = clang_getPointeeType(underlyingType);
                if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                {
                    VisitPointerFunction(cursor, cursorParent, cursor, originalType, pointeeType, depth);
                    return;
                }
            }

            var typeName = type.GetName();

            var isValid = IsValidTypeName(type, cursor, depth, typeName);
            if (!isValid)
            {
                return;
            }

            var underlyingCursor = clang_getTypeDeclaration(underlyingType);
            VisitTypeRecursive(type, underlyingType, cursor, underlyingCursor, cursorParent, depth);

            var alreadyVisited = TypeIsAlreadyVisited(typeName);
            if (alreadyVisited)
            {
                return;
            }

            LogVisit(ClangNodeKind.Typedef, string.Empty, typeName, cursor, depth);

            var typedef = _mapper.ClangTypedef(cursor, cursorParent, type, underlyingType);
            _typedefs.Add(typedef);
        }

        private void VisitOpaqueType(CXCursor cursor, CXType type, int depth)
        {
            var typeName = type.GetName();
            var typeAlreadyVisited = TypeIsAlreadyVisited(typeName);
            if (typeAlreadyVisited)
            {
                return;
            }

            LogVisit(ClangNodeKind.OpaqueType, string.Empty, typeName, cursor, depth);

            var opaqueType = _mapper.ClangOpaqueDataType(cursor, type);
            _opaqueDataTypes.Add(opaqueType);
        }

        private void VisitPointerFunction(
            CXCursor cursor, CXCursor cursorParent, CXCursor originalCursor, CXType originalType, CXType type, int depth)
        {
            if (originalType.kind == CXTypeKind.CXType_ConstantArray)
            {
                originalType = clang_getElementType(originalType);
            }

            if (originalType.kind == CXTypeKind.CXType_Typedef)
            {
                var typedefName = originalType.GetName();
                var alreadyVisited = TypeIsAlreadyVisited(typedefName);
                if (alreadyVisited)
                {
                    return;
                }
            }

            var name = originalCursor.GetName();
            var typeName = type.GetName();
            LogVisit(ClangNodeKind.PointerFunction, name, typeName, originalCursor, depth);

            var resultType = clang_getResultType(type);
            VisitType(resultType, cursor, cursorParent, depth);
            VisitPointerFunctionParameters(cursor, depth + 1);

            // typedefs always have name; otherwise the function pointer won't have a name
            //  to which we should not add it directly; instead the function pointer will be added when mapping the nested struct
            var canVisit = originalType.kind == CXTypeKind.CXType_Typedef;
            if (!canVisit)
            {
                return;
            }

            var functionPointer = _mapper.ClangFunctionPointer(originalCursor, cursorParent, originalType, type);
            _functionPointers.Add(functionPointer);
        }

        private void VisitType(CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var typeName = type.GetName();
            var isValid = IsValidTypeName(type, cursor, depth, typeName);
            if (!isValid)
            {
                return;
            }

            VisitTypeRecursive(type, type, cursor, cursor, cursorParent, depth);
        }

        private void VisitTypeRecursive(
            CXType originalType,
            CXType type,
            CXCursor originalCursor,
            CXCursor cursor,
            CXCursor cursorParent,
            int depth)
        {
            // void, int, char, etc
            if (type.IsPrimitive())
            {
                return;
            }

            // drill down to type of array
            if (type.kind == CXTypeKind.CXType_ConstantArray ||
                type.kind == CXTypeKind.CXType_IncompleteArray)
            {
                var elementType = clang_getElementType(type);
                var elementTypeCursor = clang_getTypeDeclaration(elementType);
                VisitTypeRecursive(originalType, elementType, originalCursor, elementTypeCursor, cursorParent, depth);
            }

            // drill down to type of pointer
            else if (type.kind == CXTypeKind.CXType_Pointer)
            {
                var pointeeType = clang_getPointeeType(type);
                if (pointeeType.kind == CXTypeKind.CXType_Elaborated)
                {
                    pointeeType = clang_Type_getNamedType(pointeeType);
                }

                var pointeeCursor = clang_getTypeDeclaration(pointeeType);
                if (pointeeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
                {
                    pointeeCursor = cursor;
                }

                var pointeeTypeSizeOf = clang_Type_getSizeOf(pointeeType);
                // if the pointee type doesn't have a known size, then it must be the case that it is an opaque type
                if (pointeeType.kind != CXTypeKind.CXType_Void && pointeeTypeSizeOf == -2)
                {
                    VisitOpaqueType(pointeeCursor, pointeeType, depth);
                }
                else
                {
                    VisitTypeRecursive(originalType, pointeeType, originalCursor, pointeeCursor, cursorParent, depth);
                }
            }

            // drill down to type modified by the type attribute; Clang allows for types to have attribute to specify
            //  type information; for more information see https://clang.llvm.org/docs/AttributeReference.html#type-attributes
            else if (type.kind == CXTypeKind.CXType_Attributed)
            {
                var modifiedType = clang_Type_getModifiedType(type);
                VisitType(modifiedType, cursor, cursorParent, depth);
            }

            // drill down to type without the elaborated keyword such as "struct"; e.g. "struct MyStruct" -> "MyStruct"
            else if (type.kind == CXTypeKind.CXType_Elaborated)
            {
                var namedType = clang_Type_getNamedType(type);
                var namedCursor = clang_getTypeDeclaration(namedType);
                VisitTypeRecursive(type, namedType, cursor, namedCursor, cursorParent, depth);
            }

            // function pointers don't have names; typedef, param, or field will have an identifier instead
            else if (type.kind == CXTypeKind.CXType_FunctionProto)
            {
                VisitPointerFunction(cursor, cursorParent, originalCursor, originalType, type, depth);
            }
            else
            {
                VisitTypeRecursiveNamed(originalType, type, cursor, cursorParent, depth);
            }
        }

        private void VisitTypeRecursiveNamed(
            CXType originalType, CXType type, CXCursor cursor, CXCursor cursorParent, int depth)
        {
            switch (type.kind)
            {
                case CXTypeKind.CXType_Record:
                    VisitRecord(originalType, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Enum:
                    VisitEnum(originalType, type, cursor, cursorParent, depth);
                    break;
                case CXTypeKind.CXType_Typedef:
                    VisitTypedef(originalType, type, cursorParent, depth);
                    break;
                default:
                    var up = new ClangExplorerException($"Unexpected type kind '{type.kind}'");
                    throw up;
            }
        }

        private bool IsValidTypeName(CXType type, CXCursor cursor, int depth, string typeName)
        {
            var knownToBeInvalid = _knownInvalidTypeNames.Contains(typeName);
            if (knownToBeInvalid)
            {
                return false;
            }

            var systemIgnore = TypeIsSystemIgnored(typeName, type);
            if (systemIgnore)
            {
                _diagnostics.Add(new DiagnosticClangSystemTypeIgnored(type));
                _knownInvalidTypeNames.Add(typeName);
                return false;
            }

            var isOverridenOpaqueType = TypeIsOverridenOpaqueType(typeName);
            if (isOverridenOpaqueType)
            {
                _diagnostics.Add(new DiagnosticClangTypeOpaqueOverriden(type));
                _knownInvalidTypeNames.Add(typeName);
                VisitOpaqueType(cursor, type, depth);
                return false;
            }

            var isSystemOpaque = TypeIsSystemOpaque(type);
            if (isSystemOpaque)
            {
                _diagnostics.Add(new DiagnosticClangSystemOpaqueTypeInternalsVisited(type));
            }

            return true;
        }

        private void VisitFunctionExternResult(CXCursor cursor, CXCursor cursorParent, CXType resultType, int depth)
        {
            var typeCursor = clang_getTypeDeclaration(resultType);

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

            VisitType(resultType, typeCursor, typeCursorParent, depth + 1);
        }

        private void VisitFunctionExternParameters(CXCursor cursor, int depth)
        {
            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var type = clang_getCursorType(node.Cursor);
                VisitFunctionExternParameter(node.Cursor, node.CursorParent, type, depth);
            }
        }

        private void VisitFunctionExternParameter(CXCursor cursor, CXCursor cursorParent, CXType type, int depth)
        {
            var name = cursor.GetName();
            var typeName = type.GetName();
            LogVisit(ClangNodeKind.FunctionParameter, name, typeName, cursor, depth);
            VisitType(type, cursor, cursorParent, depth + 1);
        }

        private void VisitRecordFields(CXCursor cursor, int depth)
        {
            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_FieldDecl);

            foreach (var node in nodes)
            {
                VisitRecordField(node.Cursor, node.CursorParent, depth);
            }
        }

        private void VisitRecordField(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var type = clang_getCursorType(cursor);
            VisitType(type, cursor, cursorParent, depth + 1);
        }

        private void VisitPointerFunctionParameters(CXCursor cursor, int depth)
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
            VisitType(type, cursor, cursorParent, depth + 1);
        }

        private bool TypeIsOverridenOpaqueType(string typeName)
        {
            return _overrideOpaqueTypeNames.Contains(typeName);
        }

        private bool TypeIsAlreadyVisited(string typeName)
        {
            var alreadyVisited = _visitedTypeNames.Contains(typeName);
            if (alreadyVisited)
            {
                return true;
            }

            _visitedTypeNames.Add(typeName);
            return false;
        }

        private static bool TypeIsSystemIgnored(string typeName, CXType type)
        {
            var isSystem = type.IsSystem();
            if (!isSystem)
            {
                return false;
            }

            return typeName switch
            {
                "FILE" => true,
                "DIR" => true,
                "pid_t" => true,
                "uid_t" => true,
                "gid_t" => true,
                "time_t" => true,
                "pthread_t" => true,
                "sockaddr" => true,
                "addrinfo" => true,
                "sockaddr_in" => true,
                "sockaddr_in6" => true,
                "socklen_t" => true,
                "size_t" => true,
                "ssize_t" => true,
                "int8_t" => true,
                "uint8_t" => true,
                "int16_t" => true,
                "uint16_t" => true,
                "int32_t" => true,
                "uint32_t" => true,
                "int64_t" => true,
                "uint64_t" => true,
                "uintptr_t" => true,
                "intptr_t" => true,
                "va_list" => true,
                _ => false
            };
        }

        private static bool TypeIsSystemOpaque(CXType type)
        {
            var underlyingType = type;
            while (true)
            {
                if (underlyingType.kind == CXTypeKind.CXType_Pointer)
                {
                    // pointers are legal
                    return false;
                }

                if (underlyingType.kind == CXTypeKind.CXType_Typedef)
                {
                    var declaration = clang_getTypeDeclaration(type);
                    type = clang_getTypedefDeclUnderlyingType(declaration);
                }
                else if (type.kind == CXTypeKind.CXType_Elaborated)
                {
                    type = clang_Type_getNamedType(type);
                }
                else if (!type.IsSystem())
                {
                    return false;
                }

                var typeName = type.GetName();
                return typeName switch
                {
                    "FILE" => true,
                    "DIR" => true,
                    "pthread_mutex_t" => true,
                    _ => false
                };
            }
        }

        private void LogVisit(
            ClangNodeKind nodeKind, string name, string typeName, CXCursor cursor, int depth)
        {
            if (!_printAbstractSyntaxTree)
            {
                return;
            }

            for (var i = 0; i < depth; i++)
            {
                _logBuilder.Append(' ');
            }

            _logBuilder.Append(nodeKind);
            _logBuilder.Append(' ');

            if (!string.IsNullOrEmpty(name))
            {
                _logBuilder.Append('\'');
                _logBuilder.Append(name);
                _logBuilder.Append('\'');
                _logBuilder.Append(':');
                _logBuilder.Append(' ');
            }

            _logBuilder.Append(typeName);
            _logBuilder.Append(' ');
            _logBuilder.Append('@');
            _logBuilder.Append(' ');

            var codeLocation = new ClangCodeLocation(cursor);
            _logBuilder.Append(codeLocation);

            Console.WriteLine(_logBuilder);

            _logBuilder.Clear();
        }
    }
}
