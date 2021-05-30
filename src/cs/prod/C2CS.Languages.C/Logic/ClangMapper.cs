// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangMapper
    {
        public CFunction ClangFunctionExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var name = ClangGetIdentifier(CNodeKind.Function, cursor);
            var codeLocation = new CCodeLocation(cursor);
            var callingConvention = ClangFunctionCallingConvention(type);
            var returnType = ClangFunctionExternType(cursor, cursorParent);
            var parameters = ClangFunctionExternParameters(cursor);

            return new CFunction(
                name,
                codeLocation,
                callingConvention,
                returnType,
                parameters);
        }

        private static CFunctionCallingConvention ClangFunctionCallingConvention(CXType type)
        {
            var callingConvention = clang_getFunctionTypeCallingConv(type);
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => C.CFunctionCallingConvention.C,
                _ => throw new ClangExplorerException($"Unknown calling convention '{callingConvention}'.")
            };

            return result;
        }

        private ImmutableArray<CFunctionParameter> ClangFunctionExternParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var functionExternParameter = ClangFunctionExternParameter(node.Cursor, node.CursorParent);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CFunctionParameter ClangFunctionExternParameter(
            CXCursor cursor, CXCursor cursorParent, string? name = null)
        {
            name ??= ClangGetIdentifier(CNodeKind.FunctionParameter, cursor);
            var type = ClangType(CNodeKind.FunctionParameter, cursor, cursorParent);
            var codeLocation = new CCodeLocation(cursor);

            var result = new CFunctionParameter(
                name,
                codeLocation,
                type);

            return result;
        }

        public CPointerFunction ClangFunctionPointer(
            CXCursor cursor, CXCursor cursorParent, CXType originalType, CXType type)
        {
            var name = cursor.GetName();
            var mappedType = ClangType(CNodeKind.PointerFunction, cursor, cursorParent);
            var codeLocation = new CCodeLocation(cursor);
            var parameters = ClangFunctionPointerParameters(cursor);
            var returnType = clang_getResultType(type);
            var mappedReturnType = ClangType(CNodeKind.PointerFunctionResult, cursor, cursorParent, returnType);
            var isWrapped = cursorParent.kind == CXCursorKind.CXCursor_StructDecl && originalType.kind != CXTypeKind.CXType_Typedef;

            return new CPointerFunction(
                name,
                codeLocation,
                mappedType,
                mappedReturnType,
                parameters,
                isWrapped);
        }

        private ImmutableArray<CPointerFunctionParameter> ClangFunctionPointerParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<CPointerFunctionParameter>();

            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var functionPointerParameter = ClangFunctionPointerParameter(node.Cursor, node.CursorParent);
                if (functionPointerParameter.Type.Name == "__va_list_tag")
                {
                    return ImmutableArray<CPointerFunctionParameter>.Empty;
                }

                builder.Add(functionPointerParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CPointerFunctionParameter ClangFunctionPointerParameter(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = new CCodeLocation(cursor);
            var name = ClangGetIdentifier(CNodeKind.PointerFunctionParameter, cursor);
            var type = ClangType(CNodeKind.PointerFunctionParameter, cursor, cursorParent);

            var result = new CPointerFunctionParameter(
                name,
                codeLocation,
                type);

            return result;
        }

        public CRecord ClangRecord(CXCursor cursor, CXCursor cursorParent, CXType type, string typeName)
        {
            var codeLocation = new CCodeLocation(cursor);
            var mappedType = ClangType(CNodeKind.Record, cursor, cursorParent, type, typeName);
            var recordFields = ClangRecordFields(cursor);
            var recordNestedRecords = ClangNestedNodes(cursor);

            return new CRecord(
                codeLocation,
                mappedType,
                recordFields,
                recordNestedRecords);
        }

        private ImmutableArray<CRecordField> ClangRecordFields(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<CRecordField>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);

            var nodes = underlyingCursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_FieldDecl);

            foreach (var node in nodes)
            {
                var recordField = ClangRecordField(node.Cursor, node.CursorParent);
                builder.Add(recordField);
            }

            ClangCalculatePaddingForStructFields(cursor, builder);

            var result = builder.ToImmutable();
            return result;
        }

        private static void ClangCalculatePaddingForStructFields(
            CXCursor cursor,
            ImmutableArray<CRecordField>.Builder builder)
        {
            for (var i = 1; i < builder.Count; i++)
            {
                var recordField = builder[i];
                var fieldPrevious = builder[i - 1];

                var expectedFieldOffset = fieldPrevious.Offset + fieldPrevious.Type.SizeOf;
                var hasPadding = recordField.Offset != 0 && recordField.Offset != expectedFieldOffset;
                if (!hasPadding)
                {
                    continue;
                }

                var padding = recordField.Offset - expectedFieldOffset;
                builder[i - 1] = new CRecordField(fieldPrevious, padding);
            }

            if (builder.Count >= 1)
            {
                var fieldLast = builder[^1];
                var cursorType = clang_getCursorType(cursor);
                var recordSize = (int) clang_Type_getSizeOf(cursorType);
                var expectedLastFieldOffset = recordSize - fieldLast.Type.SizeOf;
                if (fieldLast.Offset != expectedLastFieldOffset)
                {
                    var padding = expectedLastFieldOffset - fieldLast.Offset;
                    builder[^1] = new CRecordField(fieldLast, padding);
                }
            }
        }

        private CRecordField ClangRecordField(CXCursor cursor, CXCursor cursorParent)
        {
            var name = cursor.GetName();
            var codeLocation = new CCodeLocation(cursor);
            var mappedType = ClangType(CNodeKind.RecordField, cursor, cursorParent);
            var offset = (int) (clang_Cursor_getOffsetOfField(cursor) / 8);

            var isUnNamedFunctionPointer = false;
            var type = clang_getCursorType(cursor);
            if (type.kind == CXTypeKind.CXType_Pointer)
            {
                var pointeeType = clang_getPointeeType(type);
                if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                {
                    isUnNamedFunctionPointer = true;
                }
            }

            return new CRecordField(
                name,
                codeLocation,
                mappedType,
                offset,
                isUnNamedFunctionPointer);
        }

        private ImmutableArray<CNode> ClangNestedNodes(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<CNode>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);

            var nodes = underlyingCursor.GetDescendents((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return false;
                }

                var type = clang_getCursorType(child);
                var typeDeclaration = clang_getTypeDeclaration(type);
                var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;
                if (isAnonymous)
                {
                    return true;
                }

                if (type.kind == CXTypeKind.CXType_Pointer)
                {
                    var pointeeType = clang_getPointeeType(type);
                    if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        return true;
                    }
                }

                return false;
            });

            foreach (var node in nodes)
            {
                var record = ClangNestedNode(node.Cursor, node.CursorParent);
                builder.Add(record);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CNode ClangNestedNode(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang_getCursorType(cursor);
            var isPointer = type.kind == CXTypeKind.CXType_Pointer;
            if (!isPointer)
            {
                return ClangNestedStruct(cursor, cursorParent, type);
            }

            var pointeeType = clang_getPointeeType(type);
            if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
            {
                return ClangFunctionPointer(cursor, cursorParent, type, pointeeType);
            }

            var up = new ClangMapperException("Unknown mapping for nested node.");
            throw up;
        }

        private CNode ClangNestedStruct(CXCursor cursor, CXCursor cursorParent, CXType cursorType)
        {
            var declaration = clang_getTypeDeclaration(cursorType);
            var codeLocation = new CCodeLocation(declaration);
            var type = ClangType(CNodeKind.Record, declaration, cursor);

            var recordFields = ClangRecordFields(declaration);
            var recordNestedRecords = ClangNestedNodes(declaration);

            return new CRecord(
                codeLocation,
                type,
                recordFields,
                recordNestedRecords);
        }

        public CEnum ClangEnum(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var codeLocation = new CCodeLocation(cursor);
            var type = clang_getCursorType(cursor);
            var mappedType = ClangType(CNodeKind.Enum, cursor, cursorParent, type);
            var integerType = clang_getEnumDeclIntegerType(cursor);
            var mappedIntegerType = ClangType(CNodeKind.Enum, cursor, cursorParent, integerType);
            var enumValues = ClangEnumValues(cursor, depth + 1);

            var result = new CEnum(
                codeLocation,
                mappedType,
                mappedIntegerType,
                enumValues);

            return result;
        }

        // private ClangType MapEnumIntegerType(CXCursor cursor, CXCursor cursorParent)
        // {
        //     CXType enumIntegerType;
        //
        //     if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
        //     {
        //         var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
        //         var underlyingTypeDeclaration = clang_getTypeDeclaration(underlyingType);
        //         enumIntegerType = clang_getEnumDeclIntegerType(underlyingTypeDeclaration);
        //     }
        //     else
        //     {
        //         enumIntegerType = clang_getEnumDeclIntegerType(cursor);
        //     }
        //
        //     var result = MapType(cursor, cursorParent, enumIntegerType);
        //     return result;
        // }

        private ImmutableArray<CEnumValue> ClangEnumValues(CXCursor cursor, int depth)
        {
            var builder = ImmutableArray.CreateBuilder<CEnumValue>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);

            var nodes = underlyingCursor.GetDescendents((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return false;
                }

                return true;
            });

            foreach (var node in nodes)
            {
                var enumValue = ClangEnumValue(node.Cursor, node.CursorParent, depth);
                builder.Add(enumValue);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CEnumValue ClangEnumValue(CXCursor cursor, CXCursor cursorParent, int depth, string? name = null)
        {
            var value = clang_getEnumConstantDeclValue(cursor);
            var codeLocation = new CCodeLocation(cursor);
            name ??= ClangGetIdentifier(CNodeKind.EnumValue, cursor);

            var result = new CEnumValue(
                name,
                codeLocation,
                value);

            return result;
        }

        public COpaqueType ClangOpaqueDataType(CXCursor cursor, CXType type)
        {
            var location = new CCodeLocation(cursor);
            var name = type.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(type);
            var alignOf = (int) clang_Type_getAlignOf(type);

            var result = new COpaqueType(
                name,
                location,
                sizeOf,
                alignOf);

            return result;
        }

        public CTypedef ClangTypedef(CXCursor cursor, CXCursor cursorParent, CXType type, CXType underlyingType)
        {
            var codeLocation = new CCodeLocation(cursor);
            var typeName = type.GetName();
            var mappedType = ClangType(CNodeKind.Typedef, cursor, cursorParent, type, typeName);
            var mappedUnderlyingType = ClangType(CNodeKind.Typedef, cursor, cursorParent, underlyingType);

            var result = new CTypedef(
                typeName,
                codeLocation,
                mappedType,
                mappedUnderlyingType);

            return result;
        }

        public CVariable ClangVariableExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var codeLocation = new CCodeLocation(cursor);
            var name = ClangGetIdentifier(CNodeKind.Variable, cursor);
            var clangType = ClangType(CNodeKind.Variable, cursor, cursorParent, type);

            var result = new CVariable(
                name,
                codeLocation,
                clangType);

            return result;
        }

        private CType ClangType(
            CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType? type = null, string? typeName = null)
        {
            var cursorType = type ?? clang_getCursorType(cursor);
            var mappedTypeName = typeName ?? ClangNodeTypeName(nodeKind, cursor, cursorParent, cursorType);
            var originalName = cursorType.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(cursorType);
            var alignOf = (int) clang_Type_getAlignOf(cursorType);
            var arraySize = (int) clang_getArraySize(cursorType);
            var isSystemType = cursorType.IsSystem();

            var elementSize = sizeOf;
            if (cursorType.kind == CXTypeKind.CXType_ConstantArray)
            {
                var elementType = clang_getElementType(cursorType);
                elementSize = (int) clang_Type_getSizeOf(elementType);
            }

            return new CType(
                mappedTypeName,
                originalName,
                sizeOf,
                alignOf,
                elementSize,
                arraySize,
                isSystemType);
        }

        private CType ClangFunctionExternType(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang_getCursorResultType(cursor);
            var result = ClangType(CNodeKind.FunctionResult, cursor, cursorParent, type);
            return result;
        }

        private static CXCursor ClangUnderlyingCursor(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var namedType = clang_Type_getNamedType(underlyingType);
                underlyingCursor = clang_getTypeDeclaration(namedType);
            }

            return underlyingCursor;
        }

        public static string ClangGetIdentifier(CNodeKind nodeKind, CXCursor cursor)
        {
            if (nodeKind != CNodeKind.Function &&
                nodeKind != CNodeKind.FunctionParameter &&
                nodeKind != CNodeKind.PointerFunction &&
                nodeKind != CNodeKind.PointerFunctionParameter &&
                nodeKind != CNodeKind.RecordField &&
                nodeKind != CNodeKind.EnumValue &&
                nodeKind != CNodeKind.Variable)
            {
                return string.Empty;
            }

            return cursor.GetName();
        }

        public string ClangNodeTypeName(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var result = nodeKind switch
            {
                CNodeKind.Variable => type.GetName(),
                CNodeKind.Function => cursor.GetName(),
                CNodeKind.FunctionResult => ClangTypeName(nodeKind, cursor, cursorParent, type),
                CNodeKind.FunctionParameter => ClangTypeName(nodeKind, cursor, cursorParent, type),
                CNodeKind.PointerFunction => ClangTypeNameFunctionPointer(type),
                CNodeKind.PointerFunctionResult => ClangTypeName(nodeKind, cursor, cursorParent, type),
                CNodeKind.PointerFunctionParameter => ClangTypeName(nodeKind, cursor, cursorParent, type),
                CNodeKind.Typedef => type.GetName(),
                CNodeKind.Record => ClangTypeNameRecord(cursor, cursorParent, type),
                CNodeKind.RecordField => ClangTypeNameRecordField(nodeKind, cursor, cursorParent, type),
                CNodeKind.Enum => type.GetName(),
                CNodeKind.OpaqueType => type.GetName(),
                _ => throw new ClangMapperException($"Unexpected node kind '{nodeKind}'.")
            };

            var isReadOnly = clang_isConstQualifiedType(type) > 0;
            if (isReadOnly)
            {
                result = result.Replace("const ", string.Empty).Trim();
            }

            return result;
        }

        private string ClangTypeNameRecordField(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var isFunctionPointer = type.kind == CXTypeKind.CXType_Pointer &&
                                    clang_getPointeeType(type).kind == CXTypeKind.CXType_FunctionProto;
            if (isFunctionPointer)
            {
                return cursor.GetName();
            }

            return ClangTypeName(nodeKind, cursor, cursorParent, type);
        }

        private string ClangTypeName(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            string result;
            switch (type.kind)
            {
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
                    result = type.GetName();
                    break;
                case CXTypeKind.CXType_Pointer:
                    result = ClangTypeNamePointer(nodeKind, cursor, cursorParent, type);
                    break;
                case CXTypeKind.CXType_Typedef:
                    result = ClangTypeNameTypedef(nodeKind, cursor, cursorParent, type);
                    break;
                case CXTypeKind.CXType_Elaborated:
                    var namedTyped = clang_Type_getNamedType(type);
                    var namedCursor = clang_getTypeDeclaration(namedTyped);
                    result = ClangTypeName(nodeKind, namedCursor, cursor, namedTyped);
                    break;
                case CXTypeKind.CXType_Record:
                    result = ClangTypeNameRecord(cursor, cursorParent, type);
                    break;
                case CXTypeKind.CXType_Enum:
                    result = cursor.GetName();
                    break;
                case CXTypeKind.CXType_ConstantArray:
                    result = ClangTypeNameConstantArray(nodeKind, cursor, cursorParent, type);
                    break;
                case CXTypeKind.CXType_IncompleteArray:
                    result = ClangTypeNameIncompleteArray(nodeKind, type, cursor, cursorParent);
                    break;
                case CXTypeKind.CXType_FunctionProto:
                    result = ClangTypeNameFunctionProto(cursor);
                    break;
                case CXTypeKind.CXType_Attributed:
                    var modifiedType = clang_Type_getModifiedType(type);
                    result = ClangTypeName(nodeKind, cursor, cursorParent, modifiedType);
                    break;
                default:
                    var up = new ClangMapperException($"Unexpected Clang type '{type.kind}'.");
                    throw up;
            }

            return result;
        }

        private string ClangTypeNameFunctionProto(CXCursor cursor)
        {
            var result = cursor.GetName();
            return result;
        }

        private string ClangTypeNameConstantArray(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var elementType = clang_getArrayElementType(type);
            var result = ClangTypeName(nodeKind, cursor, cursorParent, elementType);
            return result;
        }

        private string ClangTypeNameIncompleteArray(CNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent)
        {
            var elementType = clang_getArrayElementType(type);
            var result = ClangTypeName(nodeKind, cursor, cursorParent, elementType);
            return $"{result}*";
        }

        private string ClangTypeNamePointer(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            string result;

            var pointeeType = clang_getPointeeType(type);
            if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
            {
                result = ClangTypeNameFunctionPointer(pointeeType);
            }
            else
            {
                var pointeeCursor = clang_getTypeDeclaration(pointeeType);
                var pointeeTypeName = ClangTypeName(nodeKind, pointeeCursor, cursorParent, pointeeType);
                result = $"{pointeeTypeName}*";
            }

            return result;
        }

        private string ClangTypeNameFunctionPointer(CXType type)
        {
            return type.GetName();
        }

        private static string ClangTypeNameTypedef(CNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var typedef = clang_getTypeDeclaration(type);
            var result = typedef.GetName();
            return result;
        }

        private string ClangTypeNameRecord(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            if (type.kind == CXTypeKind.CXType_Typedef)
            {
                return type.GetName();
            }

            string result;
            var clangRecord = clang_getTypeDeclaration(type);
            var name = cursor.GetName();

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            var isAnonymous = clang_Cursor_isAnonymous(clangRecord) > 0;
            if (isAnonymous)
            {
                var parentCursorName = cursorParent.GetName();

                if (clangRecord.kind == CXCursorKind.CXCursor_UnionDecl)
                {
                    result = $"AnonymousUnion_{parentCursorName}";
                }
                else
                {
                    result = $"AnonymousStruct_{parentCursorName}";
                }
            }
            else
            {
                if (name.Contains("union "))
                {
                    result = name.Replace("union ", string.Empty);
                }
                else if (name.Contains("struct "))
                {
                    result = name.Replace("struct ", string.Empty);
                }
                else
                {
                    result = name;
                }
            }

            return result;
        }
    }
}
