// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using C2CS.UseCases.AbstractSyntaxTreeC;

namespace C2CS.UseCases.BindgenCSharp
{
    public class CSharpMapper
    {
        private ImmutableDictionary<string, CType> _types = null!;

        private static readonly Dictionary<string, string> BuiltInPointerFunctionMappings = new()
        {
            {"void (*)(void)", "FnPtrVoid"},
            {"void (*)(void *)", "FnPtrVoidPointer"},
            {"int (*)(void *, void *)", "FnPtrIntPointerPointer"}
        };

        public CSharpAbstractSyntaxTree AbstractSyntaxTree(CAbstractSyntaxTree abstractSyntaxTree)
        {
            _types = abstractSyntaxTree.Types.ToImmutableDictionary(x => x.Name);

            var functionExterns = CSharpFunctions(
                abstractSyntaxTree.Functions);
            var functionPointers = CSharpPointerFunctions(
                abstractSyntaxTree.FunctionPointers);
            var structs = CSharpStructs(
                abstractSyntaxTree.Records,
                abstractSyntaxTree.Typedefs);
            var typedefs = CSharpTypedefs(abstractSyntaxTree.Typedefs);
            var opaqueDataTypes = CSharpOpaqueDataTypes(
                abstractSyntaxTree.OpaqueTypes);
            var enums = CSharpEnums(abstractSyntaxTree.Enums);
            var variables = CSharpVariablesExtern(abstractSyntaxTree.Variables);

            var result = new CSharpAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                structs,
                typedefs,
                opaqueDataTypes,
                enums,
                variables);

            _types = null!;

            return result;
        }

        private ImmutableArray<CSharpFunction> CSharpFunctions(
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

        private CSharpFunction CSharpFunction(CFunction cFunction)
        {
            var name = cFunction.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunction);

            var cType = CType(cFunction.ReturnType);
            var returnType = CSharpType(cType);
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

        private CType CType(string typeName)
        {
            if (_types.TryGetValue(typeName, out var type))
            {
                return type;
            }

            throw new NotImplementedException("ya");
        }

        private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
            CFunctionCallingConvention cFunctionCallingConvention)
        {
            var result = cFunctionCallingConvention switch
            {
                CFunctionCallingConvention.C => BindgenCSharp.CSharpFunctionCallingConvention.Cdecl,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(cFunctionCallingConvention), cFunctionCallingConvention, null)
            };

