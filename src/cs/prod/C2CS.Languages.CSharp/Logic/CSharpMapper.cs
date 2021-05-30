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

        public static CSharpAbstractSyntaxTree AbstractSyntaxTree(
            CAbstractSyntaxTree cAbstractSyntaxTree)
        {
            var functionExterns = CSharpFunctions(
                cAbstractSyntaxTree.FunctionExterns);
            var functionPointers = CSharpPointerFunctions(
                cAbstractSyntaxTree.FunctionPointers);
            var structs = CSharpStructs(
                cAbstractSyntaxTree.Records,
                cAbstractSyntaxTree.Typedefs);
            var typedefs = CSharpTypedefs(cAbstractSyntaxTree.Typedefs);
            var opaqueDataTypes = CSharpOpaqueDataTypes(
                cAbstractSyntaxTree.OpaqueTypes);
            var enums = CSharpEnums(cAbstractSyntaxTree.Enums);
            var variables = CSharpVariablesExtern(cAbstractSyntaxTree.Variables);

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
            ImmutableArray<CFunction> clangFunctionExterns)
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

        private static CSharpFunction CSharpFunction(CFunction cFunction)
        {
            var name = cFunction.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunction);

            var returnType = CSharpType(cFunction.ReturnType);
            var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
            var parameters = CSharpFunctionParameters(cFunction.Parameters);

            var result = new CSharpFunction(
                name,
                originalCodeLocationComment,
                callingConvention,
                returnType,
                parameters);

            return result;
        }

        private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
            CFunctionCallingConvention cFunctionCallingConvention)
        {
            var result = cFunctionCallingConvention switch
            {
                CFunctionCallingConvention.C => CSharp.CSharpFunctionCallingConvention.Cdecl,
                CFunctionCallingConvention.Unknown => CSharp.CSharpFunctionCallingConvention.Default,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cFunctionCallingConvention), cFunctionCallingConvention, null)
            };

            return result;
        }

        private static ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
            ImmutableArray<CFunctionParameter> clangFunctionExternParameters)
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
            CFunctionParameter cFunctionParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunctionParameter);
            var type = CSharpType(cFunctionParameter.Type);

            var result = new CSharpFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpPointerFunction> CSharpPointerFunctions(
            ImmutableArray<CPointerFunction> clangFunctionPointers)
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

        private static CSharpPointerFunction? CSharpPointerFunction(CPointerFunction cPointerFunction)
        {
            if (IsBuiltinPointerFunction(cPointerFunction.Type.OriginalName))
            {
                return null;
            }

            string name = cPointerFunction.Name;
            if (cPointerFunction.IsWrapped)
            {
                name = $"FnPtr_{cPointerFunction.Name}";
            }

            var isBuiltIn = IsBuiltinPointerFunction(cPointerFunction.Type.OriginalName);

            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cPointerFunction);
            var returnType = CSharpType(cPointerFunction.ReturnType);
            var parameters = CSharpPointerFunctionParameters(cPointerFunction.Parameters);

            var result = new CSharpPointerFunction(
                name,
                isBuiltIn,
                originalCodeLocationComment,
                returnType,
                parameters);

            return result;
        }

        private static ImmutableArray<CSharpPointerFunctionParameter> CSharpPointerFunctionParameters(
            ImmutableArray<CPointerFunctionParameter> clangFunctionPointerParameters)
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
            CPointerFunctionParameter cPointerFunctionParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cPointerFunctionParameter);
            var type = CSharpType(cPointerFunctionParameter.Type);

            var result = new CSharpPointerFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpStruct> CSharpStructs(
            ImmutableArray<CRecord> records,
            ImmutableArray<CTypedef> typedefs)
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

        private static CSharpStruct CSharpStruct(CRecord cRecord)
        {
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cRecord);
            var type = CSharpType(cRecord.Type);
            var fields = CSharpStructFields(cRecord.Fields);
            var nestedNodes = CSharpNestedNodes(cRecord.NestedNodes);

            return new CSharpStruct(
                originalCodeLocationComment,
                type,
                fields,
                nestedNodes);
        }

        private static ImmutableArray<CSharpStructField> CSharpStructFields(
            ImmutableArray<CRecordField> clangRecordFields)
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

        private static CSharpStructField CSharpStructField(CRecordField cRecordField)
        {
            var name = CSharpSanitizeIdentifier(cRecordField.Name);
            var originalName = cRecordField.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cRecordField);

            CSharpType type;
            if (cRecordField.IsUnNamedFunctionPointer && !IsBuiltinPointerFunction(cRecordField.Type.OriginalName))
            {
                type = CSharpType(cRecordField.Type, $"FnPtr_{name}");
            }
            else
            {
                type = CSharpType(cRecordField.Type);
            }

            var offset = cRecordField.Offset;
            var padding = cRecordField.Padding;
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

        private static ImmutableArray<CSharpNode> CSharpNestedNodes(ImmutableArray<CNode> nodes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpNode>(nodes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var node in nodes)
            {
                CSharpNode? nestedNode = node switch
                {
                    CRecord record => CSharpStruct(record),
                    CPointerFunction functionPointer => CSharpPointerFunction(functionPointer),
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
            ImmutableArray<COpaqueType> opaqueDataTypes)
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

        private static CSharpOpaqueType CSharpOpaqueDataType(COpaqueType cOpaqueType)
        {
            var name = cOpaqueType.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cOpaqueType);

            var result = new CSharpOpaqueType(
                name,
                originalCodeLocationComment,
                cOpaqueType.SizeOf,
                cOpaqueType.AlignOf);

            return result;
        }

        private static ImmutableArray<CSharpTypedef> CSharpTypedefs(ImmutableArray<CTypedef> typedefs)
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

        private static CSharpTypedef CSharpTypedef(CTypedef cTypedef)
        {
            var name = cTypedef.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cTypedef);
            var type = CSharpType(cTypedef.UnderlyingType);

            var result = new CSharpTypedef(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private static ImmutableArray<CSharpEnum> CSharpEnums(ImmutableArray<CEnum> clangEnums)
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

        private static CSharpEnum CSharpEnum(CEnum cEnum)
        {
            var name = cEnum.Type.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cEnum);
            var type = CSharpType(cEnum.IntegerType);
            var values = CSharpEnumValues(cEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                type,
                values);
            return result;
        }

        private static ImmutableArray<CSharpEnumValue> CSharpEnumValues(ImmutableArray<CEnumValue> clangEnumValues)
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

        private static CSharpEnumValue CSharpEnumValue(CEnumValue cEnumValue)
        {
            var name = cEnumValue.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cEnumValue);
            var value = cEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                originalCodeLocationComment,
                value);

            return result;
        }

        private static ImmutableArray<CSharpVariable> CSharpVariablesExtern(
            ImmutableArray<CVariable> clangVariables)
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

        private static CSharpVariable CSharpVariable(CVariable cVariable)
        {
            var name = cVariable.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cVariable);
            var type = CSharpType(cVariable.Type);

            var result = new CSharpVariable(name, originalCodeLocationComment, type);
            return result;
        }

        private static CSharpType CSharpType(CType cType, string? typeName = null)
        {
            var typeName2 = typeName ?? CSharpTypeName(cType);
            var originalName = cType.OriginalName;
            var sizeOf = cType.SizeOf;
            var alignOf = cType.AlignOf;
            var fixedBufferSize = cType.ArraySize;

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

        private static string CSharpTypeName(CType type)
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

        private static string CSharpTypeNameMapPointerFunction(CType type)
        {
            return BuiltInPointerFunctionMappings.TryGetValue(type.OriginalName, out var typeName)
                ? typeName
                : type.Name;
        }

        private static string CSharpTypeNameMapPointer(CType type)
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

        private static string CSharpOriginalCodeLocationComment(CNode node)
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
