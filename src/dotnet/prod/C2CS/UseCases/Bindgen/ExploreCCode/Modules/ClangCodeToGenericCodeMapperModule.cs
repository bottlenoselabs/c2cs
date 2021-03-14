// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public class ClangCodeToGenericCodeMapperModule
    {
        private readonly ImmutableDictionary<CXCursor, string> _namesByCursor;
        private readonly ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> _functionParametersByFunction;
        private readonly ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> _recordFieldsByRecord;
        private readonly ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> _enumValuesByEnum;
        private readonly Dictionary<CXType, GenericCodeLayout> _layoutsByClangType = new();
        private readonly Dictionary<CXType, string> _functionPointerNamesByClangType = new();

        public ClangCodeToGenericCodeMapperModule(
            ImmutableDictionary<CXCursor, string> namesByCursor,
            ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> functionParametersByFunction,
            ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> recordFieldsByRecord,
            ImmutableDictionary<CXCursor, ImmutableArray<CXCursor>> enumValuesByEnum)
        {
            _namesByCursor = namesByCursor;
            _functionParametersByFunction = functionParametersByFunction;
            _recordFieldsByRecord = recordFieldsByRecord;
            _enumValuesByEnum = enumValuesByEnum;
        }

        public GenericCodeAbstractSyntaxTree MapSyntaxTree(
            ImmutableArray<CXCursor> clangFunctions,
            ImmutableArray<CXCursor> clangRecords,
            ImmutableArray<CXCursor> clangEnums,
            ImmutableArray<CXCursor> clangOpaqueTypes,
            ImmutableArray<CXCursor> clangForwardTypes,
            ImmutableArray<ClangFunctionPointer> clangFunctionPointers,
            ImmutableArray<CXCursor> clangSystemTypes,
            Dictionary<CXCursor, string> clangNamesByCursor)
        {
            var namesByCursor = clangNamesByCursor.ToImmutableDictionary();
            var functionParametersByFunction = _functionParametersByFunction.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
            var recordFieldsByRecord = _recordFieldsByRecord.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
            var enumValuesByEnum =_enumValuesByEnum.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);

            var functionPointers = MapFunctionPointers(clangFunctionPointers);
            var functions = MapFunctionExterns(clangFunctions);
            var structs = MapStructs(clangRecords);
            var enums = MapEnums(clangEnums);
            var opaqueTypes = MapOpaqueTypes(clangOpaqueTypes);
            var forwardTypes = MapForwardTypes(clangForwardTypes);
            var systemTypes = clangSystemTypes;

            var result = new GenericCodeAbstractSyntaxTree(
                functions,
                structs,
                enums,
                opaqueTypes,
                forwardTypes,
                functionPointers,
                systemTypes,
                namesByCursor);
            return result;
        }

        private ImmutableArray<GenericCodeEnum> MapEnums(ImmutableArray<CXCursor> clangEnums)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeEnum>(clangEnums.Length);

            foreach (var clangEnum in clangEnums)
            {
                var @enum = MapEnum(clangEnum);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeEnum MapEnum(CXCursor clangEnum)
        {
            var clangUnderlyingEnum = clangEnum;
            if (clangEnum.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                clangUnderlyingEnum = clangEnum.TypedefDeclUnderlyingType.Declaration;
            }

            var name = _namesByCursor[clangEnum];
            var info = MapInfo(clangEnum, GenericCodeKind.Enum);
            var type = MapType(clangUnderlyingEnum.EnumDecl_IntegerType);
            var enumValues = MapEnumValues(clangUnderlyingEnum);

            var result = new GenericCodeEnum(
                name,
                info,
                type,
                enumValues);

            return result;
        }

        private ImmutableArray<GenericCodeValue> MapEnumValues(CXCursor clangEnum)
        {
            var clangEnumValues = _enumValuesByEnum[clangEnum];
            var builder = ImmutableArray.CreateBuilder<GenericCodeValue>(clangEnumValues.Length);

            foreach (var clangEnumValue in clangEnumValues)
            {
                var cEnumValue = MapEnumValue(clangEnumValue);
                builder.Add(cEnumValue);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeValue MapEnumValue(CXCursor clangEnumValue)
        {
            var name = clangEnumValue.Spelling.CString;
            var value = clangEnumValue.EnumConstantDeclValue;

            var result = new GenericCodeValue(
                name,
                value);

            return result;
        }

        private ImmutableArray<GenericCodeOpaqueType> MapOpaqueTypes(ImmutableArray<CXCursor> clangOpaqueTypes)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeOpaqueType>(clangOpaqueTypes.Length);

            foreach (var clangOpaqueType in clangOpaqueTypes)
            {
                var opaqueType = MapOpaqueType(clangOpaqueType);
                builder.Add(opaqueType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeOpaqueType MapOpaqueType(CXCursor clangOpaqueType)
        {
            var name = MapTypeName(clangOpaqueType.Type);
            var info = MapInfo(clangOpaqueType, GenericCodeKind.Opaque);
            var type = MapType(clangOpaqueType.Type);

            var result = new GenericCodeOpaqueType(
                name,
                info,
                type);

            return result;
        }

        public ImmutableArray<GenericCodeForwardType> MapForwardTypes(ImmutableArray<CXCursor> clangForwardTypes)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeForwardType>(clangForwardTypes.Length);

            foreach (var clangForwardType in clangForwardTypes)
            {
                var forwardType = MapForwardType(clangForwardType);
                builder.Add(forwardType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeForwardType MapForwardType(CXCursor clangForwardType)
        {
            var name = MapTypeName(clangForwardType.Type);
            var info = MapInfo(clangForwardType, GenericCodeKind.Forward);
            var type = MapType(clangForwardType.Type);
            var underlyingType = MapType(clangForwardType.Type.CanonicalType);

            var result = new GenericCodeForwardType(
                name,
                info,
                type,
                underlyingType);
            return result;
        }

        private ImmutableArray<GenericCodeFunctionPointer> MapFunctionPointers(
            ImmutableArray<ClangFunctionPointer> clangFunctionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeFunctionPointer>(clangFunctionPointers.Length);

            foreach (var clangFunctionPointer in clangFunctionPointers)
            {
                var functionPointer = MapFunctionPointer(clangFunctionPointer);
                builder.Add(functionPointer);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeFunctionPointer MapFunctionPointer(ClangFunctionPointer clangFunctionPointer)
        {
            var name = MapFunctionPointerName(
                clangFunctionPointer.Name,
                clangFunctionPointer.Type,
                clangFunctionPointer.Cursor,
                clangFunctionPointer.Parent);
            var info = MapInfo(clangFunctionPointer.Cursor, GenericCodeKind.FunctionPointer);
            var type = MapType(clangFunctionPointer.Cursor.Type);

            var result = new GenericCodeFunctionPointer(
                name,
                info,
                type);

            return result;
        }

        private string MapFunctionPointerName(string? name, CXType type, CXCursor cursor, CXCursor parent)
        {
            var typeCanonical = type.CanonicalType;
            if (_functionPointerNamesByClangType.TryGetValue(typeCanonical, out var result))
            {
                return result;
            }

            if (string.IsNullOrEmpty(name))
            {
                var cursorName = MapCursorName(cursor);
                var parentName = MapCursorName(parent);
                if (string.IsNullOrEmpty(cursorName) && string.IsNullOrEmpty(parentName))
                {
                    var guidString = Guid.NewGuid().ToString("N");
                    result = $"FunctionPointer_{guidString}";
                }
                else
                {
                    result = $"FunctionPointer_{parentName}_{cursorName}";
                }
            }
            else
            {
                result = $"FunctionPointer_{name}";
            }

            _functionPointerNamesByClangType.Add(typeCanonical, result);
            return result;
        }

        private ImmutableArray<GenericCodeFunctionExtern> MapFunctionExterns(ImmutableArray<CXCursor> clangFunctions)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeFunctionExtern>(clangFunctions.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunction in clangFunctions)
            {
                var function = MapFunctionExtern(clangFunction);
                builder.Add(function);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeFunctionExtern MapFunctionExtern(CXCursor clangFunction)
        {
            var name = MapName(clangFunction);
            var info = MapInfo(clangFunction, GenericCodeKind.FunctionExtern);
            var returnType = MapType(clangFunction.ResultType);
            var callingConvention = MapFunctionCallingConvention(clangFunction);
            var parameters = MapFunctionParameters(clangFunction);

            var result = new GenericCodeFunctionExtern(
                name,
                info,
                returnType,
                callingConvention,
                parameters);

            return result;
        }

        private GenericCodeFunctionCallingConvention MapFunctionCallingConvention(CXCursor clangFunction)
        {
            var clangCallingConvention = clangFunction.Type.FunctionTypeCallingConv;

            var callingConvention = clangCallingConvention switch
            {
                CXCallingConv.CXCallingConv_C => GenericCodeFunctionCallingConvention.C,
                _ => throw new NotImplementedException()
            };

            return callingConvention;
        }

        private ImmutableArray<GenericCodeFunctionParameter> MapFunctionParameters(CXCursor clangFunction)
        {
            var clangFunctionParameters = _functionParametersByFunction[clangFunction];

            var builder = ImmutableArray.CreateBuilder<GenericCodeFunctionParameter>(clangFunctionParameters.Length);
            for (var i = 0; i < clangFunctionParameters.Length; i++)
            {
                var clangFunctionParameter = clangFunctionParameters[i];
                var functionParameter = MapFunctionParameter(clangFunctionParameter, i);
                builder.Add(functionParameter);
            }

            var result = builder.ToImmutable();

            return result;
        }

        private GenericCodeFunctionParameter MapFunctionParameter(CXCursor clangFunctionParameter, int index)
        {
            var functionParameterName = MapName(clangFunctionParameter);
            if (string.IsNullOrEmpty(functionParameterName))
            {
                if (index == 0)
                {
                    functionParameterName = "param";
                }
                else
                {
                    functionParameterName = "param" + (index + 1);
                }
            }

            var functionParameterType = MapType(clangFunctionParameter.Type);
            var functionIsReadOnly = clangFunctionParameter.Type.IsConstQualified;

            var result = new GenericCodeFunctionParameter(
                functionParameterName,
                functionParameterType,
                functionIsReadOnly);

            return result;
        }

        private ImmutableArray<GenericCodeStruct> MapStructs(ImmutableArray<CXCursor> clangRecords)
        {
            var builder = ImmutableArray.CreateBuilder<GenericCodeStruct>(clangRecords.Length);

            foreach (var clangRecord in clangRecords)
            {
                var @struct = MapStruct(clangRecord);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private GenericCodeStruct MapStruct(CXCursor clangRecord)
        {
            var clangUnderlyingRecord = clangRecord;
            if (clangRecord.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                clangUnderlyingRecord = clangRecord.TypedefDeclUnderlyingType.Declaration;
            }

            var name = MapName(clangRecord);
            var info = MapInfo(clangUnderlyingRecord, GenericCodeKind.Struct);
            var type = MapType(clangUnderlyingRecord.Type);

            var fields = MapStructFields(clangUnderlyingRecord, type.Layout.Alignment);

            var result = new GenericCodeStruct(
                name,
                info,
                type,
                fields);

            return result;
        }

        private ImmutableArray<GenericCodeStructField> MapStructFields(CXCursor clangRecord, int alignment)
        {
            var clangRecordFields = _recordFieldsByRecord[clangRecord];

            var builder = ImmutableArray.CreateBuilder<GenericCodeStructField>();
            for (var i = 0; i < clangRecordFields.Length; i++)
            {
                var clangRecordField = clangRecordFields[i];
                var field = MapStructField(clangRecordField);

                if (i > 0)
                {
                    var cFieldPrevious = builder[i - 1];
                    var expectedFieldOffset = cFieldPrevious.Offset + cFieldPrevious.Type.Layout.Size;
                    if (field.Offset != expectedFieldOffset)
                    {
                        var padding = field.Offset - expectedFieldOffset;
                        builder[i - 1] = new GenericCodeStructField(cFieldPrevious, padding);
                    }
                }

                builder.Add(field);
            }

            var result = builder.ToImmutable();

            return result;
        }

        private GenericCodeStructField MapStructField(CXCursor clangRecordField)
        {
            var name = MapName(clangRecordField);
            var type = MapType(clangRecordField.Type);
            var offset = (int)(clangRecordField.OffsetOfField / 8);

            var result = new GenericCodeStructField(
                name,
                type,
                offset);

            return result;
        }

        private GenericCodeType MapType(CXType clangType)
        {
            var name = MapTypeName(clangType);
            var originalName = clangType.Spelling.CString;
            var arraySize = MapTypeArraySize(clangType);
            var layout = MapLayout(clangType);

            var result = new GenericCodeType(
                name,
                originalName,
                arraySize,
                layout);

            return result;
        }

        private string MapTypeName(CXType clangType)
        {
            var typeName = clangType.TypeClass switch
            {
                CX_TypeClass.CX_TypeClass_Builtin => MapTypeNameBuiltIn(clangType),
                CX_TypeClass.CX_TypeClass_Pointer => MapTypeNamePointer(clangType),
                CX_TypeClass.CX_TypeClass_Typedef => MapTypeNameTypedef(clangType),
                CX_TypeClass.CX_TypeClass_Elaborated => MapTypeNameElaborated(clangType),
                CX_TypeClass.CX_TypeClass_Record => MapTypeNameRecord(clangType),
                CX_TypeClass.CX_TypeClass_Enum => MapTypeNameEnum(clangType),
                CX_TypeClass.CX_TypeClass_ConstantArray => MapTypeNameConstArray(clangType),
                CX_TypeClass.CX_TypeClass_FunctionProto => MapTypeNameFunctionProto(clangType),
                _ => throw new NotImplementedException()
            };

            if (clangType.IsConstQualified)
            {
                typeName = typeName.Replace("const ", string.Empty).Trim();
            }

            return typeName;
        }

        private string MapTypeNameFunctionProto(CXType clangType)
        {
            var clangTypeCanonical = clangType.CanonicalType;
            return _functionPointerNamesByClangType[clangTypeCanonical];
        }

        private string MapTypeNameRecord(CXType clangType)
        {
            var clangRecord = clangType.Declaration;

            var result = clangRecord.IsAnonymous
                ? MapTypeNameRecordAnonymous(clangRecord)
                : MapTypeNameRecordNonAnonymous(clangRecord);

            return result;
        }

        private string MapTypeNameRecordAnonymous(CXCursor clangRecord)
        {
            var parent = clangRecord.SemanticParent;
            string parentFieldName = MapName(parent);

            var result = clangRecord.kind == CXCursorKind.CXCursor_UnionDecl
                ? $"Anonymous_Union_{parentFieldName}"
                : $"Anonymous_Struct_{parentFieldName}";

            return result;
        }

        private string MapTypeNameRecordNonAnonymous(CXCursor clangRecord)
        {
            var result = _namesByCursor[clangRecord];
            if (result.Contains("struct "))
            {
                result = result.Replace("struct ", string.Empty);
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

        private string MapTypeNameConstArray(CXType clangType)
        {
            var elementType = clangType.ArrayElementType;
            var result = MapTypeName(elementType);

            return result;
        }

        private string MapTypeNameBuiltIn(CXType clangType)
        {
            var result = clangType.kind switch
            {
                CXTypeKind.CXType_Void => "void",
                CXTypeKind.CXType_Bool => "CBool",
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

        private string MapTypeNamePointer(CXType clangType)
        {
            var clangPointeeType = clangType.PointeeType;

            if (TryClangGetFunctionPointerName(clangPointeeType, out string functionPointerName))
            {
                return _functionPointerNamesByClangType[clangPointeeType];
            }

            var pointeeTypeName = MapTypeName(clangPointeeType);
            var result = pointeeTypeName + "*";

            return result;
        }

        private static bool TryClangGetFunctionPointerName(CXType clangType, out string functionPointerName)
        {
            if (clangType.kind != CXTypeKind.CXType_Typedef)
            {
                functionPointerName = string.Empty;
                return false;
            }

            var clangPointeeTypeCanonical = clangType.CanonicalType;

            if (clangPointeeTypeCanonical.TypeClass != CX_TypeClass.CX_TypeClass_Pointer ||
                clangPointeeTypeCanonical.PointeeType.kind != CXTypeKind.CXType_FunctionProto)
            {
                functionPointerName = string.Empty;
                return false;
            }

            functionPointerName = clangType.Spelling.CString;

            return true;
        }

        private string MapTypeNameTypedef(CXType clangType)
        {
            string result;

            var isInSystem = clangType.Declaration.IsInSystem();

            if (isInSystem)
            {
                if (clangType.Spelling.CString == "FILE")
                {
                    result = "void";
                }
                else
                {
                    result = MapTypeNameTypedefSystem(clangType);
                }
            }
            else
            {
                result = MapTypeNameTypedefNonSystem(clangType);
            }

            return result;
        }

        private string MapTypeNameTypedefSystem(CXType clangType)
        {
            var clangUnderlyingType = clangType.Declaration.TypedefDeclUnderlyingType;

            string result = clangUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Builtin
                ? MapTypeNameBuiltIn(clangUnderlyingType.CanonicalType)
                : MapTypeName(clangType.CanonicalType);

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

        private string MapTypeNameElaborated(CXType clangType)
        {
            var clangNamedType = clangType.NamedType;
            var result = MapTypeName(clangNamedType);

            return result;
        }

        private string MapCursorName(CXCursor clangCursor)
        {
            if (clangCursor.IsInSystem())
            {
                var systemUnderlyingType = clangCursor.Type.CanonicalType;
                if (systemUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Builtin)
                {
                    return MapTypeNameBuiltIn(systemUnderlyingType);
                }

                throw new NotImplementedException();
            }

            if (clangCursor.IsAnonymous)
            {
                var fieldName = string.Empty;
                var parent = clangCursor.SemanticParent;
                parent.VisitChildren((child, __) =>
                {
                    if (child.kind == CXCursorKind.CXCursor_FieldDecl && child.Type.Declaration == clangCursor)
                    {
                        fieldName = child.Spelling.CString;
                    }
                });

                return clangCursor.kind == CXCursorKind.CXCursor_UnionDecl
                    ? $"Anonymous_Union_{fieldName}"
                    : $"Anonymous_Struct_{fieldName}";
            }

            var name = clangCursor.Spelling.CString;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var clangType = clangCursor.Type;
            if (clangType.kind == CXTypeKind.CXType_Pointer)
            {
                clangType = clangType.PointeeType;
            }

            name = clangType.Spelling.CString;

            return name;
        }

        private int MapTypeArraySize(CXType clangType)
        {
            if (clangType.TypeClass == CX_TypeClass.CX_TypeClass_ConstantArray)
            {
                return (int)clangType.ArraySize;
            }

            return 0;
        }

        private string MapName(CXCursor clangCursor)
        {
            return _namesByCursor[clangCursor];
        }

        private GenericCodeLayout MapLayout(CXType clangType)
        {
            if (_layoutsByClangType.TryGetValue(clangType, out var layout))
            {
                return layout;
            }

            var clangTypeClass = clangType.TypeClass;
            layout = clangTypeClass switch
            {
                CX_TypeClass.CX_TypeClass_Pointer => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_Builtin => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_Enum => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_Record => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_Typedef => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_Elaborated => MapLayoutForType(clangType),
                CX_TypeClass.CX_TypeClass_ConstantArray => MapLayoutForConstArray(clangType),
                CX_TypeClass.CX_TypeClass_FunctionProto => MapLayoutForType(clangType),
                _ => throw new NotImplementedException()
            };

            _layoutsByClangType.Add(clangType, layout);
            return layout;
        }

        private GenericCodeLayout MapLayoutForType(CXType clangType)
        {
            var size = (int)clangType.SizeOf;
            var alignment = (int)clangType.AlignOf;
            var result = new GenericCodeLayout(
                size,
                alignment);
            return result;
        }

        private GenericCodeLayout MapLayoutForConstArray(CXType type)
        {
            var elementLayout = MapLayout(type.ElementType);
            var size = (int)(elementLayout.Size * type.ArraySize);
            var result = new GenericCodeLayout(
                size,
                elementLayout.Alignment);
            return result;
        }

        private static GenericCodeInfo MapInfo(CXCursor clangCursor, GenericCodeKind kind)
        {
            var location = MapInfoLocation(clangCursor);

            var result = new GenericCodeInfo(
                kind,
                location);
            return result;
        }

        private static GenericCodeLocation MapInfoLocation(CXCursor clangCursor)
        {
            clangCursor.Location.GetFileLocation(
                out var file, out var line, out var column, out var offset);

            if (file.Handle == IntPtr.Zero)
            {
                return default;
            }

            var fileName = Path.GetFileName(file.Name.CString);
            var fileLine = (int)line;
            var fileColumn = (int)column;
            var dateTime = new DateTime(1970, 1, 1).AddSeconds(file.Time);

            var result = new GenericCodeLocation(
                fileName,
                fileLine,
                fileColumn,
                dateTime);

            return result;
        }
    }
}
