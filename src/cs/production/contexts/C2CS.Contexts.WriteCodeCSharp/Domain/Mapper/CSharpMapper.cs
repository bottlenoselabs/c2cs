// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Contexts.WriteCodeCSharp.Domain.Mapper.Diagnostics;
using C2CS.Foundation.UseCases.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.Mapper;

public sealed class CSharpMapper
{
    private readonly CSharpMapperParameters _parameters;

    private readonly ImmutableHashSet<string> _builtinAliases;
    private readonly Dictionary<string, string> _generatedFunctionPointersNamesByCNames = new();
    private readonly ImmutableHashSet<string> _ignoredNames;
    private readonly ImmutableDictionary<string, string> _userTypeNameAliases;

    public CSharpMapper(CSharpMapperParameters parameters)
    {
        _parameters = parameters;

        var userAliases = new Dictionary<string, string>();
        var builtinAliases = new HashSet<string>();

        foreach (var typeAlias in parameters.TypeAliases)
        {
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
        _ignoredNames = parameters.IgnoredTypeNames
            .Concat(parameters.SystemTypeNameAliases.Keys)
            .ToImmutableHashSet();
    }

    public ImmutableDictionary<TargetPlatform, CSharpNodes> Map(
        ImmutableArray<CAbstractSyntaxTree> abstractSyntaxTrees)
    {
        var builder = ImmutableDictionary.CreateBuilder<TargetPlatform, CSharpNodes>();

        foreach (var ast in abstractSyntaxTrees)
        {
            var platformNodes = CSharpNodes(ast);
            builder.Add(ast.PlatformRequested, platformNodes);
        }

        return builder.ToImmutable();
    }

    private CSharpNodes CSharpNodes(CAbstractSyntaxTree ast)
    {
        var context = new CSharpMapperContext(ast.PlatformRequested, ast.Records, ast.FunctionPointers);
        var functionsC = ast.Functions.Values.ToImmutableArray();
        var functionNamesC = ast.Functions.Keys.ToImmutableHashSet();
        var functionPointersC = ast.FunctionPointers.Values.ToImmutableArray();
        var recordsC = ast.Records.Values.ToImmutableArray();
        var typeAliasesC = ast.TypeAliases.Values.ToImmutableArray();
        var opaqueTypesC = ast.OpaqueTypes.Values.ToImmutableArray();
        var enumsC = ast.Enums.Values.ToImmutableArray();
        var constantsC = ast.MacroObjects.Values.ToImmutableArray();

        var functions = Functions(context, functionsC);
        var structs = Structs(context, recordsC, functionNamesC);
        // Typedefs need to be processed first as they can generate aliases on the fly
        var aliasStructs = AliasStructs(context, typeAliasesC);
        var functionPointers = FunctionPointers(context, functionPointersC);
        var opaqueDataTypes = OpaqueDataTypes(context, opaqueTypesC);
        var enums = Enums(context, enumsC);
        var constants = Constants(context, constantsC);

        var nodes = new CSharpNodes
        {
            Functions = functions,
            FunctionPointers = functionPointers,
            Structs = structs,
            AliasStructs = aliasStructs,
            OpaqueStructs = opaqueDataTypes,
            Enums = enums,
            Constants = constants
        };
        return nodes;
    }

    private ImmutableArray<CSharpFunction> Functions(
        CSharpMapperContext context,
        ImmutableArray<CFunction> clangFunctionExterns)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunction>(clangFunctionExterns.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        for (var index = 0; index < clangFunctionExterns.Length; index++)
        {
            var clangFunctionExtern = clangFunctionExterns[index];
            var functionExtern = FunctionCSharp(context, clangFunctionExtern);
            builder.Add(functionExtern);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunction FunctionCSharp(CSharpMapperContext context, CFunction cFunction)
    {
        var name = cFunction.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(cFunction);
        var returnTypeNameCSharp = TypeNameCSharp(context, cFunction.ReturnTypeInfo);
        var returnType = TypeCSharp(returnTypeNameCSharp, cFunction.ReturnTypeInfo);
        var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
        var parameters = CSharpFunctionParameters(context, name, cFunction.Parameters);

        var result = new CSharpFunction(
            context.Platform,
            name,
            originalCodeLocationComment,
            null,
            callingConvention,
            returnType,
            parameters);

        return result;
    }

    private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
        CFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CFunctionCallingConvention.Cdecl => Data.Model.CSharpFunctionCallingConvention.Cdecl,
            CFunctionCallingConvention.StdCall => Data.Model.CSharpFunctionCallingConvention.StdCall,
            CFunctionCallingConvention.FastCall => Data.Model.CSharpFunctionCallingConvention.FastCall,
            _ => throw new ArgumentOutOfRangeException(
                nameof(callingConvention), callingConvention, null)
        };

        return result;
    }

    private ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
        CSharpMapperContext context,
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
            var functionParameterCSharp =
                FunctionParameter(context, functionName, functionExternParameterC, parameterName);
            builder.Add(functionParameterCSharp);
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
        CSharpMapperContext context,
        string functionName,
        CFunctionParameter functionParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionParameter);
        var typeC = functionParameter.TypeInfo;
        var nameCSharp = TypeNameCSharp(context, typeC);
        var typeCSharp = TypeCSharp(nameCSharp, typeC);

