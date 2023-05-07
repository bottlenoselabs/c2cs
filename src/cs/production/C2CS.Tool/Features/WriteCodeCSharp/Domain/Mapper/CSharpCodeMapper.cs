// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Foundation;
using CAstFfi.Data;

namespace C2CS.Features.WriteCodeCSharp.Domain.Mapper;

public sealed class CSharpCodeMapper
{
    private readonly ImmutableHashSet<string> _builtinAliases;
    private readonly Dictionary<string, string> _generatedFunctionPointersNamesByCNames = new();
    private readonly ImmutableHashSet<string> _ignoredNames;
    private readonly CSharpCodeMapperOptions _options;
    private readonly ImmutableDictionary<string, string> _userTypeNameAliases;

    public CSharpCodeMapper(CSharpCodeMapperOptions options)
    {
        _options = options;

        var userAliases = new Dictionary<string, string>();
        var builtinAliases = new HashSet<string>();

        foreach (var typeAlias in options.TypeRenames)
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
            .Concat(new[] { "FFI_PLATFORM_NAME" })
            .ToImmutableHashSet();
    }

    public CSharpAbstractSyntaxTree Map(
        DiagnosticCollection diagnostics, CAbstractSyntaxTreeCrossPlatform astC)
    {
        var context = new CSharpCodeMapperContext(astC.Records, astC.FunctionPointers);
        var functionsC = astC.Functions.Values.ToImmutableArray();
        var functionNamesC = astC.Functions.Keys.ToImmutableHashSet();
        var functionPointersC = astC.FunctionPointers.Values.ToImmutableArray();
        var recordsC = astC.Records.Values.ToImmutableArray();
        var typeAliasesC = astC.TypeAliases.Values.ToImmutableArray();
        var opaqueTypesC = astC.OpaqueTypes.Values.ToImmutableArray();
        var enumsC = astC.Enums.Values.ToImmutableArray();
        var macroObjectsC = astC.MacroObjects.Values.ToImmutableArray();
        var enumConstantsC = astC.EnumConstants.Values.ToImmutableArray();

        var functions = Functions(context, functionsC);
        var structs = Structs(context, recordsC, functionNamesC);
        var aliasStructs = AliasStructs(context, typeAliasesC);
        var functionPointers = FunctionPointers(context, functionPointersC);
        var opaqueStructs = OpaqueStructs(context, opaqueTypesC);
        var enums = Enums(context, enumsC);
        var macroObjects = MacroObjects(context, macroObjectsC);
        var enumConstants = EnumConstants(context, enumConstantsC);

        var astCSharp = new CSharpAbstractSyntaxTree
        {
            Functions = functions,
            FunctionPointers = functionPointers,
            Structs = structs,
            AliasStructs = aliasStructs,
            OpaqueStructs = opaqueStructs,
            Enums = enums,
            MacroObjects = macroObjects,
            Constants = enumConstants
        };

        return astCSharp;
    }

