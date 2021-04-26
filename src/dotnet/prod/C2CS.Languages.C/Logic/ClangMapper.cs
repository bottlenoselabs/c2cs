// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using lithiumtoast.NativeTools;
using static libclang;

namespace C2CS.Languages.C
{
    public class ClangMapper
    {
        private readonly Dictionary<uint, string> _functionPointerNamesByCursorHash = new();

        public ClangFunctionExtern MapFunctionExtern(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var callingConvention = MapFunctionCallingConvention(cursor);
            var returnType = MapFunctionExternType(cursor);
            var parameters = MapFunctionExternParameters(cursor);

            var result = new ClangFunctionExtern(
                name,
                codeLocation,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private ImmutableArray<ClangFunctionExternParameter> MapFunctionExternParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionExternParameter>();

            cursor.VisitChildren(0, (child, _, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_ParmDecl)
                {
                    return;
                }

                var functionExternParameter = MapFunctionExternParameter(child);
                builder.Add(functionExternParameter);
            });

            var result = builder.ToImmutable();
            return result;
        }

        private ClangFunctionExternParameter MapFunctionExternParameter(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var type = MapType(cursor);
            var isReadOnly = MapIsReadOnly(cursor);
            var isFunctionPointer = MapIsFunctionPointer(cursor);

            var result = new ClangFunctionExternParameter(
                name,
                codeLocation,
                type,
                isReadOnly,
                isFunctionPointer);

            return result;
        }

        public ClangFunctionPointer MapFunctionPointer(CXCursor cursor, CXCursor cursorParent)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapFunctionPointerName(cursor, cursorParent);
            var cursorType = clang_getCursorType(cursor);
            var pointerSize = (int)clang_Type_getSizeOf(cursorType);
            var returnType = MapFunctionPointerType(cursor);
            var parameters = MapFunctionPointerParameters(cursor);

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

            cursor.VisitChildren(0, (child, _, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_ParmDecl)
                {
                    return;
                }

                var functionPointerParameter = MapFunctionPointerParameter(child);
                builder.Add(functionPointerParameter);
            });

