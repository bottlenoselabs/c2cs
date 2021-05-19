// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using C2CS.Languages.C;

namespace C2CS.CSharp
{
    public static class CSharpMapper
    {
        public static CSharpAbstractSyntaxTree GetAbstractSyntaxTree(
            ClangAbstractSyntaxTree clangAbstractSyntaxTree)
        {
            var functionExterns = MapFunctionExterns(
                clangAbstractSyntaxTree.FunctionExterns);
            var functionPointers = MapFunctionPointers(
                clangAbstractSyntaxTree.FunctionPointers);
            var structs = MapStructs(
                clangAbstractSyntaxTree.Records,
                clangAbstractSyntaxTree.Typedefs);
            var typedefs = MapTypedefs(clangAbstractSyntaxTree.Typedefs);
            var opaqueDataTypes = MapOpaqueDataTypes(
                clangAbstractSyntaxTree.OpaqueTypes);
            var enums = MapEnums(clangAbstractSyntaxTree.Enums);
            var variables = MapVariablesExtern(clangAbstractSyntaxTree.Variables);

            var result = new CSharpAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                structs,
                typedefs,
                opaqueDataTypes,
                enums,
                variables);

            return result;
        }

        private static ImmutableArray<CSharpTypedef> MapTypedefs(ImmutableArray<ClangTypedef> typedefs)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpTypedef>(typedefs.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangTypedef in typedefs)
            {
                var typedef = MapTypedef(clangTypedef);
                builder.Add(typedef);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static ImmutableArray<CSharpFunctionExtern> MapFunctionExterns(ImmutableArray<ClangFunctionExtern> clangFunctionExterns)
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

        private static CSharpFunctionExtern MapFunctionExtern(ClangFunctionExtern clangFunctionExtern)
        {
            var name = clangFunctionExtern.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionExtern);
            var returnType = MapType(clangFunctionExtern.ReturnType);
            var callingConvention = MapFunctionCallingConvention(clangFunctionExtern.CallingConvention);
            var parameters = MapFunctionExternParameters(clangFunctionExtern.Parameters);

            var isWrapped = false;
            foreach (var parameter in parameters)
            {
                if (parameter.IsFunctionPointer)
                {
                    isWrapped = true;
                }
            }

            var result = new CSharpFunctionExtern(
                name,
                originalCodeLocationComment,
                callingConvention,
                returnType,
                parameters,
                isWrapped);

            return result;
        }

        private static CSharpFunctionExternCallingConvention MapFunctionCallingConvention(
            ClangFunctionExternCallingConvention clangFunctionCallingConvention)
        {
            var result = clangFunctionCallingConvention switch
            {
                ClangFunctionExternCallingConvention.C => CSharpFunctionExternCallingConvention.Cdecl,
                ClangFunctionExternCallingConvention.Unknown => CSharpFunctionExternCallingConvention.WinApi,
                _ => throw new ArgumentOutOfRangeException(nameof(clangFunctionCallingConvention), clangFunctionCallingConvention, null)
            };

            return result;
        }

