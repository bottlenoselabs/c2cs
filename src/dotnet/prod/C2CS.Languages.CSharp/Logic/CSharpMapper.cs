// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Immutable;
using C2CS.Languages.C;

namespace C2CS.CSharp
{
    public class CSharpMapper
    {
        public ImmutableArray<CSharpFunctionExtern> MapFunctionExterns(ImmutableArray<ClangFunctionExtern> clangFunctionExterns)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionExtern>(clangFunctionExterns.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExtern in clangFunctionExterns)
            {
                var functionExtern = MapFunctionExtern(clangFunctionExtern);
                builder.Add(functionExtern);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionExtern MapFunctionExtern(ClangFunctionExtern clangFunctionExtern)
        {
            var name = clangFunctionExtern.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionExtern.CodeLocation);
            var returnType = MapType(clangFunctionExtern.ReturnType);
            var callingConvention = MapFunctionCallingConvention(clangFunctionExtern.CallingConvention);
            var parameters = MapFunctionExternParameters(clangFunctionExtern.Parameters);

            var result = new CSharpFunctionExtern(
                name,
                originalCodeLocationComment,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private CSharpFunctionExternCallingConvention MapFunctionCallingConvention(
            ClangFunctionExternCallingConvention clangFunctionCallingConvention)
        {
            var result = clangFunctionCallingConvention switch
            {
                ClangFunctionExternCallingConvention.C => CSharpFunctionExternCallingConvention.C,
                ClangFunctionExternCallingConvention.Unknown => CSharpFunctionExternCallingConvention.Unknown,
                _ => throw new ArgumentOutOfRangeException(nameof(clangFunctionCallingConvention), clangFunctionCallingConvention, null)
            };

            return result;
        }

        private ImmutableArray<CSharpFunctionExternParameter> MapFunctionExternParameters(
            ImmutableArray<ClangFunctionExternParameter> clangFunctionExternParameters)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionExternParameter>(clangFunctionExternParameters.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExternParameter in clangFunctionExternParameters)
            {
                var functionExternParameter = MapFunctionExternParameter(clangFunctionExternParameter);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionExternParameter MapFunctionExternParameter(
            ClangFunctionExternParameter clangFunctionExternParameter)
        {
            var name = SanitizeIdentifierName(clangFunctionExternParameter.Name);
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionExternParameter.CodeLocation);
            var type = MapType(clangFunctionExternParameter.Type);
            var isReadOnly = clangFunctionExternParameter.IsReadOnly;

            var result = new CSharpFunctionExternParameter(
                name,
                originalCodeLocationComment,
                type,
                isReadOnly);

            return result;
        }

        public ImmutableArray<CSharpFunctionPointer> MapFunctionPointers(ImmutableArray<ClangFunctionPointer> clangFunctionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(clangFunctionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointer in clangFunctionPointers)
            {
                var functionPointer = MapFunctionPointer(clangFunctionPointer);
                builder.Add(functionPointer);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionPointer MapFunctionPointer(ClangFunctionPointer clangFunctionPointer)
        {
            var name = clangFunctionPointer.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionPointer.CodeLocation);
            var type = MapType(clangFunctionPointer.Type);

            var result = new CSharpFunctionPointer(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        public ImmutableArray<CSharpStruct> MapStructs(
            ImmutableArray<ClangRecord> records,
            ImmutableArray<ClangAliasType> aliasDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(
                records.Length + aliasDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecord in records)
            {
                var @struct = MapStruct(clangRecord);
                builder.Add(@struct);
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangAliasDataType in aliasDataTypes)
            {
                var @struct = MapAliasDataType(clangAliasDataType);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStruct MapStruct(ClangRecord clangRecord)
        {
            var name = clangRecord.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangRecord.CodeLocation);
            var type = MapType(clangRecord.Type);
            var fields = MapStructFields(clangRecord.Fields);
            var nestedStructs = MapNestedStructs(clangRecord.NestedRecords);

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields,
                nestedStructs);

            return result;
        }

        private ImmutableArray<CSharpStructField> MapStructFields(ImmutableArray<ClangRecordField> clangRecordFields)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStructField>(clangRecordFields.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecordField in clangRecordFields)
            {
                var structField = MapStructField(clangRecordField);
                builder.Add(structField);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStructField MapStructField(ClangRecordField clangRecordField)
        {
            var name = SanitizeIdentifierName(clangRecordField.Name);
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangRecordField.CodeLocation);
            var type = MapType(clangRecordField.Type);
            var offset = clangRecordField.Offset;
            var padding = clangRecordField.Padding;

            var result = new CSharpStructField(
                name,
                originalCodeLocationComment,
                type,
                offset,
                padding);

            return result;
        }

        private ImmutableArray<CSharpStruct> MapNestedStructs(ImmutableArray<ClangRecord> clangNestedRecords)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(clangNestedRecords.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecordNestedRecord in clangNestedRecords)
            {
                var nestedRecord = MapStruct(clangRecordNestedRecord);
                builder.Add(nestedRecord);
            }

            var result = builder.ToImmutable();
            return result;
        }

        public ImmutableArray<CSharpOpaqueDataType> MapOpaqueDataTypes(ImmutableArray<ClangOpaqueDataType> opaqueDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpOpaqueDataType>(opaqueDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangOpaqueDataType in opaqueDataTypes)
            {
                var opaqueDataType = MapOpaqueDataType(clangOpaqueDataType);
                builder.Add(opaqueDataType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpOpaqueDataType MapOpaqueDataType(ClangOpaqueDataType clangOpaqueDataType)
        {
            var name = clangOpaqueDataType.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangOpaqueDataType.CodeLocation);

            var result = new CSharpOpaqueDataType(
                name,
                originalCodeLocationComment);

            return result;
        }

        private CSharpStruct MapAliasDataType(ClangAliasType clangAliasType)
        {
            var name = clangAliasType.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangAliasType.CodeLocation);
            var type = MapType(clangAliasType.UnderlyingType);
            var fields = MapAliasDataTypeFields(clangAliasType.UnderlyingType, originalCodeLocationComment);

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields);

            return result;
        }

        private ImmutableArray<CSharpStructField> MapAliasDataTypeFields(ClangType clangType, string originalCodeLocationComment)
        {
            var type = MapType(clangType);
            var structField = new CSharpStructField(
                "Data",
                originalCodeLocationComment,
                type,
                0,
                0);

            var result = ImmutableArray.Create(structField);
            return result;
        }

        public ImmutableArray<CSharpEnum> MapEnums(ImmutableArray<ClangEnum> clangEnums)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnum>(clangEnums.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnum in clangEnums)
            {
                var @enum = MapEnum(clangEnum);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpEnum MapEnum(ClangEnum clangEnum)
        {
            var name = clangEnum.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangEnum.CodeLocation);
            var type = MapType(clangEnum.IntegerType);
            var values = MapEnumValues(clangEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                type,
                values);
            return result;
        }

        private ImmutableArray<CSharpEnumValue> MapEnumValues(ImmutableArray<ClangEnumValue> clangEnumValues)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(clangEnumValues.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnumValue in clangEnumValues)
            {
                var @enum = MapEnumValue(clangEnumValue);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpEnumValue MapEnumValue(ClangEnumValue clangEnumValue)
        {
            var name = clangEnumValue.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangEnumValue.CodeLocation);
            var value = clangEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                originalCodeLocationComment,
                value);

            return result;
        }

        private CSharpType MapType(ClangType clangType)
        {
            var name = MapTypeName(clangType);
            var originalName = clangType.OriginalName;
            var sizeOf = clangType.SizeOf;
            var alignOf = clangType.AlignOf;

            var result = new CSharpType(
                name,
                originalName,
                0,
                sizeOf,
                alignOf);

            return result;
        }

        private static string MapTypeName(ClangType clangType)
        {
            string result = clangType.Name;

            if (clangType.IsSystemType && clangType.Name == "bool")
            {
                result = "CBool";
            }

            return result;
        }

        private string MapOriginalCodeLocationComment(ClangCodeLocation codeLocation)
        {
            var kind = codeLocation.Kind;
            var fileName = codeLocation.FileName;
            var fileLineNumber = codeLocation.FileLineNumber;
            var dateTime = codeLocation.DateTime;

            var result = $"// {kind} @ {fileName}:{fileLineNumber} {dateTime}";

            return result;
        }

        private static string SanitizeIdentifierName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "param";
            }

            var result = name;

            switch (name)
            {
                case "abstract":
                case "as":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "false":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "internal":
                case "is":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "null":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "record":
                case "ref":
                case "return":
                case "sbyte":
                case "sealed":
                case "short":
                case "sizeof":
                case "stackalloc":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "true":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unchecked":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "void":
                case "volatile":
                case "while":
                    result = $"@{name}";
                    break;
            }

            return result;
        }
    }
}