            return result;
        }

        private ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
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

        private CSharpFunctionParameter CSharpFunctionExternParameter(
            CFunctionParameter cFunctionParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunctionParameter);
            var cType = CType(cFunctionParameter.Type);
            var type = CSharpType(cType);

            var result = new CSharpFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private ImmutableArray<CSharpFunctionPointer> CSharpPointerFunctions(
            ImmutableArray<CFunctionPointer> clangFunctionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(clangFunctionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointer in clangFunctionPointers)
            {
                var functionPointer = CSharpPointerFunction(clangFunctionPointer)!;
                builder.Add(functionPointer);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionPointer? CSharpPointerFunction(CFunctionPointer cFunctionPointer)
        {
            if (IsBuiltinPointerFunction(cFunctionPointer.Type))
            {
                return null;
            }

            string name = cFunctionPointer.Name;
            if (cFunctionPointer.IsWrapped)
            {
                name = $"FnPtr_{cFunctionPointer.Name}";
            }

            var isBuiltIn = IsBuiltinPointerFunction(cFunctionPointer.Type);

            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunctionPointer);
            var cType = CType(cFunctionPointer.Type);
            var returnType = CSharpType(cType);
            var parameters = CSharpPointerFunctionParameters(cFunctionPointer.Parameters);

            var result = new CSharpFunctionPointer(
                name,
                isBuiltIn,
                originalCodeLocationComment,
                returnType,
                parameters);

            return result;
        }

        private ImmutableArray<CSharpFunctionPointerParameter> CSharpPointerFunctionParameters(
            ImmutableArray<CFunctionPointerParameter> clangFunctionPointerParameters)
        {
            var builder =
                ImmutableArray.CreateBuilder<CSharpFunctionPointerParameter>(clangFunctionPointerParameters.Length);
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

        private CSharpFunctionPointerParameter CSharpPointerFunctionParameter(
            CFunctionPointerParameter cFunctionPointerParameter, string parameterName)
        {
            var name = CSharpSanitizeIdentifier(parameterName);
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cFunctionPointerParameter);
            var cType = CType(cFunctionPointerParameter.Type);
            var type = CSharpType(cType);

            var result = new CSharpFunctionPointerParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private ImmutableArray<CSharpStruct> CSharpStructs(
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

        private CSharpStruct CSharpStruct(CRecord cRecord)
        {
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cRecord);
            var cType = CType(cRecord.Type);
            var type = CSharpType(cType);
            var fields = CSharpStructFields(cRecord.Fields);
            var nestedStructs = CSharpNestedStructs(cRecord.NestedRecords);
            var nestedFunctionPointers = CSharpNestedFunctionPointers(cRecord.NestedFunctionPointers);

            return new CSharpStruct(
                originalCodeLocationComment,
                type,
                fields,
                nestedStructs,
                nestedFunctionPointers);
        }

        private ImmutableArray<CSharpStructField> CSharpStructFields(
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

        private CSharpStructField CSharpStructField(CRecordField cRecordField)
        {
            var name = CSharpSanitizeIdentifier(cRecordField.Name);
            var codeLocationComment = CSharpOriginalCodeLocationComment(cRecordField);
            var cType = CType(cRecordField.Type);

            CSharpType type;
            if (cType.Kind == CKind.FunctionPointer && !IsBuiltinPointerFunction(cRecordField.Type))
            {
                type = CSharpType(cType, $"FnPtr_{name}");
            }
            else
            {
                type = CSharpType(cType);
            }

            var offset = cRecordField.Offset;
            var padding = cRecordField.Padding;
            var isWrapped = type.IsArray && !CSharpIsValidFixedBufferType(type.Name);

            var result = new CSharpStructField(
                name,
                codeLocationComment,
                type,
                offset,
                padding,
                isWrapped);

            return result;
        }

        private ImmutableArray<CSharpStruct> CSharpNestedStructs(ImmutableArray<CRecord> records)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var record in records)
            {
                var @struct = CSharpStruct(record);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ImmutableArray<CSharpFunctionPointer> CSharpNestedFunctionPointers(ImmutableArray<CFunctionPointer> functionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var functionPointer in functionPointers)
            {
                var functionPointerCSharp = CSharpPointerFunction(functionPointer);
                if (functionPointerCSharp != null)
                {
                    builder.Add(functionPointerCSharp);
                }
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ImmutableArray<CSharpOpaqueType> CSharpOpaqueDataTypes(
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

        private CSharpOpaqueType CSharpOpaqueDataType(COpaqueType cOpaqueType)
        {
            var name = cOpaqueType.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cOpaqueType);

            var result = new CSharpOpaqueType(
                name,
                originalCodeLocationComment);

            return result;
        }

        private ImmutableArray<CSharpTypedef> CSharpTypedefs(ImmutableArray<CTypedef> typedefs)
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

        private CSharpTypedef CSharpTypedef(CTypedef cTypedef)
        {
            var name = cTypedef.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cTypedef);
            var cUnderlingType = CType(cTypedef.UnderlyingType);
            var underlyingType = CSharpType(cUnderlingType);

            var result = new CSharpTypedef(
                name,
                originalCodeLocationComment,
                underlyingType);

            return result;
        }

        private ImmutableArray<CSharpEnum> CSharpEnums(ImmutableArray<CEnum> clangEnums)
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

        private CSharpEnum CSharpEnum(CEnum cEnum)
        {
            var name = cEnum.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cEnum);
            var cIntegerType = CType(cEnum.IntegerType);
            var integerType = CSharpType(cIntegerType);
            var values = CSharpEnumValues(cEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                integerType,
                values);
            return result;
        }

        private ImmutableArray<CSharpEnumValue> CSharpEnumValues(ImmutableArray<CEnumValue> clangEnumValues)
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

        private CSharpEnumValue CSharpEnumValue(CEnumValue cEnumValue)
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

        private ImmutableArray<CSharpVariable> CSharpVariablesExtern(
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

        private CSharpVariable CSharpVariable(CVariable cVariable)
        {
            var name = cVariable.Name;
            var originalCodeLocationComment = CSharpOriginalCodeLocationComment(cVariable);
            var cType = CType(cVariable.Type);
            var type = CSharpType(cType);

            var result = new CSharpVariable(name, originalCodeLocationComment, type);
            return result;
        }

        private static CSharpType CSharpType(CType cType, string? typeName = null)
        {
            var typeName2 = typeName ?? CSharpTypeName(cType);
            var sizeOf = cType.SizeOf ?? 0;
            var alignOf = cType.AlignOf ?? 0;
            var fixedBufferSize = cType.ArraySize ?? 0;

            // https://github.com/lithiumtoast/c2cs/issues/15
            if (typeName2 == "va_list")
            {
                typeName2 = "IntPtr";
            }

            var result = new CSharpType(
                typeName2,
                cType.Name,
                sizeOf,
                alignOf,
                fixedBufferSize);

            return result;
        }

        private static string CSharpTypeName(CType type)
        {
            var originalName = type.Name;
            if (originalName.Contains("(*)("))
            {
                return CSharpTypeNameMapPointerFunction(type);
            }

            var name = type.Name;
            if (!type.IsSystem)
            {
                return name;
            }

            var elementTypeSize = type.ElementSize ?? type.SizeOf ?? 0;

            if (name.EndsWith("*", StringComparison.InvariantCulture))
            {
                return CSharpTypeNameMapPointer(type, elementTypeSize);
            }

            return CSharpTypeNameMapElement(name, elementTypeSize);
        }

        public static bool IsBuiltinPointerFunction(string name)
        {
            return BuiltInPointerFunctionMappings.ContainsKey(name);
        }

        private static string CSharpTypeNameMapPointerFunction(CType type)
        {
            return BuiltInPointerFunctionMappings.TryGetValue(type.Name!, out var typeName) ? typeName : type.Name;
        }

        private static string CSharpTypeNameMapPointer(CType type, int sizeOf)
        {
            if (type.Name.Contains("char*"))
            {
                return type.Name.Replace("char*", "CString");
            }

            var elementTypeName = type.Name.TrimEnd('*');
            var pointersTypeName = type.Name[elementTypeName.Length..];
            var mappedElementTypeName = CSharpTypeNameMapElement(elementTypeName, sizeOf);
            return mappedElementTypeName + pointersTypeName;
        }

        private static string CSharpTypeNameMapElement(string typeName, int sizeOf)
        {
            switch (typeName)
            {
                case "char":
                    return "byte";
                case "bool":
                case "_Bool":
                    return "CBool";
                case "int8_t":
                    return "sbyte";
                case "uint8_t":
                    return "byte";
                case "int16_t":
                    return "short";
                case "uint16_t":
                    return "ushort";
                case "int32_t":
                    return "int";
                case "uint32_t":
                    return "uint";
                case "int64_t":
                    return "long";
                case "uint64_t":
                    return "ulong";
                case "uintptr_t":
                    return "UIntPtr";
                case "intptr_t":
                    return "IntPtr";
                case "unsigned char":
                case "unsigned short":
                case "unsigned short int":
                case "unsigned":
                case "unsigned int":
                case "unsigned long":
                case "unsigned long int":
                case "unsigned long long":
                case "unsigned long long int":
                case "size_t":
                    return CSharpTypeNameMapUnsignedInteger(sizeOf);
                case "signed char":
                case "short":
                case "short int":
                case "signed short":
                case "signed short int":
                case "int":
                case "signed":
                case "signed int":
                case "long":
                case "long int":
                case "signed long":
                case "signed long int":
                case "long long":
                case "long long int":
                case "signed long long int":
                case "ssize_t":
                    return CSharpTypeNameMapSignedInteger(sizeOf);
                default:
                    return typeName;
            }
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
            string kindString;
            if (node is CRecord record)
            {
                kindString = record.IsUnion ? "Union" : "Struct";
            }
            else
            {
                kindString = node.Kind.ToString();
            }

            var location = node.Location;

            string result;
            if (location.IsSystem)
            {
                result = $"// {kindString} @ System";
            }
            else
            {
                result = $"// {kindString} @ {location.Path}:{location.LineNumber}:{location.LineColumn}";
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