        private static ImmutableArray<CSharpFunctionExternParameter> MapFunctionExternParameters(
            ImmutableArray<ClangFunctionExternParameter> clangFunctionExternParameters)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionExternParameter>(clangFunctionExternParameters.Length);
            var parameterNames = new List<string>();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExternParameter in clangFunctionExternParameters)
            {
                var parameterName = MapUniqueParameterName(clangFunctionExternParameter.Name, parameterNames);
                parameterNames.Add(parameterName);
                var functionExternParameter = MapFunctionExternParameter(clangFunctionExternParameter, parameterName);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static string MapUniqueParameterName(string parameterName, List<string> parameterNames)
        {
            if (string.IsNullOrEmpty(parameterName))
            {
                parameterName = "param";
            }

            while (parameterNames.Contains(parameterName))
            {
                var numberSuffixMatch = Regex.Match(parameterName, "\\d$");
                if (numberSuffixMatch.Success)
                {
                    var parameterNameWithoutSuffix = parameterName.Substring(0, numberSuffixMatch.Index);
                    parameterName = ParameterNameUniqueSuffix(parameterNameWithoutSuffix, numberSuffixMatch.Value);
                }
                else
                {
                    parameterName = ParameterNameUniqueSuffix(parameterName, string.Empty);
                }
            }

            return parameterName;

            static string ParameterNameUniqueSuffix(string parameterNameWithoutSuffix, string parameterSuffix)
            {
                if (parameterSuffix == string.Empty)
                {
                    return parameterNameWithoutSuffix + "2";
                }

                var parameterSuffixNumber = int.Parse(parameterSuffix, NumberStyles.Integer, CultureInfo.InvariantCulture);
                parameterSuffixNumber += 1;
                var parameterName = parameterNameWithoutSuffix + parameterSuffixNumber;
                return parameterName;
            }
        }

        private static CSharpFunctionExternParameter MapFunctionExternParameter(
            ClangFunctionExternParameter clangFunctionExternParameter, string parameterName)
        {
            var name = SanitizeIdentifierName(parameterName);
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionExternParameter);
            var type = MapType(clangFunctionExternParameter.Type);
            var isFunctionPointer = clangFunctionExternParameter.IsFunctionPointer;

            var result = new CSharpFunctionExternParameter(
                name,
                originalCodeLocationComment,
                type,
                isFunctionPointer);

            return result;
        }

        private static ImmutableArray<CSharpFunctionPointer> MapFunctionPointers(
            ImmutableArray<ClangFunctionPointer> clangFunctionPointers)
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

        private static CSharpFunctionPointer MapFunctionPointer(ClangFunctionPointer clangFunctionPointer)
        {
            var name = clangFunctionPointer.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionPointer);
            var pointerSize = clangFunctionPointer.PointerSize;
            var returnType = MapType(clangFunctionPointer.ReturnType);
            var parameters = MapFunctionPointerParameters(clangFunctionPointer.Parameters);

            var result = new CSharpFunctionPointer(
                name,
                originalCodeLocationComment,
                pointerSize,
                returnType,
                parameters);

            return result;
        }

        private static ImmutableArray<CSharpFunctionPointerParameter> MapFunctionPointerParameters(
            ImmutableArray<ClangFunctionPointerParameter> clangFunctionPointerParameters)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointerParameter>(clangFunctionPointerParameters.Length);
            var parameterNames = new List<string>();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointerParameter in clangFunctionPointerParameters)
            {
                var parameterName = MapUniqueParameterName(clangFunctionPointerParameter.Name, parameterNames);
                parameterNames.Add(parameterName);
                var functionExternParameter = MapFunctionPointerParameter(clangFunctionPointerParameter, parameterName);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpFunctionPointerParameter MapFunctionPointerParameter(
            ClangFunctionPointerParameter clangFunctionPointerParameter, string parameterName)
        {
            var name = SanitizeIdentifierName(parameterName);
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangFunctionPointerParameter);
            var type = MapType(clangFunctionPointerParameter.Type);

            var result = new CSharpFunctionPointerParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpStruct> MapStructs(
            ImmutableArray<ClangRecord> records,
            ImmutableArray<ClangTypedef> typedefs)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(
                records.Length + typedefs.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecord in records)
            {
                var @struct = MapStruct(clangRecord);
                builder.Add(@struct);
            }

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            // foreach (var clangOpaquePointer in opaquePointers)
            // {
            //     var @struct = MapOpaquePointer(clangOpaquePointer);
            //     builder.Add(@struct);
            // }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpStruct MapStruct(ClangRecord clangRecord)
        {
            var name = clangRecord.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangRecord);
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

        private static ImmutableArray<CSharpStructField> MapStructFields(ImmutableArray<ClangRecordField> clangRecordFields)
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

        private static CSharpStructField MapStructField(ClangRecordField clangRecordField)
        {
            var name = SanitizeIdentifierName(clangRecordField.Name);
            var originalName = clangRecordField.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangRecordField);
            var type = MapType(clangRecordField.Type);
            var offset = clangRecordField.Offset;
            var padding = clangRecordField.Padding;
            var isWrapped = type.IsArray && !IsValidFixedBufferType(type.Name);

            var result = new CSharpStructField(
                name,
                originalName,
                originalCodeLocationComment,
                type,
                offset,
                padding,
                isWrapped);

            return result;
        }

        private static ImmutableArray<CSharpStruct> MapNestedStructs(ImmutableArray<ClangRecord> clangNestedRecords)
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

        private static ImmutableArray<CSharpOpaqueType> MapOpaqueDataTypes(ImmutableArray<ClangOpaqueType> opaqueDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(opaqueDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangOpaqueDataType in opaqueDataTypes)
            {
                var opaqueDataType = MapOpaqueDataType(clangOpaqueDataType);
                builder.Add(opaqueDataType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpOpaqueType MapOpaqueDataType(ClangOpaqueType clangOpaqueType)
        {
            var name = clangOpaqueType.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangOpaqueType);

            var result = new CSharpOpaqueType(
                name,
                originalCodeLocationComment);

            return result;
        }

        private static CSharpStruct MapOpaquePointer(ClangOpaquePointer clangOpaquePointer)
        {
            var name = clangOpaquePointer.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangOpaquePointer);
            var type = MapType(clangOpaquePointer.PointerType);
            var fields = MapOpaquePointerFields(clangOpaquePointer.PointerType, originalCodeLocationComment);

            var result = new CSharpStruct(
                name,
                originalCodeLocationComment,
                type,
                fields);

            return result;
        }

        private static ImmutableArray<CSharpStructField> MapOpaquePointerFields(ClangType clangType, string originalCodeLocationComment)
        {
            var type = MapType(clangType);
            var structField = new CSharpStructField(
                "Pointer",
                string.Empty,
                originalCodeLocationComment,
                type,
                0,
                0,
                false);

            var result = ImmutableArray.Create(structField);
            return result;
        }

        private static CSharpTypedef MapTypedef(ClangTypedef clangTypedef)
        {
            var name = clangTypedef.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangTypedef);
            var type = MapType(clangTypedef.UnderlyingType);

            var result = new CSharpTypedef(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        public static ImmutableArray<CSharpEnum> MapEnums(ImmutableArray<ClangEnum> clangEnums)
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

        private static CSharpEnum MapEnum(ClangEnum clangEnum)
        {
            var name = clangEnum.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangEnum);
            var type = MapType(clangEnum.IntegerType);
            var values = MapEnumValues(clangEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                type,
                values);
            return result;
        }

        private static ImmutableArray<CSharpEnumValue> MapEnumValues(ImmutableArray<ClangEnumValue> clangEnumValues)
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

        private static CSharpEnumValue MapEnumValue(ClangEnumValue clangEnumValue)
        {
            var name = clangEnumValue.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangEnumValue);
            var value = clangEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                originalCodeLocationComment,
                value);

            return result;
        }

        private static ImmutableArray<CSharpVariable> MapVariablesExtern(ImmutableArray<ClangVariable> clangVariables)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpVariable>(clangVariables.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangVariable in clangVariables)
            {
                var variable = MapVariable(clangVariable);
                builder.Add(variable);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpVariable MapVariable(ClangVariable clangVariable)
        {
            var name = clangVariable.Name;
            var originalCodeLocationComment = MapOriginalCodeLocationComment(clangVariable);
            var type = MapType(clangVariable.Type);

            var result = new CSharpVariable(name, originalCodeLocationComment, type);
            return result;
        }

        private static CSharpType MapType(ClangType clangType)
        {
            var name = MapTypeName(clangType);
            var originalName = clangType.OriginalName;
            var sizeOf = clangType.SizeOf;
            var alignOf = clangType.AlignOf;
            var fixedBufferSize = clangType.ArraySize;

            var result = new CSharpType(
                name,
                originalName,
                sizeOf,
                alignOf,
                fixedBufferSize);

            return result;
        }

        private static string MapTypeName(ClangType clangType)
        {
            string result = clangType.Name;

            if (clangType.IsSystemType && clangType.Name == "bool")
            {
                result = "_Bool";
            }

            return result;
        }

        private static string MapOriginalCodeLocationComment(ClangNode node)
        {
            var kind = node.Kind;
            var codeLocation = node.CodeLocation;

            string result;
            if (codeLocation.IsSystem)
            {
                result = $"// {kind} @ System";
            }
            else
            {
                var fileName = codeLocation.FileName;
                var fileLineNumber = codeLocation.FileLineNumber;

                result = $"// {kind} @ {fileName}:{fileLineNumber}";
            }

            return result;
        }

        private static string SanitizeIdentifierName(string name)
        {
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

        private static bool IsValidFixedBufferType(string typeString)
        {
            return typeString switch
            {
                "bool" => true,
                "byte" => true,
                "char" => true,
                "short" => true,
                "int" => true,
                "long" => true,
                "sbyte" => true,
                "ushort" => true,
                "uint" => true,
                "ulong" => true,
                "float" => true,
                "double" => true,
                _ => false
            };
        }
    }
}
