// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangMapper
    {
        private readonly Dictionary<string, string> _systemTypeNameMappings = new();

        public ClangFunctionExtern MapFunctionExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var identifier = GetIdentifier(ClangNodeKind.VariableExtern, cursor);
            var callingConvention = MapFunctionCallingConvention(type);
            var returnType = MapFunctionExternType(cursor, cursorParent);
            var parameters = MapFunctionExternParameters(cursor);
            var location = new ClangCodeLocation(cursor);

            var result = new ClangFunctionExtern(
                identifier,
                location,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private static ClangFunctionExternCallingConvention MapFunctionCallingConvention(CXType type)
        {
            var callingConvention = clang_getFunctionTypeCallingConv(type);
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => ClangFunctionExternCallingConvention.C,
                _ => throw new ClangExplorerException($"Unknown calling convention '{callingConvention}'.")
            };

            return result;
        }

        private ImmutableArray<ClangFunctionExternParameter> MapFunctionExternParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionExternParameter>();

            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var functionExternParameter = MapFunctionExternParameter(node.Cursor, node.CursorParent);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ClangFunctionExternParameter MapFunctionExternParameter(
            CXCursor cursor, CXCursor cursorParent, string? name = null)
        {
            name ??= GetIdentifier(ClangNodeKind.FunctionExternParameter, cursor);
            var type = MapType(cursor, cursorParent);
            var isFunctionPointer = MapIsFunctionPointer(cursor);
            var location = new ClangCodeLocation(cursor);

            var result = new ClangFunctionExternParameter(
                name,
                location,
                type,
                isFunctionPointer);

            return result;
        }

        public ClangFunctionPointer MapFunctionPointer(CXCursor cursor, CXCursor cursorParent)
        {
            var name = GetTypeName(cursor);
            var cursorType = clang_getCursorType(cursor);
            var pointerSize = (int)clang_Type_getSizeOf(cursorType);
            var returnType = MapType(cursor, cursorParent);
            var parameters = MapFunctionPointerParameters(cursor);
            var codeLocation = new ClangCodeLocation(cursor);

            var result = new ClangFunctionPointer(
                name,
                codeLocation,
                pointerSize,
                returnType,
                parameters);

            return result;
        }

        private ImmutableArray<ClangFunctionPointerParameter> MapFunctionPointerParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionPointerParameter>();

            var nodes = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var node in nodes)
            {
                var functionPointerParameter = MapFunctionPointerParameter(node.Cursor, node.CursorParent);
                if (functionPointerParameter.Type.Name == "__va_list_tag")
                {
                    return ImmutableArray<ClangFunctionPointerParameter>.Empty;
                }

                builder.Add(functionPointerParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ClangFunctionPointerParameter MapFunctionPointerParameter(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = GetIdentifier(ClangNodeKind.FunctionPointerParameter, cursor);
            var type = MapType(cursor, cursorParent);

            var result = new ClangFunctionPointerParameter(
                name,
                codeLocation,
                type);

            return result;
        }

        public ClangRecord MapRecord(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var type = MapType(cursor, cursorParent);
            var recordFields = MapRecordFields(cursor);
            var recordNestedRecords = MapNestedRecords(cursor);

            var result = new ClangRecord(
                codeLocation,
                type,
                recordFields,
                recordNestedRecords);

            return result;
        }

        private ImmutableArray<ClangRecordField> MapRecordFields(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangRecordField>();

            var underlyingCursor = MapUnderlyingCursor(cursor);

            var nodes = underlyingCursor.GetDescendents((child, parent) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return false;
                }

                return true;
            });

            foreach (var node in nodes)
            {
                var recordField = MapRecordField(node.Cursor, node.CursorParent);
                builder.Add(recordField);
            }

            CalculatePaddingForStructFields(cursor, builder);

            var result = builder.ToImmutable();
            return result;
        }

        private static void CalculatePaddingForStructFields(
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

        private ClangRecordField MapRecordField(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = GetIdentifier(ClangNodeKind.RecordField, cursor);
            var type = MapType(cursor, cursorParent);
            var offset = (int) (clang_Cursor_getOffsetOfField(cursor) / 8);

            var result = new ClangRecordField(
                name,
                codeLocation,
                type,
                offset);

            return result;
        }

        private ImmutableArray<ClangRecord> MapNestedRecords(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangRecord>();

            var underlyingCursor = MapUnderlyingCursor(cursor);

            var nodes = underlyingCursor.GetDescendents((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return false;
                }

                var type = clang_getCursorType(child);
                var typeDeclaration = clang_getTypeDeclaration(type);
                var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;
                if (!isAnonymous)
                {
                    return false;
                }

                return true;
            });

            foreach (var node in nodes)
            {
                var record = MapNestedRecord(node.Cursor, node.CursorParent);
                builder.Add(record);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ClangRecord MapNestedRecord(CXCursor cursor, CXCursor cursorParent)
        {
            var cursorType = clang_getCursorType(cursor);
            var declaration = clang_getTypeDeclaration(cursorType);
            var codeLocation = new ClangCodeLocation(declaration);
            var type = MapType(cursor, cursorParent);
            var recordFields = MapRecordFields(declaration);
            var recordNestedRecords = MapNestedRecords(declaration);

            var result = new ClangRecord(
                codeLocation,
                type,
                recordFields,
                recordNestedRecords);

            return result;
        }

        public ClangEnum MapEnum(CXCursor cursor, CXCursor cursorParent, CXType integerType)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = GetTypeName(cursor);
            var type = MapType(cursor, cursorParent, integerType);
            var enumValues = MapEnumValues(cursor);

            var result = new ClangEnum(
                name,
                codeLocation,
                type,
                enumValues);

            return result;
        }

        private ClangType MapEnumIntegerType(CXCursor cursor, CXCursor cursorParent)
        {
            CXType enumIntegerType;

            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                var underlyingTypeDeclaration = clang_getTypeDeclaration(underlyingType);
                enumIntegerType = clang_getEnumDeclIntegerType(underlyingTypeDeclaration);
            }
            else
            {
                enumIntegerType = clang_getEnumDeclIntegerType(cursor);
            }

            var result = MapType( cursor, cursorParent, enumIntegerType);
            return result;
        }

        private static ImmutableArray<ClangEnumValue> MapEnumValues(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangEnumValue>();

            var underlyingCursor = MapUnderlyingCursor(cursor);

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
                var enumValue = MapEnumValue(node.Cursor);
                builder.Add(enumValue);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static ClangEnumValue MapEnumValue(CXCursor cursor, string? name = null)
        {
            var value = clang_getEnumConstantDeclValue(cursor);
            var location = new ClangCodeLocation(cursor);

            name ??= GetIdentifier(ClangNodeKind.EnumValue, cursor);

            var result = new ClangEnumValue(
                name,
                location,
                value);

            return result;
        }

        public ClangOpaqueType MapOpaqueDataType(CXCursor cursor, CXCursor cursorParent)
        {
            var location = new ClangCodeLocation(cursor);
            var name = GetTypeName(cursor);

            var result = new ClangOpaqueType(
                name,
                location);

            return result;
        }

        // public ClangOpaquePointer MapOpaquePointer(CXCursor cursor)
        // {
        //     var codeLocation = MapCodeLocation(cursor);
        //     var name = MapName(cursor);
        //     var pointerType = MapTypeOpaquePointer(cursor);
        //
        //     var result = new ClangOpaquePointer(
        //         name,
        //         codeLocation,
        //         pointerType);
        //
        //     return result;
        // }

        public ClangTypedef MapTypedef(CXCursor cursor, CXCursor cursorParent, CXType underlyingType)
        {
            var codeLocation = new ClangCodeLocation(cursor);

            var name = GetTypeName(cursor, underlyingType);
            var type = MapType(cursor, cursorParent, underlyingType);

            var result = new ClangTypedef(
                name,
                codeLocation,
                type);

            return result;
        }

        public ClangVariable MapVariableExtern(CXCursor cursor, CXCursor cursorParent, CXType type)
        {
            var codeLocation = new ClangCodeLocation(cursor);
            var name = GetIdentifier(ClangNodeKind.VariableExtern, cursor);
            var clangType = MapType(cursor, cursorParent, type);

            var result = new ClangVariable(
                name,
                codeLocation,
                clangType);

            return result;
        }

        private static string MapName(CXType clangType)
        {
            return clangType
                .GetName()
                .Replace("const", string.Empty)
                .Replace(" ", string.Empty);
        }

        private static string MapName(CXCursor cursor, CXType clangType)
        {
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var name = cursor.GetName();
            }

            return clangType
                .GetName()
                .Replace("const ", string.Empty);
        }

        private static ClangType MapType(CXCursor cursor, CXCursor cursorParent, CXType? type = null)
        {
            var type2 = type ?? clang_getCursorType(cursor);
            var typeName = GetTypeName(cursor, type2);
            var originalName = type2.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(type2);
            var alignOf = (int) clang_Type_getAlignOf(type2);
            var arraySize = (int) clang_getArraySize(type2);
            var isSystemType = type2.IsSystem();

            var result = new ClangType(
                typeName,
                originalName,
                sizeOf,
                alignOf,
                arraySize,
                isSystemType);

            return result;
        }

        private ClangType MapFunctionExternType(CXCursor cursor, CXCursor cursorParent)
        {
            var type = clang_getCursorResultType(cursor);
            var result = MapType(cursor, cursorParent, type);
            return result;
        }

        private ClangType MapTypedefType(CXCursor cursor, CXCursor cursorParent)
        {
            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var result = MapType(cursor, cursorParent, underlyingType);
            return result;
        }

        private ClangType MapTypeOpaquePointer(CXCursor cursor)
        {
            const string typeName = "void*";
            var cursorType = clang_getCursorType(cursor);
            var canonicalType = clang_getCanonicalType(cursorType);
            var originalName = canonicalType.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(cursorType);
            var alignOf = (int) clang_Type_getAlignOf(cursorType);
            var arraySize = (int) clang_getArraySize(cursorType);
            var isSystemType = cursorType.IsSystem();

            var result = new ClangType(
                typeName,
                originalName,
                sizeOf,
                alignOf,
                arraySize,
                isSystemType);

            return result;
        }

        // private string MapTypeName(CXType clangType, CXCursor cursor, CXCursor? cursorParent)
        // {
        //     string? result;
        //     switch (clangType.kind)
        //     {
        //         case CXTypeKind.CXType_Void:
        //         case CXTypeKind.CXType_Bool:
        //         case CXTypeKind.CXType_Char_S:
        //         case CXTypeKind.CXType_Char_U:
        //         case CXTypeKind.CXType_UChar:
        //         case CXTypeKind.CXType_UShort:
        //         case CXTypeKind.CXType_UInt:
        //         case CXTypeKind.CXType_ULong:
        //         case CXTypeKind.CXType_ULongLong:
        //         case CXTypeKind.CXType_Short:
        //         case CXTypeKind.CXType_Int:
        //         case CXTypeKind.CXType_Long:
        //         case CXTypeKind.CXType_LongLong:
        //         case CXTypeKind.CXType_Float:
        //         case CXTypeKind.CXType_Double:
        //             result = MapTypeNameBuiltIn(clangType);
        //             break;
        //         case CXTypeKind.CXType_Pointer:
        //             result = MapTypeNamePointer(clangType, cursor, cursorParent);
        //             break;
        //         case CXTypeKind.CXType_Typedef:
        //             result = MapTypeNameTypedef(clangType, cursor);
        //             break;
        //         case CXTypeKind.CXType_Elaborated:
        //             result = MapTypeNameElaborated(clangType, cursor, cursorParent);
        //             break;
        //         case CXTypeKind.CXType_Record:
        //             result = MapTypeNameRecord(clangType, cursor);
        //             break;
        //         case CXTypeKind.CXType_Enum:
        //             result = MapTypeNameEnum(clangType);
        //             break;
        //         case CXTypeKind.CXType_ConstantArray:
        //             result = MapTypeNameConstArray(clangType, cursor, cursorParent);
        //             break;
        //         case CXTypeKind.CXType_IncompleteArray:
        //             result = MapTypeNameIncompleteArray(clangType, cursor, cursorParent);
        //             break;
        //         case CXTypeKind.CXType_FunctionProto:
        //             result = MapTypeNameFunctionProto(clangType, cursor, cursorParent);
        //             break;
        //         case CXTypeKind.CXType_Attributed:
        //             var modifiedType = clang_Type_getModifiedType(clangType);
        //             result = MapTypeName(modifiedType, cursor, cursorParent);
        //             break;
        //         default:
        //             var up = new ClangMapperException();
        //             throw up;
        //     }
        //
        //     var isReadOnly = clang_isConstQualifiedType(clangType) > 0;
        //     if (isReadOnly)
        //     {
        //         result = result.Replace("const ", string.Empty).Trim();
        //     }
        //
        //     return result;
        // }

        // private string MapTypeNameTypedefSystem(CXType clangType, CXCursor cursorParent)
        // {
        //     var declaration = clang_getTypeDeclaration(clangType);
        //     var underlyingType = clang_getTypedefDeclUnderlyingType(declaration);
        //
        //     var name = MapName(clangType);
        //     if (_systemTypeNameMappings.TryGetValue(name, out var mappedName))
        //     {
        //         return mappedName;
        //     }
        //
        //     string result;
        //     switch (name)
        //     {
        //         case "FILE":
        //             result = "void";
        //             break;
        //         default:
        //         {
        //             var kind = underlyingType.kind;
        //             switch (kind)
        //             {
        //                 case CXTypeKind.CXType_Void:
        //                 case CXTypeKind.CXType_Bool:
        //                 case CXTypeKind.CXType_Char_S:
        //                 case CXTypeKind.CXType_Char_U:
        //                 case CXTypeKind.CXType_UChar:
        //                 case CXTypeKind.CXType_UShort:
        //                 case CXTypeKind.CXType_UInt:
        //                 case CXTypeKind.CXType_ULong:
        //                 case CXTypeKind.CXType_ULongLong:
        //                 case CXTypeKind.CXType_Short:
        //                 case CXTypeKind.CXType_Int:
        //                 case CXTypeKind.CXType_Long:
        //                 case CXTypeKind.CXType_LongLong:
        //                 case CXTypeKind.CXType_Float:
        //                 case CXTypeKind.CXType_Double:
        //                     // var underlyingCanonicalType = clang_getCanonicalType(underlyingType);
        //                     // result = MapTypeNameBuiltIn(underlyingCanonicalType);
        //                     break;
        //                 default:
        //                     // var canonicalType = clang_getCanonicalType(clangType);
        //                     // result = MapTypeName(canonicalType, cursorParent, null);
        //                     break;
        //             }
        //
        //             break;
        //         }
        //     }
        //
        //     // _systemTypeNameMappings.Add(name, result);
        //
        //     return result;
        // }

        // private unsafe string MapTypeNameTypedefNonSystem(CXType clangType)
        // {
        //     var clangTypeCanonical = clang_getCanonicalType(clangType);
        //
        //     if (clangTypeCanonical.kind == CXTypeKind.CXType_Pointer)
        //     {
        //         var cursor = clang_getTypeDeclaration(clangType);
        //         var parent = clang_getCursorSemanticParent(cursor);
        //         return MapFunctionPointerName(cursor, parent);
        //     }
        //
        //     var typedefName = clang_getTypedefName(clangType);
        //     var cString = clang_getCString(typedefName);
        //     var result = NativeRuntime.MapString(cString);
        //     return result;
        // }
        //
        // private string MapTypeNameElaborated(CXType clangType, CXCursor cursor, CXCursor? cursorParent)
        // {
        //     var clangNamedType = clang_Type_getNamedType(clangType);
        //     var result = MapTypeName(clangNamedType, cursor, cursorParent);
        //
        //     return result;
        // }
        //
        // private string MapTypeNameRecord(CXType clangType, CXCursor cursor)
        // {
        //     string result;
        //     var clangRecord = clang_getTypeDeclaration(clangType);
        //     var name = MapName(clangType);
        //     var cursorName = MapName(cursor);
        //
        //     // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        //     var isAnonymous = clang_Cursor_isAnonymous(clangRecord) > 0;
        //     if (isAnonymous)
        //     {
        //         if (clangRecord.kind == CXCursorKind.CXCursor_UnionDecl)
        //         {
        //             result = $"Anonymous_Union_{cursorName}";
        //         }
        //         else
        //         {
        //             result = $"Anonymous_Struct_{cursorName}";
        //         }
        //     }
        //     else
        //     {
        //         if (name.Contains("union "))
        //         {
        //             result = name.Replace("union ", string.Empty);
        //         }
        //         else if (name.Contains("struct "))
        //         {
        //             result = name.Replace("struct ", string.Empty);
        //         }
        //         else
        //         {
        //             result = name;
        //         }
        //     }
        //
        //     return result;
        // }
        //
        // private string MapTypeNameEnum(CXType clangType)
        // {
        //     var result = MapName(clangType);
        //
        //     if (result.Contains("enum "))
        //     {
        //         result = result.Replace("enum ", string.Empty);
        //     }
        //
        //     return result;
        // }
        //
        // private string MapTypeNameConstArray(CXType clangType, CXCursor cursor, CXCursor? cursorParent)
        // {
        //     var elementType = clang_getArrayElementType(clangType);
        //     var result = MapTypeName(elementType, cursor, cursorParent);
        //
        //     return result;
        // }
        //
        // private string MapTypeNameIncompleteArray(CXType clangType, CXCursor cursor, CXCursor? cursorParent)
        // {
        //     var elementType = clang_getArrayElementType(clangType);
        //     var result = MapTypeName(elementType, cursor, cursorParent);
        //
        //     return result + "*";
        // }
        //
        // private string MapTypeNameFunctionProto(CXType clangType, CXCursor cursor, CXCursor? cursorParent)
        // {
        //     return MapFunctionPointerName(cursor, cursorParent);
        // }

        private static bool MapIsFunctionPointer(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var result = MapIsFunctionPointer(cursorType);
            return result;
        }

        private static bool MapIsFunctionPointer(CXType cursorType)
        {
            if (cursorType.kind != CXTypeKind.CXType_Pointer)
            {
                return false;
            }

            var pointeeType = clang_getPointeeType(cursorType);
            var result = pointeeType.kind == CXTypeKind.CXType_FunctionProto;
            return result;
        }

        private static CXCursor MapUnderlyingCursor(CXCursor cursor)
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

        public static string GetIdentifier(ClangNodeKind nodeKind, CXCursor cursor)
        {
            if (nodeKind == ClangNodeKind.FunctionExtern)
            {
                throw new NotImplementedException();
            }

            if (nodeKind != ClangNodeKind.FunctionExtern &&
                nodeKind != ClangNodeKind.FunctionExternParameter &&
                nodeKind != ClangNodeKind.FunctionPointer &&
                nodeKind != ClangNodeKind.FunctionPointerParameter &&
                nodeKind != ClangNodeKind.RecordField &&
                nodeKind != ClangNodeKind.EnumValue &&
                nodeKind != ClangNodeKind.VariableExtern)
            {
                return string.Empty;
            }

            return cursor.GetName();
        }

        public static string GetTypeName(CXCursor cursor, CXType? type = null)
        {
            var type2 = type ?? clang_getCursorType(cursor);

            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                return cursor.GetName();
            }

            var typeCursor = clang_getTypeDeclaration(type2);
            if (typeCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                return GetTypeNameForType(type2);
            }

            return typeCursor.GetName();
        }

        private static string GetTypeNameForType(CXType type)
        {
            var sizeOf = clang_Type_getSizeOf(type);

            string? result;
            switch (type.kind)
            {
                case CXTypeKind.CXType_Void:
                    result = "void";
                    break;
                case CXTypeKind.CXType_Bool:
                    result = "bool";
                    break;
                case CXTypeKind.CXType_Char_S:
                    result = "sbyte";
                    break;
                case CXTypeKind.CXType_Char_U:
                case CXTypeKind.CXType_UChar:
                    result = "byte";
                    break;
                case CXTypeKind.CXType_UShort:
                    result = "ushort";
                    break;
                case CXTypeKind.CXType_UInt:
                    result = "uint";
                    break;
                case CXTypeKind.CXType_ULong:
                    result = sizeOf == 8 ? "ulong" : "uint";
                    break;
                case CXTypeKind.CXType_ULongLong:
                    result = "ulong";
                    break;
                case CXTypeKind.CXType_Short:
                    result = "short";
                    break;
                case CXTypeKind.CXType_Int:
                    result = "int";
                    break;
                case CXTypeKind.CXType_Long:
                    result = sizeOf == 8 ? "long" : "int";
                    break;
                case CXTypeKind.CXType_LongLong:
                    result = "long";
                    break;
                case CXTypeKind.CXType_Float:
                    result = "float";
                    break;
                case CXTypeKind.CXType_Double:
                    result = "double";
                    break;
                default:
                    result = type.GetName();
                    break;
            }

            return result;
        }
    }
}
