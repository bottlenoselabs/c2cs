// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using ClangSharp.Interop;

namespace C2CS.Languages.C
{
    public class ClangMapper
    {
        private readonly Dictionary<CXType, string> _functionPointerNamesByClangType = new();

        public ClangFunctionExtern MapFunctionExtern(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = cursor.Spelling.CString;
            var callingConvention = MapFunctionCallingConvention(cursor.Type.FunctionTypeCallingConv);
            var returnType = MapType(cursor.ResultType, cursor);
            var parameters = MapFunctionExternParameters(cursor);

            var result = new ClangFunctionExtern(
                name,
                codeLocation,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private ClangFunctionExternCallingConvention MapFunctionCallingConvention(CXCallingConv callingConvention)
        {
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => ClangFunctionExternCallingConvention.C,
                _ => throw new ArgumentOutOfRangeException(nameof(callingConvention), callingConvention, null)
            };

            return result;
        }

        private ImmutableArray<ClangFunctionExternParameter> MapFunctionExternParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionExternParameter>();

            cursor.VisitChildren((child, _) =>
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
            var name = cursor.Spelling.CString;
            var type = MapType(cursor.Type, cursor);
            var isReadOnly = cursor.Type.CanonicalType.IsConstQualified;
            var isFunctionPointer = cursor.Type.kind == CXTypeKind.CXType_Pointer &&
                                    cursor.Type.PointeeType.kind == CXTypeKind.CXType_FunctionProto;

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
            var cursorType = GetFunctionProtoType(cursor);
            var codeLocation = MapCodeLocation(cursor);
            var name = MapFunctionPointerName(cursor, cursorParent);
            var pointerSize = (int)cursor.Type.SizeOf;
            var returnType = MapType(cursorType.ResultType, cursor);
            var parameters = MapFunctionPointerParameters(cursor);

            var result = new ClangFunctionPointer(
                name,
                codeLocation,
                pointerSize,
                returnType,
                parameters);

            return result;
        }

        private static CXType GetFunctionProtoType(CXCursor cursor)
        {
            var type = cursor.Type;
            if (type.kind == CXTypeKind.CXType_Typedef)
            {
                type = cursor.TypedefDeclUnderlyingType;
            }

            if (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = type.PointeeType;
            }

            Debug.Assert(type.kind == CXTypeKind.CXType_FunctionProto, "expected function proto");

            return type;
        }

        private ImmutableArray<ClangFunctionPointerParameter> MapFunctionPointerParameters(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangFunctionPointerParameter>();

            cursor.VisitChildren((child, _) =>
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
            var name = cursor.Spelling.CString;
            var type = MapType(cursor.Type, cursor);
            var isReadOnly = cursor.Type.CanonicalType.IsConstQualified;

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
            var name = cursor.Spelling.CString;
            var type = MapType(cursor.Type, cursor);
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

            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingCursor = cursor.TypedefDeclUnderlyingType.NamedType.Declaration;
            }

            underlyingCursor.VisitChildren((child, _) =>
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
                var recordSize = (int) cursor.Type.SizeOf;
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
            var recordFieldType = clang.getCursorType(cursor);
            var name = cursor.Spelling.CString;
            var type = MapType(recordFieldType, cursor);
            var offset = (int) (cursor.OffsetOfField / 8);

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

            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingCursor = cursor.TypedefDeclUnderlyingType.NamedType.Declaration;
            }

            underlyingCursor.VisitChildren((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                var type = child.Type;
                var typeDeclaration = type.Declaration;
                var isAnonymous = typeDeclaration.IsAnonymous;
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
            var cursorType = cursor.Type;
            var declaration = cursorType.Declaration;
            var codeLocation = MapCodeLocation(declaration);
            var type = MapType(cursor.Type, cursor);
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
            var name = cursor.Spelling.CString;
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
                var underlyingType = cursor.TypedefDeclUnderlyingType;
                var underlyingTypeDeclaration = underlyingType.Declaration;
                enumIntegerType = underlyingTypeDeclaration.EnumDecl_IntegerType;
            }
            else
            {
                enumIntegerType = cursor.EnumDecl_IntegerType;
            }

            var result = MapType(enumIntegerType, cursor);
            return result;
        }

        private ImmutableArray<ClangEnumValue> MapEnumValues(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<ClangEnumValue>();

            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                underlyingCursor = cursor.TypedefDeclUnderlyingType.NamedType.Declaration;
            }

            underlyingCursor.VisitChildren((child, cursorParent) =>
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
            var name = cursor.Spelling.CString;
            var value = cursor.EnumConstantDeclValue;

            var result = new ClangEnumValue(
                name,
                codeLocation,
                value);

            return result;
        }

        public ClangOpaqueDataType MapOpaqueDataType(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = cursor.Spelling.CString;
            var result = new ClangOpaqueDataType(
                name,
                codeLocation);

            return result;
        }

        public ClangOpaquePointer MapOpaquePointer(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = cursor.Spelling.CString;
            var pointerType = MapTypeOpaquePointer(cursor.Type.CanonicalType);

            var result = new ClangOpaquePointer(
                name,
                codeLocation,
                pointerType);

            return result;
        }

        public ClangAliasDataType MapAliasDataType(CXCursor cursor)
        {
            var codeLocation = MapCodeLocation(cursor);
            var name = cursor.Spelling.CString;
            var underlyingType = MapType(cursor.Type.CanonicalType, cursor);

            var result = new ClangAliasDataType(
                name,
                codeLocation,
                underlyingType);

            return result;
        }

        private ClangCodeLocation MapCodeLocation(CXCursor cursor)
        {
            cursor.Location.GetFileLocation(
                out var file, out var lineNumber, out _, out _);

            if (file.Handle == IntPtr.Zero)
            {
                return default;
            }

            var fileName = Path.GetFileName(file.Name.CString);
            var fileLine = (int) lineNumber;
            var dateTime = new DateTime(1970, 1, 1).AddSeconds(file.Time);

            var result = new ClangCodeLocation(
                fileName,
                fileLine,
                dateTime);

            return result;
        }

        private ClangType MapType(CXType type, CXCursor cursor)
        {
            var typeName = MapTypeName(type, cursor.Spelling.CString);
            var originalName = type.Spelling.CString;
            var sizeOf = (int) clang.Type_getSizeOf(type);
            var alignOf = (int) clang.Type_getAlignOf(type);
            var arraySize = (int) type.ArraySize;
            var isReadOnly = clang.isConstQualifiedType(type) > 0U;
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

        private ClangType MapTypeOpaquePointer(CXType type)
        {
            const string typeName = "void*";
            var originalName = type.Spelling.CString;
            var sizeOf = (int) clang.Type_getSizeOf(type);
            var alignOf = (int) clang.Type_getAlignOf(type);
            var arraySize = (int) type.ArraySize;
            var isReadOnly = clang.isConstQualifiedType(type) > 0U;
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

        private string MapFunctionPointerName(CXCursor cursor, CXCursor cursorParent)
        {
            string result;

            var cursorName = cursor.Spelling.CString;

            if (cursorParent.IsTranslationUnit)
            {
                result = cursorName;
            }
            else
            {
                var cursorParentName = cursorParent.Spelling.CString;
                result = $"{cursorParentName}_{cursorName}";
            }

            var typeCanonical = cursor.Type.CanonicalType.PointeeType;
            _functionPointerNamesByClangType.Add(typeCanonical, result);

            return result;
        }

        private string MapTypeName(CXType clangType, string cursorName)
        {
            var typeClass = clangType.TypeClass;
            var result = typeClass switch
            {
                CX_TypeClass.CX_TypeClass_Builtin => MapTypeNameBuiltIn(clangType),
                CX_TypeClass.CX_TypeClass_Pointer => MapTypeNamePointer(clangType, cursorName),
                CX_TypeClass.CX_TypeClass_Typedef => MapTypeNameTypedef(clangType, cursorName),
                CX_TypeClass.CX_TypeClass_Elaborated => MapTypeNameElaborated(clangType, cursorName),
                CX_TypeClass.CX_TypeClass_Record => MapTypeNameRecord(clangType, cursorName),
                CX_TypeClass.CX_TypeClass_Enum => MapTypeNameEnum(clangType),
                CX_TypeClass.CX_TypeClass_ConstantArray => MapTypeNameConstArray(clangType, cursorName),
                CX_TypeClass.CX_TypeClass_FunctionProto => MapTypeNameFunctionProto(clangType),
                _ => throw new ClangMapperUnexpectedException()
            };

            if (clangType.IsConstQualified)
            {
                result = result.Replace("const ", string.Empty).Trim();
            }

            return result;
        }

        private string MapTypeNameBuiltIn(CXType clangType)
        {
            var result = clangType.kind switch
            {
                CXTypeKind.CXType_Void => "void",
                CXTypeKind.CXType_Bool => "bool",
                CXTypeKind.CXType_Char_S => "sbyte",
                CXTypeKind.CXType_Char_U => "byte",
                CXTypeKind.CXType_UChar => "byte",
                CXTypeKind.CXType_UShort => "ushort",
                CXTypeKind.CXType_UInt => "uint",
                CXTypeKind.CXType_ULong => clangType.SizeOf == 8 ? "ulong" : "uint",
                CXTypeKind.CXType_ULongLong => "ulong",
                CXTypeKind.CXType_Short => "short",
                CXTypeKind.CXType_Int => "int",
                CXTypeKind.CXType_Long => clangType.SizeOf == 8 ? "long" : "int",
                CXTypeKind.CXType_LongLong => "long",
                CXTypeKind.CXType_Float => "float",
                CXTypeKind.CXType_Double => "double",
                CXTypeKind.CXType_Record => clangType.Spelling.CString.Replace("struct ", string.Empty).Trim(),
                CXTypeKind.CXType_Enum => clangType.Spelling.CString.Replace("enum ", string.Empty).Trim(),
                _ => throw new NotImplementedException()
            };

            return result;
        }

        private string MapTypeNamePointer(CXType clangType, string cursorName)
        {
            if (TryClangGetFunctionPointerType(clangType, out CXType clangFunctionPointerType))
            {
                return _functionPointerNamesByClangType[clangFunctionPointerType];
            }

            var clangPointeeType = clangType.PointeeType;
            var pointeeTypeName = MapTypeName(clangPointeeType, cursorName);
            string result = pointeeTypeName + "*";

            return result;
        }

        private static bool TryClangGetFunctionPointerType(CXType clangType, out CXType typeCanonical)
        {
            var underlyingType = clangType;
            if (clangType.kind == CXTypeKind.CXType_Pointer)
            {
                underlyingType = clangType.CanonicalType.PointeeType;
            }

            if (underlyingType.kind == CXTypeKind.CXType_FunctionProto)
            {
                typeCanonical = underlyingType.CanonicalType;
                return true;
            }

            if (underlyingType.kind != CXTypeKind.CXType_Typedef)
            {
                typeCanonical = default;
                return false;
            }

            var clangPointeeTypeCanonical = underlyingType.CanonicalType;

            if (clangPointeeTypeCanonical.kind != CXTypeKind.CXType_Pointer ||
                clangPointeeTypeCanonical.PointeeType.kind != CXTypeKind.CXType_FunctionProto)
            {
                typeCanonical = default;
                return false;
            }

            typeCanonical = clangPointeeTypeCanonical;
            return true;
        }

        private string MapTypeNameTypedef(CXType clangType, string cursorName)
        {
            string result;

            var isInSystem = clangType.Declaration.IsSystemCursor();

            if (isInSystem)
            {
                if (clangType.Spelling.CString == "FILE")
                {
                    result = "void";
                }
                else
                {
                    result = MapTypeNameTypedefSystem(clangType, cursorName);
                }
            }
            else
            {
                result = MapTypeNameTypedefNonSystem(clangType);
            }

            return result;
        }

        private string MapTypeNameTypedefSystem(CXType clangType, string parentName)
        {
            var clangUnderlyingType = clangType.Declaration.TypedefDeclUnderlyingType;

            string result = clangUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Builtin
                ? MapTypeNameBuiltIn(clangUnderlyingType.CanonicalType)
                : MapTypeName(clangType.CanonicalType, parentName);

            return result;
        }

        private string MapTypeNameTypedefNonSystem(CXType clangType)
        {
            var clangTypeCanonical = clangType.CanonicalType;

            if (clangTypeCanonical.TypeClass == CX_TypeClass.CX_TypeClass_Pointer &&
                clangTypeCanonical.PointeeType.kind == CXTypeKind.CXType_FunctionProto)
            {
                return _functionPointerNamesByClangType[clangTypeCanonical.PointeeType];
            }

            var result = clangType.TypedefName.CString;
            return result;
        }

        private string MapTypeNameElaborated(CXType clangType, string cursorName)
        {
            var clangNamedType = clangType.NamedType;
            var result = MapTypeName(clangNamedType, cursorName);

            return result;
        }

        private string MapTypeNameRecord(CXType clangType, string cursorName)
        {
            string result;
            var clangRecord = clangType.Declaration;
            var name = clangType.Spelling.CString;

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (clangRecord.IsAnonymous)
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
            var result = clangType.Spelling.CString;

            if (result.Contains("enum "))
            {
                result = result.Replace("enum ", string.Empty);
            }

            return result;
        }

        private string MapTypeNameConstArray(CXType clangType, string cursorName)
        {
            var elementType = clangType.ArrayElementType;
            var result = MapTypeName(elementType, cursorName);

            return result;
        }

        private string MapTypeNameFunctionProto(CXType clangType)
        {
            var clangTypeCanonical = clangType.CanonicalType;
            return _functionPointerNamesByClangType[clangTypeCanonical];
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
