// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using bottlenoselabs.Common.Diagnostics;
using C2CS.Commands.WriteCodeCSharp.Data;
using C2CS.Internal;
using c2ffi.Data;
using c2ffi.Data.Nodes;

namespace C2CS.Commands.WriteCodeCSharp.Domain.Mapper;

public sealed class CSharpCodeMapper
{
    private readonly ImmutableHashSet<string> _builtinAliases;
    private readonly Dictionary<string, string> _generatedFunctionPointersNamesByCNames = new();
    private readonly ImmutableHashSet<string> _ignoredNames;
    private readonly CSharpCodeMapperOptions _options;
    private readonly ImmutableDictionary<string, string> _userTypeNameAliases;

    private static readonly string[] IgnoredNames = { "FFI_PLATFORM_NAME" };
    private static readonly char[] IdentifierSeparatorCharacters = { '_', '.', '@' };

    public CSharpCodeMapper(CSharpCodeMapperOptions options)
    {
        _options = options;

        var userAliases = new Dictionary<string, string>();
        var builtinAliases = new HashSet<string>();

        foreach (var typeAlias in options.MappedTypeNames)
        {
            if (string.IsNullOrEmpty(typeAlias.Source) || string.IsNullOrEmpty(typeAlias.Target))
            {
                continue;
            }

            userAliases.Add(typeAlias.Source, typeAlias.Target);

            if (typeAlias.Target
                is "byte"
                or "sbyte"
                or "short"
                or "ushort"
                or "int"
                or "uint"
                or "long"
                or "ulong"
                or "CBool"
                or "CChar")
            {
                builtinAliases.Add(typeAlias.Source);
            }
        }

        _userTypeNameAliases = userAliases.ToImmutableDictionary();
        _builtinAliases = builtinAliases.ToImmutableHashSet();
        _ignoredNames = options.IgnoredNames
            .Concat(options.SystemTypeAliases.Keys)
            .Concat(IgnoredNames)
            .ToImmutableHashSet();
    }

    public CSharpAbstractSyntaxTree Map(
        CFfiCrossPlatform ffi,
        DiagnosticsSink diagnostics)
    {
        var context = new CSharpCodeMapperContext(ffi);

        var functionsC = ffi.Functions.Values.ToImmutableArray();
        var functionNamesC = ffi.Functions.Keys.ToImmutableHashSet();
        var functionPointersC = ffi.FunctionPointers.Values.ToImmutableArray();
        var recordsC = ffi.Records.Values.ToImmutableArray();
        var typeAliasesC = ffi.TypeAliases.Values.ToImmutableArray();
        var opaqueTypesC = ffi.OpaqueTypes.Values.ToImmutableArray();
        var enumsC = ffi.Enums.Values.ToImmutableArray();
        var macroObjectsC = ffi.MacroObjects.Values.ToImmutableArray();

        var functions = Functions(context, functionsC);
        var structs = Structs(context, recordsC, functionNamesC);
        var enums = Enums(context, enumsC);
        var aliasStructs = AliasStructs(context, typeAliasesC);
        var functionPointers = FunctionPointers(context, functionPointersC);
        var opaqueStructs = OpaqueStructs(context, opaqueTypesC);
        var macroObjects = MacroObjects(context, macroObjectsC);

        var ast = new CSharpAbstractSyntaxTree
        {
            Functions = functions,
            FunctionPointers = functionPointers,
            Structs = structs,
            AliasStructs = aliasStructs,
            OpaqueStructs = opaqueStructs,
            Enums = enums,
            MacroObjects = macroObjects
        };

        return ast;
    }

