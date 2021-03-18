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
            var name = clangFunctionExternParameter.Name;
            var type = MapType(clangFunctionExternParameter.Type);
            var isReadOnly = clangFunctionExternParameter.IsReadOnly;

            var result = new CSharpFunctionExternParameter(
                name,
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
            ImmutableArray<ClangOpaqueDataType> opaqueDataTypes,
            ImmutableArray<ClangSystemDataType> systemDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(
                records.Length + opaqueDataTypes.Length + systemDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecord in records)
            {
                var @struct = MapRecord(clangRecord);
                builder.Add(@struct);
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangOpaqueDataType in opaqueDataTypes)
            {
                var @struct = MapOpaqueDataType(clangOpaqueDataType);
                builder.Add(@struct);
            }

            foreach (var clangSystemDataType in systemDataTypes)
            {
                var @struct = MapSystemDataType(clangSystemDataType);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStruct MapRecord(ClangRecord clangRecord)
        {
            var name = clangRecord.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangRecord.CodeLocation);
            var type = MapType(clangRecord.Type);
            var fields = MapRecordFields(clangRecord.Fields);

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields);

            return result;
        }

        private ImmutableArray<CSharpStructField> MapRecordFields(ImmutableArray<ClangRecordField> clangRecordFields)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStructField>(clangRecordFields.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecordField in clangRecordFields)
            {
                var structField = MapRecordField(clangRecordField);
                builder.Add(structField);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStructField MapRecordField(ClangRecordField clangRecordField)
        {
            var name = clangRecordField.Name;
            var type = MapType(clangRecordField.Type);
            var offset = clangRecordField.Offset;
            var padding = clangRecordField.Padding;

            var result = new CSharpStructField(
                name,
                type,
                offset,
                padding);

            return result;
        }

        private CSharpStruct MapOpaqueDataType(ClangOpaqueDataType clangOpaqueDataType)
        {
            var name = clangOpaqueDataType.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangOpaqueDataType.CodeLocation);
            var type = MapType(clangOpaqueDataType.PointerType);
            var fields = ImmutableArray<CSharpStructField>.Empty;

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields);

            return result;
        }

        private CSharpStruct MapSystemDataType(ClangSystemDataType clangSystemDataType)
        {
            var name = clangSystemDataType.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangSystemDataType.CodeLocation);
            var type = MapType(clangSystemDataType.UnderlyingType);
            var fields = ImmutableArray<CSharpStructField>.Empty;

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields);

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
            var value = clangEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                value);

            return result;
        }

        private CSharpType MapType(ClangType clangType)
        {
            var name = clangType.Name;
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

        private string MapOriginalCodeLocationComment(ClangCodeLocation codeLocation)
        {
            var kind = codeLocation.Kind;
            var fileName = codeLocation.FileName;
            var fileLineNumber = codeLocation.FileLineNumber;
            var dateTime = codeLocation.DateTime;

            var result = $"// {kind} @ {fileName}:{fileLineNumber} {dateTime}";

            return result;
        }
    }
}
