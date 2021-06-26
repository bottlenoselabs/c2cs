// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using C2CS.UseCases.AbstractSyntaxTreeC;

namespace C2CS.UseCases.BindgenCSharp
{
    public class CSharpMapper
    {
        private ImmutableDictionary<string, CType> _types = null!;
        private readonly ImmutableDictionary<string, string> _aliasesLookup;
        private readonly ImmutableHashSet<string> _builtinAliases;

        private static readonly Dictionary<string, string> BuiltInPointerFunctionMappings = new()
        {
            // FnPtr{FIRST_PARAM_TYPE}{SECOND_PARAM_TYPE}...{LAST_PARAM_TYPE}{RETURN_TYPE}
            {"void (void)", "FnPtrVoid"},
            {"void *(void)", "FnPtrPointer"},
            {"void *(void *)", "FnPtrPointerPointer"},
            {"void (void *)", "FnPtrPointerVoid"},
            {"int (void *, void *)", "FnPtrPointerPointerInt"},
        };

        public CSharpMapper(ImmutableArray<CSharpTypeAlias> typeAliases)
        {
            var aliasesLookup = new Dictionary<string, string>();
            var builtinAliases = new HashSet<string>();

            foreach (var typeAlias in typeAliases)
            {
                aliasesLookup.Add(typeAlias.From, typeAlias.To);

                if (typeAlias.To
                    is "byte"
                    or "sbyte"
                    or "short"
                    or "ushort"
                    or "int"
                    or "uint"
                    or "long"
                    or "ulong"
                    or "CBool")
                {
                    builtinAliases.Add(typeAlias.From);
                }
            }

            _aliasesLookup = aliasesLookup.ToImmutableDictionary();
            _builtinAliases = builtinAliases.ToImmutableHashSet();
        }