    private ImmutableArray<CSharpFunction> Functions(
        CSharpCodeMapperContext context,
        ImmutableArray<CFunction> functions)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunction>(functions.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var function in functions)
        {
            if (_ignoredNames.Contains(function.Name))
            {
                continue;
            }

            var value = Function(context, function);
            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpFunction Function(CSharpCodeMapperContext context, CFunction cFunction)
    {
        var nameCSharp = cFunction.Name;
        var returnTypeCSharp = TypeCSharp(context, cFunction.ReturnType);
        var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
        var parameters = CSharpFunctionParameters(context, nameCSharp, cFunction.Parameters);
        var className = ClassName(nameCSharp, out var nameCSharpMapped);
        var nameCSharpFinal = IdiomaticName(nameCSharpMapped, false);

        var result = new CSharpFunction(
            nameCSharpFinal,
            className,
            cFunction.Name,
            null,
            callingConvention,
            returnTypeCSharp,
            parameters);

        return result;
    }

    private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
        CFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CFunctionCallingConvention.Cdecl => Data.CSharpFunctionCallingConvention.Cdecl,
            CFunctionCallingConvention.StdCall => Data.CSharpFunctionCallingConvention.StdCall,
            CFunctionCallingConvention.FastCall => Data.CSharpFunctionCallingConvention.FastCall,
            _ => throw new ArgumentOutOfRangeException(
                nameof(callingConvention), callingConvention, null)
        };