    private ImmutableArray<CSharpFunction> Functions(
        CSharpCodeMapperContext context,
        ImmutableArray<CFunction> functions)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunction>(functions.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var function in functions)
        {
            var value = FunctionCSharp(context, function);
            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpFunction FunctionCSharp(CSharpCodeMapperContext context, CFunction cFunction)
    {
        var name = cFunction.Name;
        var returnTypeNameCSharp = TypeNameCSharp(context, cFunction.ReturnTypeInfo);
        var returnType = TypeCSharp(returnTypeNameCSharp, cFunction.ReturnTypeInfo);
        var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
        var parameters = CSharpFunctionParameters(context, name, cFunction.Parameters);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cFunction.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpFunction(
            name,
            null,
            callingConvention,
            returnType,
            parameters,
            attributes);

        return result;
    }

    private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
        CFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CFunctionCallingConvention.Cdecl => Features.WriteCodeCSharp.Data.CSharpFunctionCallingConvention.Cdecl,
            CFunctionCallingConvention.StdCall => Features.WriteCodeCSharp.Data.CSharpFunctionCallingConvention.StdCall,
            CFunctionCallingConvention.FastCall => Features.WriteCodeCSharp.Data.CSharpFunctionCallingConvention.FastCall,
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
        foreach (var functionExternParameterC in functionParameters)
        {
            var parameterName = CSharpUniqueParameterName(functionExternParameterC.Name, parameterNames);
            parameterNames.Add(parameterName);
            var value =
                FunctionParameter(context, functionName, functionExternParameterC, parameterName);
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
        CFunctionParameter functionParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var typeC = functionParameter.TypeInfo;
        var nameCSharp = TypeNameCSharp(context, typeC);
        var typeCSharp = TypeCSharp(nameCSharp, typeC);

        var typeCSharpName = typeCSharp.Name;
        var typeCSharpNameBase = typeCSharpName.TrimEnd('*');
        if (typeCSharpNameBase == functionName)
        {
            typeCSharpName = typeCSharpName.Replace(
                typeCSharpNameBase,
                typeCSharpNameBase + "_",
                StringComparison.InvariantCulture);
        }

        var attributes = ImmutableArray<Attribute>.Empty;

        var functionParameterCSharp = new CSharpFunctionParameter(
            name,
            typeC.SizeOf,
            typeCSharpName,
            attributes);

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
        var functionPointerType = cFunctionPointer.TypeInfo;
        var typeNameCSharp = TypeNameCSharpFunctionPointer(context, functionPointerType.Name, cFunctionPointer);
        if (names.ContainsKey(typeNameCSharp))
        {
            // This can happen if there is attributes on the function pointer return type or parameters.
            return null;
        }

        var returnTypeC = cFunctionPointer.ReturnTypeInfo;
        var returnTypeNameCSharp = TypeNameCSharp(context, returnTypeC);
        var returnTypeCSharp = TypeCSharp(returnTypeNameCSharp, returnTypeC);

        var parameters = FunctionPointerParameters(context, cFunctionPointer.Parameters);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cFunctionPointer.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpFunctionPointer(
            typeNameCSharp,
            functionPointerType.SizeOf,
            returnTypeCSharp,
            parameters,
            attributes);

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
        var typeC = cFunctionPointerParameter.TypeInfo;
        var nameCSharp = TypeNameCSharp(context, typeC);
        var typeCSharp = TypeCSharp(nameCSharp, typeC);
        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpParameter(
            name,
            typeC.SizeOf,
            typeCSharp,
            attributes);

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
            if (_builtinAliases.Contains(record.Name) ||
                _ignoredNames.Contains(record.Name))
            {
                // short circuit, prevents emitting the type
                continue;
            }

            var value = Struct(context, record, functionNames);
            if (_ignoredNames.Contains(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpStruct Struct(
        CSharpCodeMapperContext context,
        CRecord cRecord,
        ImmutableHashSet<string> functionNames)
    {
        var name = cRecord.Name;
        if (functionNames.Contains(cRecord.Name))
        {
            name = cRecord.Name + "_";
        }

        var (fields, nestedRecords) = StructFields(context, cRecord.Name, cRecord.Fields);
        var nestedStructs = Structs(context, nestedRecords, functionNames);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cRecord.Kind.ToString()
            }
        }.ToImmutableArray();

        return new CSharpStruct(
            name,
            cRecord.SizeOf,
            cRecord.AlignOf,
            fields,
            nestedStructs,
            attributes);
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
        if (string.IsNullOrEmpty(field.Name) && context.Records.TryGetValue(field.TypeInfo.Name, out var @struct))
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
        var name = SanitizeIdentifier(cField.Name);
        var typeC = cField.TypeInfo;

        CSharpTypeInfo typeInfoCSharp;
        if (typeC.Kind == CKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[typeC.Name];
            var functionPointerName = TypeNameCSharpFunctionPointer(context, typeC.Name, functionPointer);
            typeInfoCSharp = TypeCSharp(functionPointerName, typeC);
        }
        else
        {
            var nameCSharp = TypeNameCSharp(context, typeC);
            typeInfoCSharp = TypeCSharp(nameCSharp, typeC);
        }

        var offsetOf = cField.OffsetOf;
        var isWrapped = typeInfoCSharp.IsArray && !IsValidFixedBufferType(typeInfoCSharp.Name);
        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpStructField(
            name,
            typeC.SizeOf,
            typeInfoCSharp,
            offsetOf,
            isWrapped,
            attributes);

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
            var value = OpaqueDataStruct(opaqueDataTypeC);

            if (_ignoredNames.Contains(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpOpaqueType OpaqueDataStruct(COpaqueType cOpaqueType)
    {
        var nameCSharp = TypeNameCSharpRaw(cOpaqueType.Name, cOpaqueType.SizeOf);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cOpaqueType.Kind.ToString()
            }
        }.ToImmutableArray();

        var opaqueTypeCSharp = new CSharpOpaqueType(nameCSharp, attributes);
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
            if (_builtinAliases.Contains(typedef.Name) ||
                _ignoredNames.Contains(typedef.Name))
            {
                continue;
            }

            var value = AliasStruct(context, typedef);
            if (_ignoredNames.Contains(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpAliasType AliasStruct(
        CSharpCodeMapperContext context,
        CTypeAlias cTypeAlias)
    {
        var name = cTypeAlias.Name;
        var underlyingTypeC = cTypeAlias.UnderlyingTypeInfo;
        var underlyingNameCSharp = TypeNameCSharp(context, underlyingTypeC);
        var underlyingTypeCSharp = TypeCSharp(underlyingNameCSharp, underlyingTypeC);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cTypeAlias.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpAliasType(
            name,
            underlyingTypeC.SizeOf,
            underlyingTypeCSharp,
            attributes);

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
            if (_ignoredNames.Contains(value.Name))
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
        var integerTypeC = cEnum.IntegerTypeInfo;
        var integerNameCSharp = TypeNameCSharp(context, integerTypeC, true);
        var integerType = TypeCSharp(integerNameCSharp, integerTypeC);
        var values = EnumValues(context, cEnum.Values);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cEnum.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpEnum(
            name,
            integerType,
            values,
            attributes);
        return result;
    }

    private ImmutableArray<CSharpEnumValue> EnumValues(
        CSharpCodeMapperContext context, ImmutableArray<CEnumValue> enumValues)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(enumValues.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumValue in enumValues)
        {
            var value = EnumValue(context, enumValue);
            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnumValue EnumValue(
        CSharpCodeMapperContext context, CEnumValue cEnumValue)
    {
        var name = cEnumValue.Name;
        var value = cEnumValue.Value;
        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpEnumValue(
            name,
            null,
            value,
            attributes);

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
        var typeKind = cMacroObject.TypeInfo.Kind;
        var isConstant = typeKind is CKind.Primitive;
        var typeSize = cMacroObject.TypeInfo.SizeOf;

        var typeName = TypeNameCSharp(context, cMacroObject.TypeInfo);
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

        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cMacroObject.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpMacroObject(
            cMacroObject.Name,
            typeSize,
            typeName,
            value,
            isConstant,
            attributes);
        return result;
    }

    private ImmutableArray<CSharpConstant> EnumConstants(
        CSharpCodeMapperContext context,
        ImmutableArray<CEnumConstant> enumConstants)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpConstant>(enumConstants.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumConstant in enumConstants)
        {
            if (_ignoredNames.Contains(enumConstant.Name))
            {
                continue;
            }

            var value = EnumConstant(context, enumConstant);
            builder.Add(value);
        }

        builder.Sort();
        return builder.ToImmutable();
    }

    private CSharpConstant EnumConstant(CSharpCodeMapperContext context, CEnumConstant cEnumConstant)
    {
        var typeName = TypeNameCSharp(context, cEnumConstant.TypeInfo);
        var attributes = new Attribute[]
        {
            new CNodeAttribute
            {
                Kind = cEnumConstant.Kind.ToString()
            }
        }.ToImmutableArray();

        var result = new CSharpConstant(
            cEnumConstant.Name,
            cEnumConstant.TypeInfo.SizeOf,
            typeName,
            cEnumConstant.Value,
            attributes);
        return result;
    }

    private CSharpTypeInfo TypeCSharp(string nameCSharp, CTypeInfo typeInfo)
    {
        var attributesBuilder = ImmutableArray.CreateBuilder<Attribute>();
        if (typeInfo.IsConst)
        {
            var constAttribute = new CConstAttribute();
            attributesBuilder.Add(constAttribute);
        }

        var result = new CSharpTypeInfo
        {
            Name = nameCSharp,
            OriginalName = typeInfo.Name,
            SizeOf = typeInfo.SizeOf,
            AlignOf = typeInfo.AlignOf,
            ArraySizeOf = typeInfo.ArraySizeOf,
            Attributes = attributesBuilder.ToImmutable()
        };

        return result;
    }

    private string TypeNameCSharp(
        CSharpCodeMapperContext context,
        CTypeInfo typeInfo,
        bool forceUnsigned = false)
    {
        if (typeInfo.Kind == CKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[typeInfo.Name];
            return TypeNameCSharpFunctionPointer(context, typeInfo.Name, functionPointer);
        }

        var typeName = typeInfo.Name;
        if (typeName.Contains("const ", StringComparison.InvariantCulture))
        {
            typeName = typeName.Replace("const ", string.Empty, StringComparison.InvariantCulture);
        }

        if (typeName.Contains("*const", StringComparison.InvariantCulture))
        {
            typeName = typeName.Replace("*const", "*", StringComparison.InvariantCulture);
        }

        if (typeInfo.Kind is CKind.Pointer or CKind.Array)
        {
            typeName = TypeNameCSharpPointer(typeName, typeInfo.InnerTypeInfo);
        }
        else
        {
            typeName = TypeNameCSharpRaw(typeName, typeInfo.SizeOf, forceUnsigned);
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
        var returnTypeC = functionPointer.ReturnTypeInfo;
        var returnTypeNameCSharpOriginal = TypeNameCSharp(context, returnTypeC);
        var returnTypeNameCSharp = returnTypeNameCSharpOriginal.Replace("*", "Ptr", StringComparison.InvariantCulture);
        var returnTypeStringCapitalized = char.ToUpper(returnTypeNameCSharp[0], CultureInfo.InvariantCulture) +
                                          returnTypeNameCSharp.Substring(1);

        var parameterStringsCSharp = new List<string>();
        foreach (var parameter in functionPointer.Parameters)
        {
            var nameCSharp = TypeNameCSharp(context, parameter.TypeInfo);
            var typeCSharp = TypeCSharp(nameCSharp, parameter.TypeInfo);
            var typeNameCSharpOriginal = typeCSharp.Name;
            var typeNameCSharp = typeNameCSharpOriginal.Replace("*", "Ptr", StringComparison.InvariantCulture);
            var typeNameCSharpCapitalized =
                char.ToUpper(typeNameCSharp[0], CultureInfo.InvariantCulture) + typeNameCSharp[1..];
            parameterStringsCSharp.Add(typeNameCSharpCapitalized);
        }

        var parameterStringsCSharpJoined = string.Join('_', parameterStringsCSharp);
        var functionPointerNameCSharp = $"FnPtr_{parameterStringsCSharpJoined}_{returnTypeStringCapitalized}"
            .Replace("__", "_", StringComparison.InvariantCulture);
        return functionPointerNameCSharp;
    }

    private string TypeNameCSharpPointer(string typeName, CTypeInfo? innerTypeInfo)
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

        if (pointerTypeName.StartsWith("char*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("char*", "CString", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("wchar_t*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("wchar_t*", "CStringWide", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("FILE*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("FILE*", "nint", StringComparison.InvariantCulture);
        }

        if (pointerTypeName.StartsWith("DIR*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("DIR*", "nint", StringComparison.InvariantCulture);
        }

        var elementTypeName = pointerTypeName.TrimEnd('*');
        var pointersTypeName = pointerTypeName[elementTypeName.Length..];
        if (elementTypeName.Length == 0)
        {
            return "void" + pointersTypeName;
        }

        if (innerTypeInfo == null)
        {
            return "void*";
        }

        var mappedElementTypeName = TypeNameCSharpRaw(elementTypeName, innerTypeInfo.SizeOf);
        var result = mappedElementTypeName + pointersTypeName;
        return result;
    }

    private string TypeNameCSharpRaw(string typeName, int sizeOf, bool forceUnsignedInteger = false)
    {
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
                return forceUnsignedInteger ? TypeNameMapUnsignedInteger(sizeOf) : TypeNameMapSignedInteger(sizeOf);

            case "float":
            case "double":
            case "long double":
                return TypeNameMapFloatingPoint(sizeOf);

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