        public CSharpAbstractSyntaxTree AbstractSyntaxTree(CAbstractSyntaxTree abstractSyntaxTree)
        {
            _types = abstractSyntaxTree.Types.ToImmutableDictionary(x => x.Name);

            var functionExterns = Functions(
                abstractSyntaxTree.Functions);
            var functionPointers = FunctionPointers(
                abstractSyntaxTree.FunctionPointers);

            var recordsBuilder = ImmutableArray.CreateBuilder<CRecord>();
            foreach (var record in abstractSyntaxTree.Records)
            {
                if (_builtinAliases.Contains(record.Type))
                {
                    // short circuit, prevents emitting the type
                    continue;
                }

                recordsBuilder.Add(record);
            }

            var structs = Structs(recordsBuilder.ToImmutable());

            var typedefsBuilder = ImmutableArray.CreateBuilder<CTypedef>();
            foreach (var typedef in abstractSyntaxTree.Typedefs)
            {
                if (_builtinAliases.Contains(typedef.Name))
                {
                    // short circuit, prevents emitting the type
                    continue;
                }

                typedefsBuilder.Add(typedef);
            }

            var typedefs = Typedefs(typedefsBuilder.ToImmutable());
            var opaqueDataTypes = OpaqueDataTypes(
                abstractSyntaxTree.OpaqueTypes);
            var enums = Enums(abstractSyntaxTree.Enums);
            var variables = Variables(abstractSyntaxTree.Variables);

            var className = Path.GetFileNameWithoutExtension(abstractSyntaxTree.FileName);
            var result = new CSharpAbstractSyntaxTree(
                className,
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

        private ImmutableArray<CSharpFunction> Functions(
            ImmutableArray<CFunction> clangFunctionExterns)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunction>(clangFunctionExterns.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionExtern in clangFunctionExterns)
            {
                var functionExtern = Function(clangFunctionExtern);
                builder.Add(functionExtern);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunction Function(CFunction cFunction)
        {
            var name = cFunction.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cFunction);

            var cType = CType(cFunction.ReturnType);
            var returnType = Type(cType);
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
                    FunctionParameter(clangFunctionExternParameter, parameterName);
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

        private CSharpFunctionParameter FunctionParameter(
            CFunctionParameter cFunctionParameter, string parameterName)
        {
            var name = SanitizeIdentifier(parameterName);
            var originalCodeLocationComment = OriginalCodeLocationComment(cFunctionParameter);
            var cType = CType(cFunctionParameter.Type);
            var type = Type(cType);

            var result = new CSharpFunctionParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private ImmutableArray<CSharpFunctionPointer> FunctionPointers(
            ImmutableArray<CFunctionPointer> clangFunctionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(clangFunctionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangFunctionPointer in clangFunctionPointers)
            {
                var functionPointer = FunctionPointer(clangFunctionPointer)!;
                builder.Add(functionPointer);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionPointer? FunctionPointer(CFunctionPointer cFunctionPointer)
        {
            if (IsBuiltinFunctionPointer(cFunctionPointer.Type))
            {
                return null;
            }

            string name = cFunctionPointer.Type;

            if (cFunctionPointer.IsWrapped)
            {
                name = $"FnPtr_{cFunctionPointer.Name}";
            }

            var originalCodeLocationComment = OriginalCodeLocationComment(cFunctionPointer);
            var returnType = Type(CType(cFunctionPointer.ReturnType));
            var parameters = FunctionPointerParameters(cFunctionPointer.Parameters);

            var result = new CSharpFunctionPointer(
                name,
                true,
                originalCodeLocationComment,
                returnType,
                parameters);

            return result;
        }

        private ImmutableArray<CSharpFunctionPointerParameter> FunctionPointerParameters(
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
                    FunctionPointerParameter(clangFunctionPointerParameter, parameterName);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpFunctionPointerParameter FunctionPointerParameter(
            CFunctionPointerParameter cFunctionPointerParameter, string parameterName)
        {
            var name = SanitizeIdentifier(parameterName);
            var originalCodeLocationComment = OriginalCodeLocationComment(cFunctionPointerParameter);
            var cType = CType(cFunctionPointerParameter.Type);
            var type = Type(cType);

            var result = new CSharpFunctionPointerParameter(
                name,
                originalCodeLocationComment,
                type);

            return result;
        }

        private ImmutableArray<CSharpStruct> Structs(ImmutableArray<CRecord> records)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecord in records)
            {
                var @struct = Struct(clangRecord);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStruct Struct(CRecord cRecord)
        {
            var originalCodeLocationComment = OriginalCodeLocationComment(cRecord);
            var cType = CType(cRecord.Type);
            var type = Type(cType);
            var fields = StructFields(cRecord.Fields);
            var nestedStructs = NestedStructs(cRecord.NestedRecords);
            var nestedFunctionPointers = NestedFunctionPointers(cRecord.NestedFunctionPointers);

            return new CSharpStruct(
                originalCodeLocationComment,
                type,
                fields,
                nestedStructs,
                nestedFunctionPointers);
        }

        private ImmutableArray<CSharpStructField> StructFields(
            ImmutableArray<CRecordField> clangRecordFields)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStructField>(clangRecordFields.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangRecordField in clangRecordFields)
            {
                var structField = StructField(clangRecordField);
                builder.Add(structField);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpStructField StructField(CRecordField cRecordField)
        {
            var name = SanitizeIdentifier(cRecordField.Name);
            var codeLocationComment = OriginalCodeLocationComment(cRecordField);
            var cType = CType(cRecordField.Type);

            CSharpType type;
            if (cType.Kind == CKind.FunctionPointer)
            {
                if (BuiltInPointerFunctionMappings.TryGetValue(cRecordField.Type, out var functionPointerName))
                {
                    type = Type(cType, functionPointerName);
                }
                else
                {
                    type = Type(cType, $"FnPtr_{name}");
                }
            }
            else
            {
                type = Type(cType);
            }

            var offset = cRecordField.Offset;
            var padding = cRecordField.Padding;
            var isWrapped = type.IsArray && !IsValidFixedBufferType(type.Name);

            var result = new CSharpStructField(
                name,
                codeLocationComment,
                type,
                offset,
                padding,
                isWrapped);

            return result;
        }

        private ImmutableArray<CSharpStruct> NestedStructs(ImmutableArray<CRecord> records)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var record in records)
            {
                var @struct = Struct(record);
                builder.Add(@struct);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ImmutableArray<CSharpFunctionPointer> NestedFunctionPointers(ImmutableArray<CFunctionPointer> functionPointers)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var functionPointer in functionPointers)
            {
                var functionPointerCSharp = FunctionPointer(functionPointer);
                if (functionPointerCSharp != null)
                {
                    builder.Add(functionPointerCSharp);
                }
            }

            var result = builder.ToImmutable();
            return result;
        }

        private ImmutableArray<CSharpOpaqueType> OpaqueDataTypes(
            ImmutableArray<COpaqueType> opaqueDataTypes)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(opaqueDataTypes.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangOpaqueDataType in opaqueDataTypes)
            {
                var opaqueDataType = OpaqueDataType(clangOpaqueDataType);
                builder.Add(opaqueDataType);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpOpaqueType OpaqueDataType(COpaqueType cOpaqueType)
        {
            var name = cOpaqueType.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cOpaqueType);

            var result = new CSharpOpaqueType(
                name,
                originalCodeLocationComment);

            return result;
        }

        private ImmutableArray<CSharpTypedef> Typedefs(ImmutableArray<CTypedef> typedefs)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpTypedef>(typedefs.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangTypedef in typedefs)
            {
                var typedef = Typedef(clangTypedef);
                builder.Add(typedef);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpTypedef Typedef(CTypedef cTypedef)
        {
            var name = cTypedef.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cTypedef);
            var cUnderlingType = CType(cTypedef.UnderlyingType);
            var underlyingType = Type(cUnderlingType);

            var result = new CSharpTypedef(
                name,
                originalCodeLocationComment,
                underlyingType);

            return result;
        }

        private ImmutableArray<CSharpEnum> Enums(ImmutableArray<CEnum> clangEnums)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnum>(clangEnums.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnum in clangEnums)
            {
                var @enum = Enum(clangEnum);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpEnum Enum(CEnum cEnum)
        {
            var name = cEnum.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cEnum);
            var cIntegerType = CType(cEnum.IntegerType);
            var integerType = Type(cIntegerType);
            var values = EnumValues(cEnum.Values);

            var result = new CSharpEnum(
                name,
                originalCodeLocationComment,
                integerType,
                values);
            return result;
        }

        private ImmutableArray<CSharpEnumValue> EnumValues(ImmutableArray<CEnumValue> clangEnumValues)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(clangEnumValues.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangEnumValue in clangEnumValues)
            {
                var @enum = EnumValue(clangEnumValue);
                builder.Add(@enum);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpEnumValue EnumValue(CEnumValue cEnumValue)
        {
            var name = cEnumValue.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cEnumValue);
            var value = cEnumValue.Value;

            var result = new CSharpEnumValue(
                name,
                originalCodeLocationComment,
                value);

            return result;
        }

        private ImmutableArray<CSharpVariable> Variables(
            ImmutableArray<CVariable> clangVariables)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpVariable>(clangVariables.Length);

            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (var clangVariable in clangVariables)
            {
                var variable = Variable(clangVariable);
                builder.Add(variable);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CSharpVariable Variable(CVariable cVariable)
        {
            var name = cVariable.Name;
            var originalCodeLocationComment = OriginalCodeLocationComment(cVariable);
            var cType = CType(cVariable.Type);
            var type = Type(cType);

            var result = new CSharpVariable(name, originalCodeLocationComment, type);
            return result;
        }

        private CSharpType Type(CType cType, string? typeName = null)
        {
            var typeName2 = typeName ?? TypeName(cType);
            var sizeOf = cType.SizeOf ?? 0;
            var alignOf = cType.AlignOf ?? 0;
            var fixedBufferSize = cType.ArraySize ?? 0;

            var result = new CSharpType(
                typeName2,
                cType.Name,
                sizeOf,
                alignOf,
                fixedBufferSize);

            return result;
        }

        private string TypeName(CType type)
        {
            var originalName = type.Name;
            if (type.Kind == CKind.FunctionPointer)
            {
                return CSharpTypeNameMapPointerFunction(type);
            }

            var name = type.Name;
            var elementTypeSize = type.ElementSize ?? type.SizeOf ?? 0;
            string typeName;

            if (name.EndsWith("*", StringComparison.InvariantCulture) || name.EndsWith("]", StringComparison.InvariantCulture))
            {
                typeName = TypeNameMapPointer(type, elementTypeSize, type.IsSystem);
            }
            else
            {
                typeName = TypeNameMapElement(name, elementTypeSize, type.IsSystem);
            }

            // TODO: https://github.com/lithiumtoast/c2cs/issues/15
            if (typeName == "va_list")
            {
                typeName = "IntPtr";
            }

            return typeName;
        }

        public static bool IsBuiltinFunctionPointer(string name)
        {
            return BuiltInPointerFunctionMappings.ContainsKey(name);
        }

        private static string CSharpTypeNameMapPointerFunction(CType type)
        {
            if (BuiltInPointerFunctionMappings.TryGetValue(type.Name!, out var typeName))
            {
                return typeName;
            }
            else
            {
                // TODO: What happens when the function pointer name is not found for types used in parameters?
                return type.Name;
            }
        }

        private string TypeNameMapPointer(CType type, int sizeOf, bool isSystem)
        {
            var pointerTypeName = type.Name;

            // Replace [] with *
            while (true)
            {
                var x = pointerTypeName.IndexOf('[');

                if (x == -1)
                {
                    break;
                }

                var y = pointerTypeName.IndexOf(']', x);

                pointerTypeName = pointerTypeName[..x] + "*" + pointerTypeName[(y + 1)..];
            }

            if (pointerTypeName.Contains("char*"))
            {
                return pointerTypeName.Replace("char*", "CString");
            }

            var elementTypeName = pointerTypeName.TrimEnd('*');
            var pointersTypeName = pointerTypeName[elementTypeName.Length..];
            var mappedElementTypeName = TypeNameMapElement(elementTypeName, sizeOf, isSystem);
            return mappedElementTypeName + pointersTypeName;
        }

        private string TypeNameMapElement(string typeName, int sizeOf, bool isSystem)
        {
            if (!isSystem)
            {
                if (_aliasesLookup.TryGetValue(typeName, out var aliasName))
                {
                    return aliasName;
                }

                return typeName;
            }

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
                    return TypeNameMapUnsignedInteger(sizeOf);
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
                    return TypeNameMapSignedInteger(sizeOf);
                default:
                    return typeName;
            }
        }

        private static string TypeNameMapUnsignedInteger(int sizeOf)
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

        private static string TypeNameMapSignedInteger(int sizeOf)
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

        private static string OriginalCodeLocationComment(CNode node)
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

        private static string SanitizeIdentifier(string name)
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
