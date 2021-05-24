// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangMapper
    {
        public ClangFunction ClangFunctionExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var name = ClangGetIdentifier(ClangNodeKind.Function, cursor);
            var codeLocation = new ClangCodeLocation(cursor);
            var callingConvention = ClangFunctionCallingConvention(type);
            var returnType = ClangFunctionExternType(cursor, cursorParent);
            var parameters = ClangFunctionExternParameters(cursor);

            return new ClangFunction(
                name,
                codeLocation,
                callingConvention,
                returnType,
                parameters);
        }

        private static ClangFunctionCallingConvention ClangFunctionCallingConvention(CXType type)
        {
            var callingConvention = clang_getFunctionTypeCallingConv(type);
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => C.ClangFunctionCallingConvention.C,
                _ => throw new ClangExplorerException($"Unknown calling convention '{callingConvention}'.")
            };

            return result;
        }

        private ImmutableArray<ClangFunctionParameter> ClangFunctionExternParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionParameter>();

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

        private ClangFunctionParameter ClangFunctionExternParameter(
            CXCursor cursor, CXCursor cursorParent, string? name = null)
        {
            name ??= ClangGetIdentifier(ClangNodeKind.FunctionParameter, cursor);
            var type = ClangType(ClangNodeKind.FunctionParameter, cursor, cursorParent);
            var codeLocation = new ClangCodeLocation(cursor);

            var result = new ClangFunctionParameter(
                name,
                codeLocation,
                type);

            return result;
        }

        public ClangPointerFunction ClangFunctionPointer(
            CXCursor cursor, CXCursor cursorParent, CXType originalType, CXType type)
        {
            var name = cursor.GetName();
            var mappedType = ClangType(ClangNodeKind.PointerFunction, cursor, cursorParent);
            var codeLocation = new ClangCodeLocation(cursor);
            var parameters = ClangFunctionPointerParameters(cursor);
            var pointerSize = (int)clang_Type_getSizeOf(type);
            var returnType = clang_getResultType(type);
            var mappedReturnType = ClangType(ClangNodeKind.PointerFunctionResult, cursor, cursorParent, returnType);
            var isWrapped = cursorParent.kind == CXCursorKind.CXCursor_StructDecl && originalType.kind != CXTypeKind.CXType_Typedef;

            return new ClangPointerFunction(
                name,
                codeLocation,
                pointerSize,
                mappedType,
                mappedReturnType,
                parameters,
                isWrapped);
        }

        private ImmutableArray<ClangPointerFunctionParameter> ClangFunctionPointerParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangPointerFunctionParameter>();

            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var functionPointerParameter = ClangFunctionPointerParameter(node.Cursor, node.CursorParent);
                if (functionPointerParameter.Type.Name == "__va_list_tag")
                {
                    return ImmutableArray<ClangPointerFunctionParameter>.Empty;
                }

                builder.Add(functionPointerParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ClangPointerFunctionParameter ClangFunctionPointerParameter(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = ClangGetIdentifier(ClangNodeKind.PointerFunctionParameter, cursor);
            var type = ClangType(ClangNodeKind.PointerFunctionParameter, cursor, cursorParent);

            var result = new ClangPointerFunctionParameter(
                name,
                codeLocation,
                type);

            return result;
        }

        public ClangRecord ClangRecord(CXCursor cursor, CXCursor cursorParent, CXType type, string typeName)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var mappedType = ClangType(ClangNodeKind.Record, cursor, cursorParent, type, typeName);
            var recordFields = ClangRecordFields(cursor);
            var recordNestedRecords = ClangNestedNodes(cursor);

            return new ClangRecord(
                codeLocation,
                mappedType,
                recordFields,
                recordNestedRecords);
        }

        private ImmutableArray<ClangRecordField> ClangRecordFields(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangRecordField>();

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
            ImmutableArray<ClangRecordField>.Builder builder)
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
                builder[i - 1] = new ClangRecordField(fieldPrevious, padding);
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
                    builder[^1] = new ClangRecordField(fieldLast, padding);
                }
            }
        }

        private ClangRecordField ClangRecordField(CXCursor cursor, CXCursor cursorParent)
        {
            var name = cursor.GetName();
            var codeLocation = new ClangCodeLocation(cursor);
            var mappedType = ClangType(ClangNodeKind.RecordField, cursor, cursorParent);
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

            return new ClangRecordField(
                name,
                codeLocation,
                mappedType,
                offset,
                isUnNamedFunctionPointer);
        }

        private ImmutableArray<ClangNode> ClangNestedNodes(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangNode>();

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

        private ClangNode ClangNestedNode(CXCursor cursor, CXCursor cursorParent)
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

        private ClangNode ClangNestedStruct(CXCursor cursor, CXCursor cursorParent, CXType cursorType)
        {
            var declaration = clang_getTypeDeclaration(cursorType);
            var codeLocation = new ClangCodeLocation(declaration);
            var type = ClangType(ClangNodeKind.Record, declaration, cursor);

            var recordFields = ClangRecordFields(declaration);
            var recordNestedRecords = ClangNestedNodes(declaration);

            return new ClangRecord(
                codeLocation,
                type,
                recordFields,
                recordNestedRecords);
        }

        public ClangEnum ClangEnum(CXCursor cursor, CXCursor cursorParent, int depth)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var type = clang_getCursorType(cursor);
            var mappedType = ClangType(ClangNodeKind.Enum, cursor, cursorParent, type);
            var integerType = clang_getEnumDeclIntegerType(cursor);
            var mappedIntegerType = ClangType(ClangNodeKind.Enum, cursor, cursorParent, integerType);
            var enumValues = ClangEnumValues(cursor, depth + 1);

            var result = new ClangEnum(
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

        private ImmutableArray<ClangEnumValue> ClangEnumValues(CXCursor cursor, int depth)
        {
            var builder = ImmutableArray.CreateBuilder<ClangEnumValue>();

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

        private ClangEnumValue ClangEnumValue(CXCursor cursor, CXCursor cursorParent, int depth, string? name = null)
        {
            var value = clang_getEnumConstantDeclValue(cursor);
            var codeLocation = new ClangCodeLocation(cursor);
            name ??= ClangGetIdentifier(ClangNodeKind.EnumValue, cursor);

            var result = new ClangEnumValue(
                name,
                codeLocation,
                value);

            return result;
        }

        public ClangOpaqueType ClangOpaqueDataType(CXCursor cursor, CXType type, int depth)
        {
            var location = new ClangCodeLocation(cursor);
            var name = type.GetName();

            var result = new ClangOpaqueType(
                name,
                location);

            return result;
        }

        public ClangTypedef ClangTypedef(CXCursor cursor, CXCursor cursorParent, CXType type, CXType underlyingType)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var typeName = type.GetName();
            var mappedType = ClangType(ClangNodeKind.Typedef, cursor, cursorParent, type, typeName);
            var mappedUnderlyingType = ClangType(ClangNodeKind.Typedef, cursor, cursorParent, underlyingType);

            var result = new ClangTypedef(
                typeName,
                codeLocation,
                mappedType,
                mappedUnderlyingType);

            return result;
        }

        public ClangVariable ClangVariableExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = ClangGetIdentifier(ClangNodeKind.Variable, cursor);
            var clangType = ClangType(ClangNodeKind.Variable, cursor, cursorParent, type);

            var result = new ClangVariable(
                name,
                codeLocation,
                clangType);

            return result;
        }

        private ClangType ClangType(
            ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType? type = null, string? typeName = null)
        {
            var type2 = type ?? clang_getCursorType(cursor);
            var typeName2 = typeName ?? ClangNodeTypeName(nodeKind, cursor, cursorParent, type2);
            var originalName = type2.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(type2);
            var alignOf = (int) clang_Type_getAlignOf(type2);
            var arraySize = (int) clang_getArraySize(type2);
            var isSystemType = type2.IsSystem();

            var elementSize = sizeOf;
            if (type2.kind == CXTypeKind.CXType_ConstantArray)
            {
                var elementType = clang_getElementType(type2);
                elementSize = (int) clang_Type_getSizeOf(elementType);
            }

            return new ClangType(
                typeName2,
                originalName,
                sizeOf,
                alignOf,
                elementSize,
                arraySize,
                isSystemType);
        }

        private ClangType ClangFunctionExternType(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang_getCursorResultType(cursor);
            var result = ClangType(ClangNodeKind.FunctionResult, cursor, cursorParent, type);
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

        public static string ClangGetIdentifier(ClangNodeKind nodeKind, CXCursor cursor)
        {
            if (nodeKind != ClangNodeKind.Function &&
                nodeKind != ClangNodeKind.FunctionParameter &&
                nodeKind != ClangNodeKind.PointerFunction &&
                nodeKind != ClangNodeKind.PointerFunctionParameter &&
                nodeKind != ClangNodeKind.RecordField &&
                nodeKind != ClangNodeKind.EnumValue &&
                nodeKind != ClangNodeKind.Variable)
            {
                return string.Empty;
            }

            return cursor.GetName();
        }

        public string ClangNodeTypeName(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var result = nodeKind switch
            {
                ClangNodeKind.Variable => type.GetName(),
                ClangNodeKind.Function => cursor.GetName(),
                ClangNodeKind.FunctionResult => ClangTypeName(nodeKind, cursor, cursorParent, type),
                ClangNodeKind.FunctionParameter => ClangTypeName(nodeKind, cursor, cursorParent, type),
                ClangNodeKind.PointerFunction => ClangTypeNameFunctionPointer(type),
                ClangNodeKind.PointerFunctionResult => ClangTypeName(nodeKind, cursor, cursorParent, type),
                ClangNodeKind.PointerFunctionParameter => ClangTypeName(nodeKind, cursor, cursorParent, type),
                ClangNodeKind.Typedef => type.GetName(),
                ClangNodeKind.Record => ClangTypeNameRecord(cursor, cursorParent, type),
                ClangNodeKind.RecordField => ClangTypeNameRecordField(nodeKind, cursor, cursorParent, type),
                ClangNodeKind.Enum => type.GetName(),
                ClangNodeKind.OpaqueType => type.GetName(),
                _ => throw new ClangMapperException($"Unexpected node kind '{nodeKind}'.")
            };

            var isReadOnly = clang_isConstQualifiedType(type) > 0;
            if (isReadOnly)
            {
                result = result.Replace("const ", string.Empty).Trim();
            }

            return result;
        }

        private string ClangTypeNameRecordField(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var isFunctionPointer = type.kind == CXTypeKind.CXType_Pointer &&
                                    clang_getPointeeType(type).kind == CXTypeKind.CXType_FunctionProto;
            if (isFunctionPointer)
            {
                return cursor.GetName();
            }

            return ClangTypeName(nodeKind, cursor, cursorParent, type);
        }

        private string ClangTypeName(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
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

        private string ClangTypeNameConstantArray(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var elementType = clang_getArrayElementType(type);
            var result = ClangTypeName(nodeKind, cursor, cursorParent, elementType);
            return result;
        }

        private string ClangTypeNameIncompleteArray(ClangNodeKind nodeKind, CXType type, CXCursor cursor, CXCursor cursorParent)
        {
            var elementType = clang_getArrayElementType(type);
            var result = ClangTypeName(nodeKind, cursor, cursorParent, elementType);
            return $"{result}*";
        }

        private string ClangTypeNamePointer(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
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

        private static string ClangTypeNameTypedef(ClangNodeKind nodeKind, CXCursor cursor, CXCursor cursorParent, CXType type)
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
