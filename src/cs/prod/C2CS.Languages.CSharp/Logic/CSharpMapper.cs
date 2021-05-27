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
        private static readonly Dictionary<string, string> BuiltInPointerFunctionMappings = new()
        {
            {"void (*)(void)", "FnPtrVoid"},
            {"void (*)(void *)", "FnPtrVoidPointer"},
            {"int (*)(void *, void *)", "FnPtrIntPointerPointer"}
        };

        public static CSharpAbstractSyntaxTree GetAbstractSyntaxTree(
            ClangAbstractSyntaxTree clangAbstractSyntaxTree)
        {
            var functionExterns = CSharpFunctions(
                clangAbstractSyntaxTree.FunctionExterns);
            var functionPointers = CSharpPointerFunctions(
                clangAbstractSyntaxTree.FunctionPointers);
            var structs = CSharpStructs(
                clangAbstractSyntaxTree.Records,
                clangAbstractSyntaxTree.Typedefs);
            var typedefs = CSharpTypedefs(clangAbstractSyntaxTree.Typedefs);
            var opaqueDataTypes = CSharpOpaqueDataTypes(
                clangAbstractSyntaxTree.OpaqueTypes);
            var enums = CSharpEnums(clangAbstractSyntaxTree.Enums);
            var variables = CSharpVariablesExtern(clangAbstractSyntaxTree.Variables);

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

        private static ImmutableArray<CSharpFunction> CSharpFunctions(
            ImmutableArray<ClangFunction> clangFunctionExterns)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunction>(clangFunctionExterns.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExtern in clangFunctionExterns)
            {
                var functionExtern = CSharpFunction(clangFunctionExtern);
                builder.Add(functionExtern);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpFunction CSharpFunction(ClangFunction clangFunction)
        {
            var name = clangFunction.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangFunction);

            var returnType = CSharpType(clangFunction.ReturnType);
            var callingConvention = CSharpFunctionCallingConvention(clangFunction.CallingConvention);
            var parameters = CSharpFunctionParameters(clangFunction.Parameters);

            var result = new CSharpFunction(
                name,
                originalCodeLocationComment,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
            ClangFunctionCallingConvention clangFunctionCallingConvention)
        {
            var result = clangFunctionCallingConvention switch
            {
                ClangFunctionCallingConvention.C => CSharp.CSharpFunctionCallingConvention.Cdecl,
                ClangFunctionCallingConvention.Unknown => CSharp.CSharpFunctionCallingConvention.Default,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(clangFunctionCallingConvention), clangFunctionCallingConvention, null)
            };

            return result;
        }

        private static ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
            ImmutableArray<ClangFunctionParameter> clangFunctionExternParameters)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionParameter>(clangFunctionExternParameters.Length);
            var parameterNames = new List<string>();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExternParameter in clangFunctionExternParameters)
            {
                var parameterName = CSharpUniqueParameterName(clangFunctionExternParameter.Name, parameterNames);
                parameterNames.Add(parameterName);
                var functionExternParameter =
                    CSharpFunctionExternParameter(clangFunctionExternParameter, parameterName);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static string CSharpUniqueParameterName(string parameterName, List<string> parameterNames)
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

                var parameterSuffixNumber =
                    int.Parse(parameterSuffix, NumberStyles.Integer, CultureInfo.InvariantCulture);
                parameterSuffixNumber += 1;
                var parameterName = parameterNameWithoutSuffix + parameterSuffixNumber;
                return parameterName;
            }
        }

        private static CSharpFunctionParameter CSharpFunctionExternParameter(
            ClangFunctionParameter clangFunctionParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangFunctionParameter);
            var type = CSharpType(clangFunctionParameter.Type);

            var result = new CSharpFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpPointerFunction> CSharpPointerFunctions(
            ImmutableArray<ClangPointerFunction> clangFunctionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpPointerFunction>(clangFunctionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointer in clangFunctionPointers)
            {
                var functionPointer = CSharpPointerFunction(clangFunctionPointer)!;
                builder.Add(functionPointer);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpPointerFunction? CSharpPointerFunction(ClangPointerFunction clangPointerFunction)
        {
            if (IsBuiltinPointerFunction(clangPointerFunction.Type.OriginalName))
            {
                return null;
            }

            string name = clangPointerFunction.Name;
            if (clangPointerFunction.IsWrapped)
            {
                name = $"FnPtr_{clangPointerFunction.Name}";
            }

            var isBuiltIn = IsBuiltinPointerFunction(clangPointerFunction.Type.OriginalName);

            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangPointerFunction);
            var returnType = CSharpType(clangPointerFunction.ReturnType);
            var parameters = CSharpPointerFunctionParameters(clangPointerFunction.Parameters);

            var result = new CSharpPointerFunction(
                name,
                isBuiltIn,
                originalCodeLocationComment,
                returnType,
                parameters);

            return result;
        }

        private static ImmutableArray<CSharpPointerFunctionParameter> CSharpPointerFunctionParameters(
            ImmutableArray<ClangPointerFunctionParameter> clangFunctionPointerParameters)
        {
            var builder =
                ImmutableArray.CreateBuilder<CSharpPointerFunctionParameter>(clangFunctionPointerParameters.Length);
            var parameterNames = new List<string>();

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointerParameter in clangFunctionPointerParameters)
            {
                var parameterName = CSharpUniqueParameterName(clangFunctionPointerParameter.Name, parameterNames);
                parameterNames.Add(parameterName);
                var functionExternParameter =
                    CSharpPointerFunctionParameter(clangFunctionPointerParameter, parameterName);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpPointerFunctionParameter CSharpPointerFunctionParameter(
            ClangPointerFunctionParameter clangPointerFunctionParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangPointerFunctionParameter);
            var type = CSharpType(clangPointerFunctionParameter.Type);

            var result = new CSharpPointerFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpStruct> CSharpStructs(
            ImmutableArray<ClangRecord> records,
            ImmutableArray<ClangTypedef> typedefs)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(
                records.Length + typedefs.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecord in records)
            {
                var @struct = CSharpStruct(clangRecord);
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

        private static CSharpStruct CSharpStruct(ClangRecord clangRecord)
        {
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangRecord);
            var type = CSharpType(clangRecord.Type);
            var fields = CSharpStructFields(clangRecord.Fields);
            var nestedNodes = CSharpNestedNodes(clangRecord.NestedNodes);

            return new CSharpStruct(
                originalCodeLocationComment,
                type,
                fields,
                nestedNodes);
        }

        private static ImmutableArray<CSharpStructField> CSharpStructFields(
            ImmutableArray<ClangRecordField> clangRecordFields)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStructField>(clangRecordFields.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecordField in clangRecordFields)
            {
                var structField = CSharpStructField(clangRecordField);
                builder.Add(structField);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpStructField CSharpStructField(ClangRecordField clangRecordField)
        {
            var name = CSharpSanitizeIdentifier(clangRecordField.Name);
            var originalName = clangRecordField.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangRecordField);

            CSharpType type;
            if (clangRecordField.IsUnNamedFunctionPointer && !IsBuiltinPointerFunction(clangRecordField.Type.OriginalName))
            {
                type = CSharpType(clangRecordField.Type, $"FnPtr_{name}");
            }
            else
            {
                type = CSharpType(clangRecordField.Type);
            }

            var offset = clangRecordField.Offset;
            var padding = clangRecordField.Padding;
            var isWrapped = type.IsArray && !CSharpIsValidFixedBufferType(type.Name);

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

        private static ImmutableArray<CSharpNode> CSharpNestedNodes(ImmutableArray<ClangNode> nodes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpNode>(nodes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var node in nodes)
            {
                CSharpNode? nestedNode = node switch
                {
                    ClangRecord record => CSharpStruct(record),
                    ClangPointerFunction functionPointer => CSharpPointerFunction(functionPointer),
                    _ => throw new NotImplementedException()
                };

                if (nestedNode != null)
                {
                    builder.Add(nestedNode);
                }
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static ImmutableArray<CSharpOpaqueType> CSharpOpaqueDataTypes(
            ImmutableArray<ClangOpaqueType> opaqueDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(opaqueDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangOpaqueDataType in opaqueDataTypes)
            {
                var opaqueDataType = CSharpOpaqueDataType(clangOpaqueDataType);
                builder.Add(opaqueDataType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpOpaqueType CSharpOpaqueDataType(ClangOpaqueType clangOpaqueType)
        {
            var name = clangOpaqueType.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangOpaqueType);

            var result = new CSharpOpaqueType(
                name,
                originalCodeLocationComment,
                clangOpaqueType.SizeOf,
                clangOpaqueType.AlignOf);

            return result;
        }

        private static ImmutableArray<CSharpTypedef> CSharpTypedefs(ImmutableArray<ClangTypedef> typedefs)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpTypedef>(typedefs.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangTypedef in typedefs)
            {
                var typedef = CSharpTypedef(clangTypedef);
                builder.Add(typedef);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpTypedef CSharpTypedef(ClangTypedef clangTypedef)
        {
            var name = clangTypedef.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangTypedef);
            var type = CSharpType(clangTypedef.UnderlyingType);

            var result = new CSharpTypedef(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpEnum> CSharpEnums(ImmutableArray<ClangEnum> clangEnums)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnum>(clangEnums.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnum in clangEnums)
            {
                var @enum = CSharpEnum(clangEnum);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpEnum CSharpEnum(ClangEnum clangEnum)
        {
            var name = clangEnum.Type.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangEnum);
            var type = CSharpType(clangEnum.IntegerType);
            var values = CSharpEnumValues(clangEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                type,
                values);
            return result;
        }

        private static ImmutableArray<CSharpEnumValue> CSharpEnumValues(ImmutableArray<ClangEnumValue> clangEnumValues)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(clangEnumValues.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnumValue in clangEnumValues)
            {
                var @enum = CSharpEnumValue(clangEnumValue);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpEnumValue CSharpEnumValue(ClangEnumValue clangEnumValue)
        {
            var name = clangEnumValue.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangEnumValue);
            var value = clangEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                originalCodeLocationComment,
                value);

            return result;
        }

        private static ImmutableArray<CSharpVariable> CSharpVariablesExtern(
            ImmutableArray<ClangVariable> clangVariables)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpVariable>(clangVariables.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangVariable in clangVariables)
            {
                var variable = CSharpVariable(clangVariable);
                builder.Add(variable);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private static CSharpVariable CSharpVariable(ClangVariable clangVariable)
        {
            var name = clangVariable.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(clangVariable);
            var type = CSharpType(clangVariable.Type);

            var result = new CSharpVariable(name, originalCodeLocationComment, type);
            return result;
        }

        private static CSharpType CSharpType(ClangType clangType, string? typeName = null)
        {
            var typeName2 = typeName ?? CSharpTypeName(clangType);
            var originalName = clangType.OriginalName;
            var sizeOf = clangType.SizeOf;
            var alignOf = clangType.AlignOf;
            var fixedBufferSize = clangType.ArraySize;

            // https://github.com/lithiumtoast/c2cs/issues/15
            if (originalName == "va_list")
            {
                typeName2 = "IntPtr";
            }

            var result = new CSharpType(
                typeName2,
                originalName,
                sizeOf,
                alignOf,
                fixedBufferSize);

            return result;
        }

        private static string CSharpTypeName(ClangType type)
        {
            if (type.OriginalName.Contains("(*)("))
            {
                return CSharpTypeNameMapPointerFunction(type);
            }

            if (!type.IsSystemType)
            {
                return type.Name;
            }

            if (!type.Name.EndsWith("*", StringComparison.InvariantCulture))
            {
                return CSharpTypeNameMapElement(type.Name, type.ElementSize);
            }

            return CSharpTypeNameMapPointer(type);
        }

        public static bool IsBuiltinPointerFunction(string originalTypeName)
        {
            return BuiltInPointerFunctionMappings.ContainsKey(originalTypeName);
        }

        private static string CSharpTypeNameMapPointerFunction(ClangType type)
        {
            return BuiltInPointerFunctionMappings.TryGetValue(type.OriginalName, out var typeName)
                ? typeName
                : type.Name;
        }

        private static string CSharpTypeNameMapPointer(ClangType type)
        {
            if (type.Name.Contains("char*"))
            {
                return type.Name.Replace("char*", "AnsiStringPtr");
            }

            var elementTypeName = type.Name.TrimEnd('*');
            var pointersTypeName = type.Name[elementTypeName.Length..];
            var mappedElementTypeName = CSharpTypeNameMapElement(elementTypeName, type.ElementSize);
            return mappedElementTypeName + pointersTypeName;
        }

        private static string CSharpTypeNameMapElement(string typeName, int sizeOf)
        {
            return typeName switch
            {
                "char" => "byte",
                "bool" => "CBool",
                "_Bool" => "CBool",
                "int8_t" => "sbyte",
                "uint8_t" => "byte",
                "int16_t" => "short",
                "uint16_t" => "ushort",
                "int32_t" => "int",
                "uint32_t" => "uint",
                "int64_t" => "long",
                "uint64_t" => "ulong",
                "uintptr_t" => "UIntPtr",
                "intptr_t" => "IntPtr",
                "unsigned char" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned short" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned short int" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned int" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned long" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned long int" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned long long" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "unsigned long long int" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "size_t" => CSharpTypeNameMapUnsignedInteger(sizeOf),
                "signed char" => CSharpTypeNameMapSignedInteger(sizeOf),
                "short" => CSharpTypeNameMapSignedInteger(sizeOf),
                "short int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed short" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed short int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "long" => CSharpTypeNameMapSignedInteger(sizeOf),
                "long int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed long" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed long int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "long long" => CSharpTypeNameMapSignedInteger(sizeOf),
                "long long int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "signed long long int" => CSharpTypeNameMapSignedInteger(sizeOf),
                "ssize_t" => CSharpTypeNameMapSignedInteger(sizeOf),
                _ => typeName
            };
        }

        private static string CSharpTypeNameMapUnsignedInteger(int sizeOf)
        {
            return sizeOf switch
            {
                1 => "byte",
                2 => "ushort",
                4 => "uint",
                8 => "ulong",
                _ => throw new InvalidOperationException()
            };
        }

        private static string CSharpTypeNameMapSignedInteger(int sizeOf)
        {
            return sizeOf switch
            {
                1 => "sbyte",
                2 => "short",
                4 => "int",
                8 => "long",
                _ => throw new InvalidOperationException()
            };
        }

        private static string CSharpOriginalCodeLocationComment(ClangNode node)
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

        private static string CSharpSanitizeIdentifier(string name)
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

        private static bool CSharpIsValidFixedBufferType(string typeString)
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