            var result = builder.ToImmutable();
            return result;
        }

        private ClangFunctionPointerParameter MapFunctionPointerParameter(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var type = MapType(cursor);
            var isReadOnly = MapIsReadOnly(cursor);

            var result = new ClangFunctionPointerParameter(
                name,
                codeLocation,
                type,
                isReadOnly);

            return result;
        }

        public ClangRecord MapRecord(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var type = MapRecordType(cursor);
            var recordFields = MapRecordFields(cursor);
            var recordNestedRecords = MapNestedRecords(cursor);

            var result = new ClangRecord(
                name,
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

            underlyingCursor.VisitChildren(0, (child, _, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                var recordField = MapRecordField(child);
                builder.Add(recordField);
            });

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

        private ClangRecordField MapRecordField(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var type = MapType(cursor);
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

            underlyingCursor.VisitChildren(0, (child, _, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                var type = clang_getCursorType(child);
                var typeDeclaration = clang_getTypeDeclaration(type);
                var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;
                if (!isAnonymous)
                {
                    return;
                }

                var record = MapNestedRecord(child);
                builder.Add(record);
            });

            var result = builder.ToImmutable();
            return result;
        }

        private ClangRecord MapNestedRecord(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var declaration = clang_getTypeDeclaration(cursorType);
            var codeLocation = MapCodeLocation(declaration);
            var type = MapType(cursor);
            var name = type.Name;
            var recordFields = MapRecordFields(declaration);
            var recordNestedRecords = MapNestedRecords(declaration);

            var result = new ClangRecord(
                name,
                codeLocation,
                type,
                recordFields,
                recordNestedRecords);

            return result;
        }

        public ClangEnum MapEnum(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var integerType = MapEnumIntegerType(cursor);
            var enumValues = MapEnumValues(cursor);

            var result = new ClangEnum(
                name,
                codeLocation,
                integerType,
                enumValues);

            return result;
        }

        private ClangType MapEnumIntegerType(CXCursor cursor)
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

            var result = MapType(enumIntegerType, cursor);
            return result;
        }

        private ImmutableArray<ClangEnumValue> MapEnumValues(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangEnumValue>();

            var underlyingCursor = MapUnderlyingCursor(cursor);

            underlyingCursor.VisitChildren(0, (child, _, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                var enumValue = MapEnumValue(child);
                builder.Add(enumValue);
            });

            var result = builder.ToImmutable();
            return result;
        }

        private ClangEnumValue MapEnumValue(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var value = clang_getEnumConstantDeclValue(cursor);

            var result = new ClangEnumValue(
                name,
                codeLocation,
                value);

            return result;
        }

        public ClangOpaqueDataType MapOpaqueDataType(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var result = new ClangOpaqueDataType(
                name,
                codeLocation);

            return result;
        }

        public ClangOpaquePointer MapOpaquePointer(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var pointerType = MapTypeOpaquePointer(cursor);

            var result = new ClangOpaquePointer(
                name,
                codeLocation,
                pointerType);

            return result;
        }

        public ClangAlias MapAlias(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = MapName(cursor);
            var underlyingType = MapAliasDataType(cursor);

            var result = new ClangAlias(
                name,
                codeLocation,
                underlyingType);

            return result;
        }

        private static unsafe string MapName(CXCursor clangCursor)
        {
            var spelling = clang_getCursorSpelling(clangCursor);

            var cString = clang_getCString(spelling);
            if ((IntPtr) cString == IntPtr.Zero)
            {
                return string.Empty;
            }

            var result = Native.MapString(cString);
            return result;
        }

        private static unsafe string MapName(CXType clangType)
        {
            var spelling = clang_getTypeSpelling(clangType);

            var cString = clang_getCString(spelling);
            if ((IntPtr) cString == IntPtr.Zero)
            {
                return string.Empty;
            }

            var result = Native.MapString(cString);
            return result;
        }

        private unsafe ClangCodeLocation MapCodeLocation(CXCursor cursor)
        {
            var location = clang_getCursorLocation(cursor);
            CXFile file;
            uint lineNumber;
            uint columnNumber;
            uint offset;
            clang_getFileLocation(location, &file, &lineNumber, &columnNumber, &offset);

            var handle = (IntPtr)file.Pointer;
            if (handle == IntPtr.Zero)
            {
                return default;
            }

            var fileName = clang_getFileName(file);
            var cString = clang_getCString(fileName);
            var fileNamePath = Native.MapString(cString);
            var fileNamePathFileName = Path.GetFileName(fileNamePath);
            var fileLine = (int) lineNumber;
            var fileTime = clang_getFileTime(file);
            var dateTime = new DateTime(1970, 1, 1).AddSeconds(fileTime);

            var result = new ClangCodeLocation(
                fileNamePathFileName,
                fileLine,
                dateTime);

            return result;
        }

        private ClangType MapType(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var result = MapType(cursorType, cursor);
            return result;
        }

        private ClangType MapType(CXType type, CXCursor cursor)
        {
            var typeName = MapTypeName(type, cursor);
            var originalName = type.GetName();
            var sizeOf = (int) clang_Type_getSizeOf(type);
            var alignOf = (int) clang_Type_getAlignOf(type);
            var arraySize = (int) clang_getArraySize(type);
            var isReadOnly = clang_isConstQualifiedType(type) > 0U;
            var isSystemType = type.IsSystemType();

            var result = new ClangType(
                typeName,
                originalName,
                sizeOf,
                alignOf,
                arraySize,
                isReadOnly,
                isSystemType);

            return result;
        }

        private ClangType MapFunctionExternType(CXCursor cursor)
        {
            var cursorResultType = clang_getCursorResultType(cursor);
            var result = MapType(cursorResultType, cursor);
            return result;
        }

        private ClangType MapFunctionPointerType(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            if (cursorType.kind == CXTypeKind.CXType_Typedef)
            {
                cursorType = clang_getTypedefDeclUnderlyingType(cursor);
            }

            if (cursorType.kind == CXTypeKind.CXType_Pointer)
            {
                cursorType = clang_getPointeeType(cursorType);
            }

            Debug.Assert(cursorType.kind == CXTypeKind.CXType_FunctionProto, "expected function proto");

            var type = clang_getResultType(cursorType);
            var result = MapType(type, cursor);
            return result;
        }

        private ClangType MapRecordType(CXCursor cursor)
        {
            var result = MapType(cursor);
            return result;
        }

        private ClangType MapAliasDataType(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var canonicalType = clang_getCanonicalType(cursorType);
            var result = MapType(canonicalType, cursor);
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
            var isReadOnly = clang_isConstQualifiedType(cursorType) > 0U;
            var isSystemType = cursorType.IsSystemType();

            var result = new ClangType(
                typeName,
                originalName,
                sizeOf,
                alignOf,
                arraySize,
                isReadOnly,
                isSystemType);

            return result;
        }

        private string MapFunctionPointerName(CXCursor cursor, CXCursor cursorParent)
        {
            var hash = clang_hashCursor(cursor);
            if (_functionPointerNamesByCursorHash.TryGetValue(hash, out var result))
            {
                return result;
            }

            var cursorName = MapName(cursor);

            var parentIsTranslationUnit = clang_isTranslationUnit(cursorParent.kind) > 0;
            if (parentIsTranslationUnit)
            {
                result = cursorName;
            }
            else
            {
                var parentName = MapName(cursorParent);
                result = $"{parentName}_{cursorName}";
            }

            _functionPointerNamesByCursorHash.Add(hash, result);

            return result;
        }

        private string MapTypeName(CXType clangType, CXCursor cursor)
        {
            var result = clangType.kind switch
            {
                CXTypeKind.CXType_Void => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Bool => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Char_S => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Char_U => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_UChar => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_UShort => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_UInt => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_ULong => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_ULongLong => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Short => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Int => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Long => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_LongLong => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Float => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Double => MapTypeNameBuiltIn(clangType),
                CXTypeKind.CXType_Pointer => MapTypeNamePointer(clangType, cursor),
                CXTypeKind.CXType_Typedef => MapTypeNameTypedef(clangType, cursor),
                CXTypeKind.CXType_Elaborated => MapTypeNameElaborated(clangType, cursor),
                CXTypeKind.CXType_Record => MapTypeNameRecord(clangType, cursor),
                CXTypeKind.CXType_Enum => MapTypeNameEnum(clangType),
                CXTypeKind.CXType_ConstantArray => MapTypeNameConstArray(clangType, cursor),
                CXTypeKind.CXType_FunctionProto => MapTypeNameFunctionProto(clangType, cursor),
                _ => throw new ClangMapperUnexpectedException()
            };

            var isReadOnly = clang_isConstQualifiedType(clangType) > 0;
            if (isReadOnly)
            {
                result = result.Replace("const ", string.Empty).Trim();
            }

            return result;
        }

        private string MapTypeNameBuiltIn(CXType clangType)
        {
            var sizeOf = clang_Type_getSizeOf(clangType);
            var name = MapName(clangType);

            var result = clangType.kind switch
            {
                CXTypeKind.CXType_Void => "void",
                CXTypeKind.CXType_Bool => "bool",
                CXTypeKind.CXType_Char_S => "sbyte",
                CXTypeKind.CXType_Char_U => "byte",
                CXTypeKind.CXType_UChar => "byte",
                CXTypeKind.CXType_UShort => "ushort",
                CXTypeKind.CXType_UInt => "uint",
                CXTypeKind.CXType_ULong => sizeOf == 8 ? "ulong" : "uint",
                CXTypeKind.CXType_ULongLong => "ulong",
                CXTypeKind.CXType_Short => "short",
                CXTypeKind.CXType_Int => "int",
                CXTypeKind.CXType_Long => sizeOf == 8 ? "long" : "int",
                CXTypeKind.CXType_LongLong => "long",
                CXTypeKind.CXType_Float => "float",
                CXTypeKind.CXType_Double => "double",
                CXTypeKind.CXType_Record => name.Replace("struct ", string.Empty).Trim(),
                CXTypeKind.CXType_Enum => name.Replace("enum ", string.Empty).Trim(),
                _ => throw new NotImplementedException()
            };

            return result;
        }

        private string MapTypeNamePointer(CXType clangType, CXCursor cursor)
        {
            var hash = clang_hashCursor(cursor);
            if (_functionPointerNamesByCursorHash.TryGetValue(hash, out var functionPointerName))
            {
                return functionPointerName;
            }

            var clangPointeeType = clang_getPointeeType(clangType);
            var pointeeTypeName = MapTypeName(clangPointeeType, cursor);
            string result = pointeeTypeName + "*";

            return result;
        }

        private string MapTypeNameTypedef(CXType clangType, CXCursor cursor)
        {
            string result;

            var declaration = clang_getTypeDeclaration(clangType);
            var isInSystem = declaration.IsSystemCursor();

            if (isInSystem)
            {
                var name = MapName(clangType);
                if (name == "FILE")
                {
                    result = "void";
                }
                else
                {
                    result = MapTypeNameTypedefSystem(clangType, cursor);
                }
            }
            else
            {
                result = MapTypeNameTypedefNonSystem(clangType);
            }

            return result;
        }

        private string MapTypeNameTypedefSystem(CXType clangType, CXCursor parentCursor)
        {
            var clangDeclaration = clang_getTypeDeclaration(clangType);
            var clangUnderlyingType = clang_getTypedefDeclUnderlyingType(clangDeclaration);

            string result;
            var kind = clangUnderlyingType.kind;
            switch (kind)
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
                    var underlyingCanonicalType = clang_getCanonicalType(clangUnderlyingType);
                    result = MapTypeNameBuiltIn(underlyingCanonicalType);
                    break;
                default:
                    var canonicalType = clang_getCanonicalType(clangType);
                    result = MapTypeName(canonicalType, parentCursor);
                    break;
            }

            return result;
        }

        private unsafe string MapTypeNameTypedefNonSystem(CXType clangType)
        {
            var clangTypeCanonical = clang_getCanonicalType(clangType);

            if (clangTypeCanonical.kind == CXTypeKind.CXType_Pointer)
            {
                var cursor = clang_getTypeDeclaration(clangType);
                var parent = clang_getCursorSemanticParent(cursor);
                return MapFunctionPointerName(cursor, parent);
            }

            var typedefName = clang_getTypedefName(clangType);
            var cString = clang_getCString(typedefName);
            var result = Native.MapString(cString);
            return result;
        }

        private string MapTypeNameElaborated(CXType clangType, CXCursor cursor)
        {
            var clangNamedType = clang_Type_getNamedType(clangType);
            var result = MapTypeName(clangNamedType, cursor);

            return result;
        }

        private string MapTypeNameRecord(CXType clangType, CXCursor cursor)
        {
            string result;
            var clangRecord = clang_getTypeDeclaration(clangType);
            var name = MapName(clangType);
            var cursorName = MapName(cursor);

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            var isAnonymous = clang_Cursor_isAnonymous(clangRecord) > 0;
            if (isAnonymous)
            {
                if (clangRecord.kind == CXCursorKind.CXCursor_UnionDecl)
                {
                    result = $"Anonymous_Union_{cursorName}";
                }
                else
                {
                    result = $"Anonymous_Struct_{cursorName}";
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

        private string MapTypeNameEnum(CXType clangType)
        {
            var result = MapName(clangType);

            if (result.Contains("enum "))
            {
                result = result.Replace("enum ", string.Empty);
            }

            return result;
        }

        private string MapTypeNameConstArray(CXType clangType, CXCursor cursor)
        {
            var elementType = clang_getArrayElementType(clangType);
            var result = MapTypeName(elementType, cursor);

            return result;
        }

        private string MapTypeNameFunctionProto(CXType clangType, CXCursor cursor)
        {
            var hash = clang_hashCursor(cursor);
            return _functionPointerNamesByCursorHash[hash];
        }

        private ClangFunctionExternCallingConvention MapFunctionCallingConvention(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var result = MapFunctionCallingConvention(cursorType);
            return result;
        }

        private ClangFunctionExternCallingConvention MapFunctionCallingConvention(CXType type)
        {
            var callingConvention = clang_getFunctionTypeCallingConv(type);
            var result = MapFunctionExternCallingConvention(callingConvention);
            return result;
        }

        private static ClangFunctionExternCallingConvention MapFunctionExternCallingConvention(
            CXCallingConv callingConvention)
        {
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => C.ClangFunctionExternCallingConvention.C,
                _ => throw new ArgumentOutOfRangeException(nameof(callingConvention), callingConvention, null)
            };

            return result;
        }

        private static bool MapIsReadOnly(CXCursor cursor)
        {
            var cursorType = clang_getCursorType(cursor);
            var result = MapIsReadOnly(cursorType);
            return result;
        }

        private static bool MapIsReadOnly(CXType type)
        {
            var canonicalType = clang_getCanonicalType(type);
            var result = clang_isConstQualifiedType(canonicalType) > 0;
            return result;
        }

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

        public class ClangMapperUnexpectedException : Exception
        {
            public ClangMapperUnexpectedException()
                : base("The header file used has unforeseen conditions. Please create an issue on GitHub with the stack trace along with the header file.")
            {
            }
        }
    }
}