        var typeCSharpName = typeCSharp.Name;
        var typeCSharpNameBase = typeCSharpName.TrimEnd('*');
        if (typeCSharpNameBase == functionName)
        {
            typeCSharpName = typeCSharpName.Replace(typeCSharpNameBase, typeCSharpNameBase + "_");
        }

        var functionParameterCSharp = new CSharpFunctionParameter(
            context.Platform,
            name,
            originalCodeLocationComment,
            typeC.SizeOf,
            typeCSharpName);

        return functionParameterCSharp;
    }

    private ImmutableArray<CSharpFunctionPointer> FunctionPointers(
        CSharpMapperContext context,
        ImmutableArray<CFunctionPointer> functionPointers)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);
        var names = new Dictionary<string, CFunctionPointer>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointer in functionPointers)
        {
            var functionPointerCSharp = FunctionPointer(context, names, functionPointer);
            if (functionPointerCSharp == null)
            {
                continue;
            }

            builder.Add(functionPointerCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointer? FunctionPointer(
        CSharpMapperContext context,
        Dictionary<string, CFunctionPointer> names,
        CFunctionPointer functionPointer)
    {
        var functionPointerType = functionPointer.TypeInfo;
        var typeNameCSharp = TypeNameCSharpFunctionPointer(context, functionPointerType.Name, functionPointer);
        if (names.ContainsKey(typeNameCSharp))
        {
            // This can happen if there is attributes on the function pointer return type or parameters.
            return null;
        }

        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointer);
        var returnTypeC = functionPointer.ReturnTypeInfo;
        var returnTypeNameCSharp = TypeNameCSharp(context, returnTypeC);
        var returnTypeCSharp = TypeCSharp(returnTypeNameCSharp, returnTypeC);

        var parameters = FunctionPointerParameters(context, functionPointer.Parameters);

        var result = new CSharpFunctionPointer(
            context.Platform,
            typeNameCSharp,
            originalCodeLocationComment,
            functionPointerType.SizeOf,
            returnTypeCSharp,
            parameters);

        names.Add(typeNameCSharp, functionPointer);

        return result;
    }

    private ImmutableArray<CSharpFunctionPointerParameter> FunctionPointerParameters(
        CSharpMapperContext context,
        ImmutableArray<CFunctionPointerParameter> functionPointerParameters)
    {
        var builder =
            ImmutableArray.CreateBuilder<CSharpFunctionPointerParameter>(functionPointerParameters.Length);
        var parameterNames = new List<string>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointerParameterC in functionPointerParameters)
        {
            var parameterName = CSharpUniqueParameterName(functionPointerParameterC.Name, parameterNames);
            parameterNames.Add(parameterName);
            var functionExternParameterCSharp =
                FunctionPointerParameter(context, functionPointerParameterC, parameterName);
            builder.Add(functionExternParameterCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointerParameter FunctionPointerParameter(
        CSharpMapperContext context,
        CFunctionPointerParameter functionPointerParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointerParameter);
        var typeC = functionPointerParameter.TypeInfo;
        var nameCSharp = TypeNameCSharp(context, typeC);
        var typeCSharp = TypeCSharp(nameCSharp, typeC);

        var result = new CSharpFunctionPointerParameter(
            context.Platform,
            name,
            originalCodeLocationComment,
            typeC.SizeOf,
            typeCSharp);

        return result;
    }

    private ImmutableArray<CSharpStruct> Structs(
        CSharpMapperContext context,
        ImmutableArray<CRecord> records,
        ImmutableHashSet<string> functionNames)
    {
        var results = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        foreach (var record in records)
        {
            if (_builtinAliases.Contains(record.Name) ||
                _ignoredNames.Contains(record.Name))
            {
                // short circuit, prevents emitting the type
                continue;
            }

            var item = Struct(context, record, functionNames);
            if (_ignoredNames.Contains(item.Name))
            {
                continue;
            }

            results.Add(item);
        }

        return results.ToImmutable();
    }

    private CSharpStruct Struct(
        CSharpMapperContext context,
        CRecord record,
        ImmutableHashSet<string> functionNames)
    {
        var originalCodeLocationComment = OriginalCodeLocationComment(record);
        var (fields, nestedRecords) = StructFields(context, record.Name, record.Fields);
        var nestedStructs = Structs(context, nestedRecords, functionNames);

        var name = record.Name;
        if (functionNames.Contains(record.Name))
        {
            name = record.Name + "_";
        }

        return new CSharpStruct(
            name,
            context.Platform,
            originalCodeLocationComment,
            record.SizeOf,
            record.AlignOf,
            fields,
            nestedStructs);
    }

    private (ImmutableArray<CSharpStructField> Fields, ImmutableArray<CRecord> NestedRecords) StructFields(
        CSharpMapperContext context,
        string structName,
        ImmutableArray<CRecordField> fields)
    {
        var resultFields = ImmutableArray.CreateBuilder<CSharpStructField>(fields.Length);
        var resultNestedRecords = ImmutableArray.CreateBuilder<CRecord>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var field in fields)
        {
            StructField(context, structName, field, resultFields, resultNestedRecords);
        }

        var result = (resultFields.ToImmutable(), resultNestedRecords.ToImmutable());
        return result;
    }

    private void StructField(
        CSharpMapperContext context,
        string structName,
        CRecordField field,
        ImmutableArray<CSharpStructField>.Builder resultFields,
        ImmutableArray<CRecord>.Builder resultNestedRecords)
    {
        if (string.IsNullOrEmpty(field.Name) && context.Records.TryGetValue(field.TypeInfo.Name, out var @struct))
        {
            var (nestedFields, _) = StructFields(context, structName, @struct.Fields);
            resultFields.AddRange(nestedFields);
        }
        else
        {
            var structFieldCSharp = StructField(context, field);
            if (context.Records.TryGetValue(field.TypeInfo.Name, out var record))
            {
                resultNestedRecords.Add(record);
            }

            resultFields.Add(structFieldCSharp);
        }
    }

    private CSharpStructField StructField(
        CSharpMapperContext context,
        CRecordField field)
    {
        var name = SanitizeIdentifier(field.Name);
        var codeLocationComment = OriginalCodeLocationComment(field);
        var typeC = field.TypeInfo;

        CSharpType typeCSharp;
        if (typeC.Kind == CKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[typeC.Name];
            var functionPointerName = TypeNameCSharpFunctionPointer(context, typeC.Name, functionPointer);
            typeCSharp = TypeCSharp(functionPointerName, typeC);
        }
        else
        {
            var nameCSharp = TypeNameCSharp(context, typeC);
            typeCSharp = TypeCSharp(nameCSharp, typeC);
        }

        var offset = field.OffsetOf;
        var padding = field.PaddingOf;
        var isWrapped = typeCSharp.IsArray && !IsValidFixedBufferType(typeCSharp.Name);

        var result = new CSharpStructField(
            context.Platform,
            name,
            codeLocationComment,
            typeC.SizeOf,
            typeCSharp,
            offset ?? 0,
            padding ?? 0,
            isWrapped);

        return result;
    }

    private ImmutableArray<CSharpOpaqueStruct> OpaqueDataTypes(
        CSharpMapperContext context,
        ImmutableArray<COpaqueType> opaqueDataTypes)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>(opaqueDataTypes.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var opaqueDataTypeC in opaqueDataTypes)
        {
            var opaqueDataTypeCSharp = OpaqueDataStruct(context, opaqueDataTypeC);

            if (_ignoredNames.Contains(opaqueDataTypeCSharp.Name))
            {
                continue;
            }

            builder.Add(opaqueDataTypeCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpOpaqueStruct OpaqueDataStruct(
        CSharpMapperContext context,
        COpaqueType opaqueType)
    {
        var nameCSharp = TypeNameCSharpRaw(opaqueType.Name, opaqueType.SizeOf);
        var originalCodeLocationComment = OriginalCodeLocationComment(opaqueType);

        var opaqueTypeCSharp = new CSharpOpaqueStruct(
            context.Platform,
            nameCSharp,
            originalCodeLocationComment,
            opaqueType.SizeOf);

        return opaqueTypeCSharp;
    }

    private ImmutableArray<CSharpAliasStruct> AliasStructs(
        CSharpMapperContext context,
        ImmutableArray<CTypeAlias> typedefs)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpAliasStruct>(typedefs.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var typedef in typedefs)
        {
            if (_builtinAliases.Contains(typedef.Name) ||
                _ignoredNames.Contains(typedef.Name))
            {
                continue;
            }

            var aliasStruct = AliasStruct(context, typedef);
            if (_ignoredNames.Contains(aliasStruct.Name))
            {
                continue;
            }

            builder.Add(aliasStruct);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpAliasStruct AliasStruct(
        CSharpMapperContext context,
        CTypeAlias typeAlias)
    {
        var name = typeAlias.Name;

        var originalCodeLocationComment = OriginalCodeLocationComment(typeAlias);
        var underlyingTypeC = typeAlias.UnderlyingTypeInfo;
        var underlyingNameCSharp = TypeNameCSharp(context, underlyingTypeC);
        var underlyingTypeCSharp = TypeCSharp(underlyingNameCSharp, underlyingTypeC);

        var result = new CSharpAliasStruct(
            context.Platform,
            name,
            originalCodeLocationComment,
            underlyingTypeC.SizeOf,
            underlyingTypeCSharp);

        return result;
    }

    private ImmutableArray<CSharpEnum> Enums(
        CSharpMapperContext context,
        ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var enumCSharp = Enum(context, enumC);

            if (_ignoredNames.Contains(enumCSharp.Name))
            {
                continue;
            }

            builder.Add(enumCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnum Enum(
        CSharpMapperContext context,
        CEnum @enum)
    {
        var name = @enum.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(@enum);
        var integerTypeC = @enum.IntegerTypeInfo;
        var integerNameCSharp = TypeNameCSharp(context, integerTypeC);
        var integerType = TypeCSharp(integerNameCSharp, integerTypeC);
        var values = EnumValues(context, @enum.Values);

        var result = new CSharpEnum(
            context.Platform,
            name,
            originalCodeLocationComment,
            integerType,
            values);
        return result;
    }

    private ImmutableArray<CSharpEnumValue> EnumValues(
        CSharpMapperContext context, ImmutableArray<CEnumValue> enumValues)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(enumValues.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumValue in enumValues)
        {
            var @enum = EnumValue(context, enumValue);
            builder.Add(@enum);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnumValue EnumValue(
        CSharpMapperContext context, CEnumValue enumValue)
    {
        var name = enumValue.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(enumValue);
        var value = enumValue.Value;

        var result = new CSharpEnumValue(
            context.Platform,
            name,
            originalCodeLocationComment,
            null,
            value);

        return result;
    }

    private ImmutableArray<CSharpConstant> Constants(
        CSharpMapperContext context, ImmutableArray<CMacroObject> constants)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpConstant>(constants.Length);

        var lookup = new Dictionary<string, CSharpConstant>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var macroObject in constants)
        {
            if (_ignoredNames.Contains(macroObject.Name))
            {
                continue;
            }

            var constant = Constant(context, macroObject, lookup);
            if (constant == null)
            {
                var diagnostic = new TranspileMacroObjectFailureDiagnostic(macroObject.Name, macroObject.Location);
                _parameters.DiagnosticsSink.Add(diagnostic);
            }
            else
            {
                builder.Add(constant);
                lookup.Add(constant.Name, constant);
            }
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpConstant? Constant(
        CSharpMapperContext context,
        CMacroObject macro,
        Dictionary<string, CSharpConstant> lookup)
    {
        var originalCodeLocationComment = OriginalCodeLocationComment(macro);
        var tokens = macro.Tokens.ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            foreach (var keyValuePair in _parameters.SystemTypeNameAliases)
            {
                var typeName = keyValuePair.Key;
                var typeNameAlias = keyValuePair.Value;
                if (token == typeName)
                {
                    token = tokens[i] = typeNameAlias;
                }
            }

            foreach (var keyValuePair in _userTypeNameAliases)
            {
                var typeName = keyValuePair.Key;
                var typeNameAlias = keyValuePair.Value;
                if (token == typeName)
                {
                    token = tokens[i] = typeNameAlias;
                }
            }

            if (token == "size_t")
            {
                token = tokens[i] = "ulong";
            }

            if (token.ToUpper(CultureInfo.InvariantCulture).EndsWith("ULL", StringComparison.InvariantCulture))
            {
                var possibleIntegerToken = token.Substring(0, token.Length - 3);

                if (possibleIntegerToken.StartsWith("0x", StringComparison.InvariantCulture))
                {
                    possibleIntegerToken = possibleIntegerToken.Substring(2);
                    if (ulong.TryParse(
                            possibleIntegerToken,
                            NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture,
                            out _))
                    {
                        token = tokens[i] = $"0x{possibleIntegerToken}UL";
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    if (ulong.TryParse(possibleIntegerToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                    {
                        token = tokens[i] = $"{possibleIntegerToken}UL";
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        var typeValue = GetMacroExpressionTypeAndValue(tokens.ToImmutableArray(), lookup);
        if (typeValue == null)
        {
            return null;
        }

        var (type, value) = typeValue.Value;
        if (type == "?")
        {
            return null;
        }

        var result = new CSharpConstant(
            context.Platform,
            macro.Name,
            originalCodeLocationComment,
            null,
            type,
            value);
        return result;
    }

    private readonly string _dotNetPath = DotNetPath();

    private (string Type, string Value)? GetMacroExpressionTypeAndValue(
        ImmutableArray<string> tokens, IReadOnlyDictionary<string, CSharpConstant> lookup)
    {
        var dependentMacros = new List<string>();
        foreach (var token in tokens)
        {
            if (lookup.TryGetValue(token, out var dependentMacro))
            {
                dependentMacros.Add($"var {dependentMacro.Name} = {dependentMacro.Value};");
            }
        }

        var value = string.Join(string.Empty, tokens);
        var code = @$"
using System;
{string.Join("\n", dependentMacros)}
var x = {value};
".Trim();

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var variableDeclarations = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<VariableDeclarationSyntax>();
        var variables = variableDeclarations.Last().Variables;
        if (variables.Count > 1)
        {
            // something is wrong with the macro; it's probably not an object-like macro
            return null;
        }

        var variable = variables.Single();
        var variableInitializer = variable.Initializer;
        if (variableInitializer == null)
        {
            return null;
        }

        var expression = variableInitializer.Value;
        var mscorlib = MetadataReference.CreateFromFile(Path.Combine(_dotNetPath, "mscorlib.dll"));
        var privateCoreLib =
            MetadataReference.CreateFromFile(Path.Combine(_dotNetPath, "System.Private.CoreLib.dll"));
        var compilation = CSharpCompilation.Create("Assembly")
            .AddReferences(mscorlib, privateCoreLib)
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var typeInfo = semanticModel.GetTypeInfo(expression);

        if (typeInfo.ConvertedType == null)
        {
            return null;
        }

        var type = typeInfo.ConvertedType!.ToString()!;

        if (value.StartsWith("(uint)-", StringComparison.InvariantCulture) ||
            value.StartsWith("(ulong)-", StringComparison.InvariantCulture))
        {
            value = $"unchecked({value})";
        }

        return (type, value);
    }

    private CSharpType TypeCSharp(string nameCSharp, CTypeInfo typeInfo)
    {
        var result = new CSharpType
        {
            Name = nameCSharp,
            OriginalName = typeInfo.Name,
            SizeOf = typeInfo.SizeOf,
            AlignOf = typeInfo.AlignOf,
            ArraySizeOf = typeInfo.ArraySizeOf
        };

        return result;
    }

    private string TypeNameCSharp(
        CSharpMapperContext context,
        CTypeInfo typeInfo)
    {
        if (typeInfo!.Kind == CKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[typeInfo.Name];
            return TypeNameCSharpFunctionPointer(context, typeInfo.Name, functionPointer);
        }

        string result;

        if (typeInfo.Kind is CKind.Pointer or CKind.Array)
        {
            result = TypeNameCSharpPointer(typeInfo.Name, typeInfo.InnerType!);
        }
        else
        {
            result = TypeNameCSharpRaw(typeInfo.Name, typeInfo.SizeOf);
        }

        // TODO: https://github.com/lithiumtoast/c2cs/issues/15
        if (result == "va_list")
        {
            result = "nint";
        }

        return result;
    }

    private string TypeNameCSharpFunctionPointer(
        CSharpMapperContext context,
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

    private string CreateFunctionPointerName(CSharpMapperContext context, CFunctionPointer functionPointer)
    {
        var returnTypeC = functionPointer.ReturnTypeInfo;
        var returnTypeNameCSharpOriginal = TypeNameCSharp(context, returnTypeC);
        var returnTypeNameCSharp = returnTypeNameCSharpOriginal.Replace("*", "Ptr");
        var returnTypeStringCapitalized = char.ToUpper(returnTypeNameCSharp[0], CultureInfo.InvariantCulture) +
                                          returnTypeNameCSharp.Substring(1);

        var parameterStringsCSharp = new List<string>();
        foreach (var parameter in functionPointer.Parameters)
        {
            var nameCSharp = TypeNameCSharp(context, parameter.TypeInfo);
            var typeCSharp = TypeCSharp(nameCSharp, parameter.TypeInfo);
            var typeNameCSharpOriginal = typeCSharp.Name;
            var typeNameCSharp = typeNameCSharpOriginal.Replace("*", "Ptr");
            var typeNameCSharpCapitalized =
                char.ToUpper(typeNameCSharp[0], CultureInfo.InvariantCulture) + typeNameCSharp.Substring(1);
            parameterStringsCSharp.Add(typeNameCSharpCapitalized);
        }

        var parameterStringsCSharpJoined = string.Join("_", parameterStringsCSharp);
        var functionPointerNameCSharp = $"FnPtr_{parameterStringsCSharpJoined}_{returnTypeStringCapitalized}"
            .Replace("__", "_");
        return functionPointerNameCSharp;
    }

    private string TypeNameCSharpPointer(string typeName, CTypeInfo innerTypeInfo)
    {
        var pointerTypeName = typeName;

        // Replace [] with *
        while (true)
        {
            var x = pointerTypeName.IndexOf('[');

            if (x == -1)
            {
                break;
            }

            var y = pointerTypeName.IndexOf(']', x);

            var a = pointerTypeName.Substring(0, x) + "*";
            var b = pointerTypeName.Substring(y + 1, pointerTypeName.Length);
            pointerTypeName = a + b;
        }

        if (pointerTypeName.StartsWith("char*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("char*", "CString");
        }

        if (pointerTypeName.StartsWith("wchar_t*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("wchar_t*", "CStringWide");
        }

        if (pointerTypeName.StartsWith("FILE*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("FILE*", "nint");
        }

        if (pointerTypeName.StartsWith("DIR*", StringComparison.InvariantCulture))
        {
            return pointerTypeName.Replace("DIR*", "nint");
        }

        var elementTypeName = pointerTypeName.TrimEnd('*');
        var pointersTypeName = pointerTypeName.Substring(elementTypeName.Length);
        if (elementTypeName.Length == 0)
        {
            return "void" + pointersTypeName;
        }

        var mappedElementTypeName = TypeNameCSharpRaw(elementTypeName, innerTypeInfo!.SizeOf);
        var result = mappedElementTypeName + pointersTypeName;
        return result;
    }

    private string TypeNameCSharpRaw(string typeName, int sizeOf)
    {
        if (_userTypeNameAliases.TryGetValue(typeName, out var aliasName))
        {
            return aliasName;
        }

        if (_parameters.SystemTypeNameAliases.TryGetValue(typeName, out var mappedSystemTypeName))
        {
            return mappedSystemTypeName;
        }

        switch (typeName)
        {
            case "char":
                return "CChar";
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

    private static string OriginalCodeLocationComment(CNode node)
    {
        var kindString = node switch
        {
            CRecord record => record.RecordKind.ToString(),
            _ => node.Kind.ToString()
        };

        if (node is not CNodeWithLocation nodeWithLocation)
        {
            return $"// {kindString}";
        }

        var location = nodeWithLocation.Location;
        return $"// {kindString} @ " + location;
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

    private static string DotNetPath()
    {
        Version version = new(0, 0, 0, 0);
        var path = string.Empty;
        var shellOutput = "dotnet --list-runtimes".ExecuteShell();
        var runtimesString = shellOutput.Output;
        var runtimeStrings =
            runtimesString.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var fullRuntimeString in runtimeStrings)
        {
            var parse = fullRuntimeString.Split(new[] {" [" }, StringSplitOptions.RemoveEmptyEntries);
            var runtimeString = parse[0];
            var runtimeStringParse = runtimeString.Split(new[] {' ' }, StringSplitOptions.RemoveEmptyEntries);
            var runtimeName = runtimeStringParse[0];
            var runtimeVersionString = runtimeStringParse[1];
            var runtimePath = parse[1].Trim(']');

            if (!runtimeName.Contains("Microsoft.NETCore.App"))
            {
                continue;
            }

            var versionCharIndexHyphen = runtimeVersionString.IndexOf('-');
            if (versionCharIndexHyphen != -1)
            {
                // can possibly happen for release candidates of .NET
                runtimeVersionString = runtimeVersionString.Substring(0, versionCharIndexHyphen);
            }

            var candidateVersion = Version.Parse(runtimeVersionString);
            if (candidateVersion <= version)
            {
                continue;
            }

            version = candidateVersion;
            path = Path.Combine(runtimePath, runtimeVersionString);
        }

        return path;
    }
}