        return result;
    }

    private ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
        CSharpCodeMapperContext context,
        string functionName,
        ImmutableArray<CFunctionParameter> functionParameters)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionParameter>(functionParameters.Length);
        var parameterNames = new List<string>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionParameter in functionParameters)
        {
            var parameterName = CSharpUniqueParameterName(functionParameter.Name, parameterNames);
            parameterNames.Add(parameterName);
            var value =
                FunctionParameter(context, functionName, functionParameter, parameterName);
            builder.Add(value);
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
            if (string.IsNullOrEmpty(parameterSuffix))
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
        CSharpCodeMapperContext context,
        string functionName,
        CFunctionParameter cFunctionParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var typeC = cFunctionParameter.Type;
        var typeCSharp = TypeCSharp(context, typeC);

        var typeCSharpName = typeCSharp.FullName;
        var typeCSharpNameBase = typeCSharpName.TrimEnd('*');
        if (typeCSharpNameBase == functionName)
        {
            typeCSharpName = typeCSharpName.Replace(
                typeCSharpNameBase,
                typeCSharpNameBase + "_",
                StringComparison.InvariantCulture);
        }

        var nameFinal = IdiomaticName(name, false, isParameter: true);

        var functionParameterCSharp = new CSharpFunctionParameter(
            nameFinal,
            typeCSharp.ClassName,
            cFunctionParameter.Name,
            typeC.SizeOf,
            typeCSharpName);

        return functionParameterCSharp;
    }

    private ImmutableArray<CSharpFunctionPointer> FunctionPointers(
        CSharpCodeMapperContext context,
        ImmutableArray<CFunctionPointer> functionPointers)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);
        var names = new Dictionary<string, CFunctionPointer>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointer in functionPointers)
        {
            var value = FunctionPointer(context, names, functionPointer);
            if (value == null)
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpFunctionPointer? FunctionPointer(
        CSharpCodeMapperContext context,
        Dictionary<string, CFunctionPointer> names,
        CFunctionPointer cFunctionPointer)
    {
        var functionPointerType = cFunctionPointer.Type;
        var typeNameCSharp = TypeNameCSharpFunctionPointer(context, functionPointerType.Name, cFunctionPointer);
        if (names.ContainsKey(typeNameCSharp))
        {
            // This can happen if there is attributes on the function pointer return type or parameters.
            return null;
        }

        var returnTypeC = cFunctionPointer.ReturnType;
        var returnTypeCSharp = TypeCSharp(context, returnTypeC);

        var parameters = FunctionPointerParameters(context, cFunctionPointer.Parameters);
        var className = ClassName(
            typeNameCSharp,
            out var typeNameCSharpMapped);
        var typeNameCSharpFinal = IdiomaticName(typeNameCSharpMapped, false);

        var result = new CSharpFunctionPointer(
            typeNameCSharpFinal,
            className,
            cFunctionPointer.Name,
            functionPointerType.SizeOf,
            returnTypeCSharp,
            parameters);

        names.Add(typeNameCSharp, cFunctionPointer);

        return result;
    }

    private ImmutableArray<CSharpParameter> FunctionPointerParameters(
        CSharpCodeMapperContext context,
        ImmutableArray<CFunctionPointerParameter> functionPointerParameters)
    {
        var builder =
            ImmutableArray.CreateBuilder<CSharpParameter>(functionPointerParameters.Length);
        var parameterNames = new List<string>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointerParameterC in functionPointerParameters)
        {
            var parameterName = CSharpUniqueParameterName(functionPointerParameterC.Name, parameterNames);
            parameterNames.Add(parameterName);
            var value =
                FunctionPointerParameter(context, functionPointerParameterC, parameterName);
            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpParameter FunctionPointerParameter(
        CSharpCodeMapperContext context,
        CFunctionPointerParameter cFunctionPointerParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var typeC = cFunctionPointerParameter.Type;
        var typeCSharp = TypeCSharp(context, typeC);

        var result = new CSharpParameter(
            name,
            typeCSharp.ClassName,
            cFunctionPointerParameter.Name,
            typeC.SizeOf,
            typeCSharp);

        return result;
    }

    private ImmutableArray<CSharpStruct> Structs(
        CSharpCodeMapperContext context,
        ImmutableArray<CRecord> records,
        ImmutableHashSet<string> functionNames)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        foreach (var record in records)
        {
            var value = Struct(context, record, functionNames);
            if (value == null)
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpStruct? Struct(
        CSharpCodeMapperContext context,
        CRecord cRecord,
        ImmutableHashSet<string> functionNames)
    {
        var name = TypeNameCSharpRaw(cRecord.Name, 0);

        if (IsIgnored(name))
        {
            return null;
        }

        if (functionNames.Contains(cRecord.Name))
        {
            name = cRecord.Name + "_";
        }

        var (fields, nestedRecords) = StructFields(context, cRecord.Name, cRecord.Fields);
        var nestedStructs = Structs(context, nestedRecords, functionNames);
        var className = ClassName(name, out var nameCSharpMapped);
        var cSharpNameFinal = IdiomaticName(nameCSharpMapped, false);

        return new CSharpStruct(
            cSharpNameFinal,
            className,
            cRecord.Name,
            cRecord.SizeOf,
            cRecord.AlignOf,
            fields,
            nestedStructs);
    }

    private (ImmutableArray<CSharpStructField> Fields, ImmutableArray<CRecord> NestedRecords) StructFields(
        CSharpCodeMapperContext context,
        string structName,
        ImmutableArray<CRecordField> fields)
    {
        var resultFields = ImmutableArray.CreateBuilder<CSharpStructField>(fields.Length);
        var resultNestedRecords = ImmutableArray.CreateBuilder<CRecord>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var field in fields)
        {
            StructField(context, structName, field, resultFields);
        }

        var result = (resultFields.ToImmutable(), resultNestedRecords.ToImmutable());
        return result;
    }

    private void StructField(
        CSharpCodeMapperContext context,
        string structName,
        CRecordField field,
        ImmutableArray<CSharpStructField>.Builder resultFields)
    {
        if (string.IsNullOrEmpty(field.Name) && context.Records.TryGetValue(field.Type.Name, out var @struct))
        {
            var (value, _) = StructFields(context, structName, @struct.Fields);
            resultFields.AddRange(value);
        }
        else
        {
            var value = StructField(context, field);
            resultFields.Add(value);
        }
    }

    private CSharpStructField StructField(
        CSharpCodeMapperContext context,
        CRecordField cField)
    {
        var nameCSharp = SanitizeIdentifier(cField.Name);
        var nameCSharpFinal = IdiomaticName(nameCSharp, false);
        var typeC = cField.Type;
        var typeInfoCSharp = TypeCSharp(context, typeC);
        var offsetOf = cField.OffsetOf;
        var isWrapped = typeInfoCSharp.IsArray && !IsValidFixedBufferType(typeInfoCSharp.Name);

        var result = new CSharpStructField(
            nameCSharpFinal,
            typeInfoCSharp.ClassName,
            cField.Name,
            typeC.SizeOf,
            typeInfoCSharp,
            offsetOf,
            isWrapped);

        return result;
    }

    private ImmutableArray<CSharpOpaqueType> OpaqueStructs(
        CSharpCodeMapperContext context,
        ImmutableArray<COpaqueType> cOpaqueDataTypes)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(cOpaqueDataTypes.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var opaqueDataTypeC in cOpaqueDataTypes)
        {
            var opaqueStruct = OpaqueStruct(opaqueDataTypeC);
            if (opaqueStruct == null)
            {
                continue;
            }

            builder.Add(opaqueStruct);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpOpaqueType? OpaqueStruct(COpaqueType cOpaqueType)
    {
        var name = TypeNameCSharpRaw(cOpaqueType.Name, 0);
        if (IsIgnored(name))
        {
            return null;
        }

        var className = ClassName(
            name, out var nameCSharpMapped);
        var nameCSharpFinal = IdiomaticName(nameCSharpMapped, false);

        var opaqueTypeCSharp = new CSharpOpaqueType(
            nameCSharpFinal,
            className,
            cOpaqueType.Name);
        return opaqueTypeCSharp;
    }

    private ImmutableArray<CSharpAliasType> AliasStructs(
        CSharpCodeMapperContext context,
        ImmutableArray<CTypeAlias> typedefs)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpAliasType>(typedefs.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var typedef in typedefs)
        {
            var aliasStruct = AliasStruct(context, typedef);
            if (aliasStruct == null)
            {
                continue;
            }

            builder.Add(aliasStruct);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpAliasType? AliasStruct(
        CSharpCodeMapperContext context,
        CTypeAlias cTypeAlias)
    {
        if (IsIgnored(cTypeAlias.Name))
        {
            return null;
        }

        var underlyingTypeC = cTypeAlias.UnderlyingType;
        var underlyingTypeCSharp = TypeCSharp(context, underlyingTypeC);

        if (underlyingTypeCSharp.Name == cTypeAlias.Name)
        {
            return null;
        }

        var className = ClassName(
            cTypeAlias.Name,
            out var cSharpNameMapped);
        var cSharpNameFinal = IdiomaticName(cSharpNameMapped, false);

        var result = new CSharpAliasType(
            cSharpNameFinal,
            className,
            cTypeAlias.Name,
            underlyingTypeC.SizeOf ?? 0,
            underlyingTypeCSharp);

        return result;
    }

    private ImmutableArray<CSharpEnum> Enums(
        CSharpCodeMapperContext context,
        ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var value = Enum(context, enumC);
            if (IsIgnored(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpEnum Enum(
        CSharpCodeMapperContext context,
        CEnum cEnum)
    {
        var name = cEnum.Name;
        if (name.StartsWith("enum ", StringComparison.InvariantCulture))
        {
            name = name.ReplaceFirst("enum ", string.Empty, StringComparison.InvariantCulture);
        }

        var className = ClassName(name, out var cSharpNameMapped);
        var cSharpNameFinal = IdiomaticName(cSharpNameMapped, false);
        var values = EnumValues(context, cEnum.Values, cSharpNameFinal);

        var result = new CSharpEnum(
            cSharpNameFinal,
            className,
            cEnum.Name,
            cEnum.SizeOf,
            values);
        return result;
    }

    private ImmutableArray<CSharpEnumValue> EnumValues(
        CSharpCodeMapperContext context, ImmutableArray<CEnumValue> enumValues, string cSharpEnumName)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(enumValues.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumValue in enumValues)
        {
            var value = EnumValue(context, enumValue, cSharpEnumName);
            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnumValue EnumValue(
        CSharpCodeMapperContext context,
        CEnumValue cEnumValue,
        string cSharpEnumName)
    {
        var cSharpName = cEnumValue.Name;
        var value = cEnumValue.Value;
        var className = ClassName(
            cSharpName, out var cSharpNameName);
        var cSharpNameFinal = IdiomaticName(cSharpNameName, false, cSharpEnumName);

        var result = new CSharpEnumValue(
            cSharpNameFinal,
            className,
            cEnumValue.Name,
            null,
            value);

        return result;
    }

    private ImmutableArray<CSharpMacroObject> MacroObjects(
        CSharpCodeMapperContext context, ImmutableArray<CMacroObject> macroObjects)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpMacroObject>(macroObjects.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var macroObject in macroObjects)
        {
            if (_ignoredNames.Contains(macroObject.Name))
            {
                continue;
            }

            var value = MacroObject(context, macroObject);
            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpMacroObject MacroObject(
        CSharpCodeMapperContext context,
        CMacroObject cMacroObject)
    {
        var typeKind = cMacroObject.Type.NodeKind;
        var isConstant = typeKind is CNodeKind.Primitive;
        var typeSize = cMacroObject.Type.SizeOf;

        var typeName = TypeNameCSharp(context, cMacroObject.Type);
        if (typeName == "CString")
        {
            typeName = "string";
        }

        if (typeName == "CBool")
        {
            typeName = "int";
        }

        var value = cMacroObject.Value;
        if (typeName == "float")
        {
            value += "f";
        }

        var className = ClassName(cMacroObject.Name, out var cSharpNameMapped);
        var cSharpNameFinal = IdiomaticName(cSharpNameMapped, false, isMacroObject: true);

        var result = new CSharpMacroObject(
            cSharpNameFinal,
            className,
            cMacroObject.Name,
            typeSize,
            typeName,
            value,
            isConstant);
        return result;
    }

    private string ClassName(
        string typeName,
        out string mappedTypeName)
    {
        var nameSpace = string.Empty;
        mappedTypeName = typeName;

        foreach (var mappedName in _options.MappedCNamespaces)
        {
            if (typeName.StartsWith(mappedName.Source, StringComparison.InvariantCultureIgnoreCase) ||
                typeName.StartsWith("_" + mappedName.Source, StringComparison.InvariantCultureIgnoreCase))
            {
                nameSpace = mappedName.Target;
                var mappedTypeNameCandidate = typeName[mappedName.Source.Length..];

                var firstPointerIndex = mappedTypeNameCandidate.IndexOf('*', StringComparison.InvariantCulture);
                if (firstPointerIndex == -1)
                {
                    mappedTypeName = SanitizeIdentifier(mappedTypeNameCandidate);
                }
                else
                {
                    var identifierCandidate = mappedTypeNameCandidate[..firstPointerIndex];
                    var pointersTrailing = mappedTypeNameCandidate[firstPointerIndex..];
                    var identifier = SanitizeIdentifier(identifierCandidate);
                    mappedTypeName = identifier + pointersTrailing;
                }

                break;
            }
        }

        return nameSpace;
    }

    private CSharpType TypeCSharp(
        CSharpCodeMapperContext context,
        CType type)
    {
        var nameCSharp = TypeNameCSharp(context, type);
        var className = ClassName(nameCSharp, out var nameCSharpMapped);
        var nameCSharpFinal = IdiomaticName(nameCSharpMapped, true);

        var result = new CSharpType
        {
            Name = nameCSharpFinal,
            ClassName = className,
            OriginalName = type.Name,
            SizeOf = type.SizeOf ?? 0,
            AlignOf = type.AlignOf ?? 0,
            ArraySizeOf = type.ArraySizeOf
        };

        return result;
    }

    private string IdiomaticName(string name, bool isType, string enumName = "", bool isMacroObject = false, bool isParameter = false)
    {
        if (!_options.IsEnabledIdiomaticCSharp)
        {
            return name;
        }

        string identifier;
        var pointersTrailing = string.Empty;

        var firstPointerIndex = name.IndexOf('*', StringComparison.InvariantCulture);
        if (firstPointerIndex == -1)
        {
            identifier = name;
        }
        else
        {
            identifier = name[..firstPointerIndex];
            pointersTrailing = name[firstPointerIndex..];
        }

        if (!string.IsNullOrEmpty(enumName))
        {
            if (_options.IsEnabledEnumValueNamesUpperCase)
            {
                var enumNameUpperCase = enumName.ToUpperInvariant();
                var identifierUpperCase = identifier.ToUpperInvariant();
                if (identifierUpperCase.StartsWith(enumNameUpperCase, StringComparison.InvariantCulture) ||
                    identifierUpperCase.StartsWith("_" + enumNameUpperCase, StringComparison.InvariantCulture))
                {
                    identifier = identifier[(enumName.Length + 1)..];
                    identifier = char.ToUpper(identifier[0], CultureInfo.InvariantCulture) +
                                 identifier[1..].ToLowerInvariant();
                }
            }
            else
            {
                if (identifier.StartsWith(enumName, StringComparison.InvariantCulture) ||
                    identifier.StartsWith("_" + enumName, StringComparison.InvariantCulture))
                {
                    identifier = identifier[(enumName.Length + 1)..];
                }
            }
        }

        if (isType)
        {
            if (identifier
                is "bool"
                or "char"
                or "byte"
                or "sbyte"
                or "short"
                or "ushort"
                or "int"
                or "uint"
                or "long"
                or "ulong"
                or "float"
                or "decimal"
                or "void"
                or "nint"
                or "CBool"
                or "CString"
                or "CStringWide")
            {
                return identifier + pointersTrailing;
            }
        }

        if (string.IsNullOrEmpty(enumName) && identifier.StartsWith("FnPtr", StringComparison.InvariantCulture))
        {
            return identifier + pointersTrailing;
        }

        var parts = identifier.Split(IdentifierSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);

        var partsCapitalized = parts.Select(x =>
        {
            if (isMacroObject)
            {
                return char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..].ToLowerInvariant();
            }

            return char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..];
        }).ToArray();

        if (isParameter)
        {
            partsCapitalized[0] = char.ToLower(partsCapitalized[0][0], CultureInfo.InvariantCulture) +
                                  partsCapitalized[0][1..];
        }

        var identifierIdiomatic = string.Join(string.Empty, partsCapitalized);

        if (isParameter)
        {
            identifierIdiomatic = SanitizeIdentifier(identifierIdiomatic);
        }

        if (char.IsNumber(identifierIdiomatic[0]))
        {
            identifierIdiomatic = "_" + identifier;
        }

        return identifierIdiomatic + pointersTrailing;
    }

    private string TypeNameCSharp(
        CSharpCodeMapperContext context,
        CType type)
    {
        var forceUnsigned = type.NodeKind == CNodeKind.EnumValue;

        if (type.NodeKind == CNodeKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[type.Name];
            return TypeNameCSharpFunctionPointer(context, type.Name, functionPointer);
        }

        var typeName = type.Name;
        if (typeName.Contains("const ", StringComparison.InvariantCulture))
        {
            typeName = typeName.Replace("const ", string.Empty, StringComparison.InvariantCulture);
        }

        if (type.NodeKind is CNodeKind.Pointer or CNodeKind.Array)
        {
            typeName = TypeNameCSharpPointer(typeName, type.InnerType);
        }
        else
        {
            typeName = TypeNameCSharpRaw(typeName, type.SizeOf ?? 0, forceUnsigned);
        }

        // TODO: https://github.com/lithiumtoast/c2cs/issues/15
        if (typeName == "va_list")
        {
            typeName = "nint";
        }

        return typeName;
    }

    private string TypeNameCSharpFunctionPointer(
        CSharpCodeMapperContext context,
        string typeName,
        CFunctionPointer functionPointer)
    {
        if (functionPointer.Name != typeName)
        {
            return functionPointer.Name;
        }

        if (_generatedFunctionPointersNamesByCNames.TryGetValue(typeName, out var functionPointerName))
        {
            return functionPointerName;
        }

        functionPointerName = CreateFunctionPointerName(context, functionPointer);
        _generatedFunctionPointersNamesByCNames.Add(typeName, functionPointerName);
        return functionPointerName;
    }

    private string CreateFunctionPointerName(CSharpCodeMapperContext context, CFunctionPointer functionPointer)
    {
        var returnTypeC = functionPointer.ReturnType;
        var returnTypeCSharp = TypeCSharp(context, returnTypeC);
        var returnTypeNameCSharp = returnTypeCSharp.FullName.Replace("*", "Ptr", StringComparison.InvariantCulture);
        var returnTypeNameCSharpParts = returnTypeNameCSharp.Split(IdentifierSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);

        var returnTypeNameCSharpPartsCapitalized = returnTypeNameCSharpParts.Select(x =>
            char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..]);
        var returnTypeNameCSharpPartsJoined = string.Join(string.Empty, returnTypeNameCSharpPartsCapitalized);

        var parameterStringsCSharp = new List<string>();
        foreach (var parameter in functionPointer.Parameters)
        {
            var typeCSharp = TypeCSharp(context, parameter.Type);
            var typeNameCSharp = typeCSharp.FullName.Replace("*", "Ptr", StringComparison.InvariantCulture);
            var typeNameCSharpParts = typeNameCSharp.Split(IdentifierSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);

            var typeNameCSharpPartsCapitalized = typeNameCSharpParts.Select(x =>
                char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..]);
            var typeNameParameter = string.Join(string.Empty, typeNameCSharpPartsCapitalized);
            parameterStringsCSharp.Add(typeNameParameter);
        }

        var parameterStringsCSharpJoined = string.Join('_', parameterStringsCSharp);
        var functionPointerNameCSharp = $"FnPtr_{parameterStringsCSharpJoined}_{returnTypeNameCSharpPartsJoined}"
            .Replace("__", "_", StringComparison.InvariantCulture)
            .Replace(".", string.Empty, StringComparison.InvariantCulture);
        return functionPointerNameCSharp;
    }

    private string TypeNameCSharpPointer(string typeName, CType? innerType)
    {
        var pointerTypeName = typeName;

        // Replace [] with *
        while (true)
        {
            var x = pointerTypeName.IndexOf('[', StringComparison.InvariantCulture);

            if (x == -1)
            {
                break;
            }

            var y = pointerTypeName.IndexOf(']', x);

            pointerTypeName = pointerTypeName[..x] + "*" + pointerTypeName[(y + 1)..];
        }

        if (pointerTypeName.StartsWith("char *", StringComparison.InvariantCulture))
        {
            return pointerTypeName.ReplaceFirst("char *", "CString", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("wchar_t *", StringComparison.InvariantCulture))
        {
            return pointerTypeName.ReplaceFirst("wchar_t *", "CStringWide", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("FILE *", StringComparison.InvariantCulture))
        {
            return pointerTypeName.ReplaceFirst("FILE *", "nint", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("DIR *", StringComparison.InvariantCulture))
        {
            return pointerTypeName.ReplaceFirst("DIR *", "nint", StringComparison.InvariantCulture);
        }

        var elementTypeName = pointerTypeName.TrimEnd('*').TrimEnd();
        var pointersTypeName = pointerTypeName[elementTypeName.Length..]
            .Replace(" ", string.Empty, StringComparison.InvariantCulture);
        if (elementTypeName.Length == 0)
        {
            return "void" + pointersTypeName;
        }

        if (innerType == null)
        {
            return "void*";
        }

        var mappedElementTypeName = TypeNameCSharpRaw(elementTypeName, innerType.SizeOf ?? 0);
        var result = mappedElementTypeName + pointersTypeName;
        return result;
    }

    private string TypeNameCSharpRaw(string typeName, int? sizeOf = null, bool forceUnsignedInteger = false)
    {
        if (typeName.StartsWith("struct ", StringComparison.InvariantCulture))
        {
            typeName = typeName.ReplaceFirst("struct ", string.Empty, StringComparison.InvariantCulture);
        }

        if (typeName.StartsWith("union ", StringComparison.InvariantCulture))
        {
            typeName = typeName.ReplaceFirst("union ", string.Empty, StringComparison.InvariantCulture);
        }

        if (typeName.StartsWith("enum ", StringComparison.InvariantCulture))
        {
            typeName = typeName.ReplaceFirst("enum ", string.Empty, StringComparison.InvariantCulture);
        }

        if (_userTypeNameAliases.TryGetValue(typeName, out var aliasName))
        {
            return aliasName;
        }

        if (_options.SystemTypeAliases.TryGetValue(typeName, out var mappedSystemTypeName))
        {
            return mappedSystemTypeName;
        }

        switch (typeName)
        {
            case "char":
                return _options.IsEnabledLibraryImportAttribute ? "char" : "CChar";
            case "bool":
            case "_Bool":
                return _options.IsEnabledLibraryImportAttribute ? "bool" : "CBool";
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
                return TypeNameMapUnsignedInteger(sizeOf!.Value);

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
                if (forceUnsignedInteger)
                {
                    return TypeNameMapUnsignedInteger(sizeOf!.Value);
                }

                return TypeNameMapSignedInteger(sizeOf!.Value);

            case "float":
            case "double":
            case "long double":
                return TypeNameMapFloatingPoint(sizeOf!.Value);

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

    private string TypeNameMapFloatingPoint(int sizeOf)
    {
        return sizeOf switch
        {
            4 => "float",
            8 => "double",
            16 => "decimal",
            _ => throw new InvalidOperationException()
        };
    }

    private bool IsIgnored(string name)
    {
        return _builtinAliases.Contains(name) ||
               _ignoredNames.Contains(name);
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
