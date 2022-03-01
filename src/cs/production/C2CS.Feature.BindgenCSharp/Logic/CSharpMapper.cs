// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using C2CS.Feature.BindgenCSharp.Data;
using C2CS.Feature.BindgenCSharp.Diagnostics;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Feature.BindgenCSharp.Logic;

public class CSharpMapper
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
                or "CChar"
                or "CCharWide")
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

    public CSharpAbstractSyntaxTree AbstractSyntaxTree(CAbstractSyntaxTree abstractSyntaxTree)
    {
        var types = abstractSyntaxTree.Types.ToImmutableDictionary(x => x.Name);

        var functionExterns = Functions(
            types,
            abstractSyntaxTree.Functions);

        var recordsBuilder = ImmutableArray.CreateBuilder<CRecord>();
        foreach (var record in abstractSyntaxTree.Records)
        {
            if (_builtinAliases.Contains(record.Name) ||
                _ignoredNames.Contains(record.Name))
            {
                // short circuit, prevents emitting the type
                continue;
            }

            recordsBuilder.Add(record);
        }

        var structs = Structs(types, recordsBuilder.ToImmutable());

        var typedefsBuilder = ImmutableArray.CreateBuilder<CTypedef>();
        foreach (var typedef in abstractSyntaxTree.Typedefs)
        {
            if (_builtinAliases.Contains(typedef.Name) ||
                _ignoredNames.Contains(typedef.Name))
            {
                // short circuit, prevents emitting the type
                continue;
            }

            typedefsBuilder.Add(typedef);
        }

        // Typedefs need to be processed first as they can generate aliases on the fly
        var typedefs = Typedefs(types, typedefsBuilder.ToImmutable());
        var functionPointers = FunctionPointers(types, abstractSyntaxTree.FunctionPointers);
        var opaqueDataTypes = OpaqueDataTypes(types, abstractSyntaxTree.OpaqueTypes);
        var enums = Enums(types, abstractSyntaxTree.Enums);
        var pseudoEnums = PseudoEnums(types, abstractSyntaxTree.PseudoEnums);
        var constants = Constants(abstractSyntaxTree.Constants);

        var result = new CSharpAbstractSyntaxTree
        {
            FunctionExterns = functionExterns,
            FunctionPointers = functionPointers,
            Structs = structs,
            Typedefs = typedefs,
            OpaqueDataTypes = opaqueDataTypes,
            Enums = enums,
            PseudoEnums = pseudoEnums,
            Constants = constants
        };

        return result;
    }

    private ImmutableArray<CSharpFunction> Functions(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CFunction> clangFunctionExterns)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunction>(clangFunctionExterns.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var clangFunctionExtern in clangFunctionExterns)
        {
            var functionExtern = Function(types, clangFunctionExtern);
            builder.Add(functionExtern);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunction Function(ImmutableDictionary<string, CType> types, CFunction cFunction)
    {
        var name = cFunction.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(cFunction);

        var cType = CType(types, cFunction.ReturnType);

        var returnType = Type(types, cType);
        var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
        var parameters = CSharpFunctionParameters(types, cFunction.Parameters);

        var result = new CSharpFunction(
            name,
            originalCodeLocationComment,
            callingConvention,
            returnType,
            parameters);

        return result;
    }

    private static CType CType(ImmutableDictionary<string, CType> types, string typeName)
    {
        if (types.TryGetValue(typeName, out var type))
        {
            return type;
        }

        var up = new UseCaseException($"Expected a type with the name '{typeName}' but it was not found.");
        throw up;
    }

    private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
        CFunctionCallingConvention cFunctionCallingConvention)
    {
        var result = cFunctionCallingConvention switch
        {
            CFunctionCallingConvention.Cdecl => Data.CSharpFunctionCallingConvention.Cdecl,
            CFunctionCallingConvention.StdCall => Data.CSharpFunctionCallingConvention.StdCall,
            _ => throw new ArgumentOutOfRangeException(
                nameof(cFunctionCallingConvention), cFunctionCallingConvention, null)
        };

        return result;
    }

    private ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CFunctionParameter> functionExternParameters)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionParameter>(functionExternParameters.Length);
        var parameterNames = new List<string>();

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionExternParameterC in functionExternParameters)
        {
            var parameterName = CSharpUniqueParameterName(functionExternParameterC.Name, parameterNames);
            parameterNames.Add(parameterName);
            var functionExternParameterCSharp =
                FunctionParameter(types, functionExternParameterC, parameterName);
            builder.Add(functionExternParameterCSharp);
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
        ImmutableDictionary<string, CType> types,
        CFunctionParameter functionParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionParameter);
        var typeC = CType(types, functionParameter.Type);
        var typeCSharp = Type(types, typeC);

        var functionParameterCSharp = new CSharpFunctionParameter(
            name,
            originalCodeLocationComment,
            typeCSharp);

        return functionParameterCSharp;
    }

    private ImmutableArray<CSharpFunctionPointer> FunctionPointers(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CFunctionPointer> functionPointers)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointerC in functionPointers)
        {
            var functionPointerCSharp = FunctionPointer(types, functionPointerC);
            builder.Add(functionPointerCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointer FunctionPointer(
        ImmutableDictionary<string, CType> types,
        CFunctionPointer functionPointerC)
    {
        var typeName = string.IsNullOrEmpty(functionPointerC.Name) ? functionPointerC.Type : functionPointerC.Name;
        var typeC = CType(types, typeName);
        var typeNameCSharp = TypeNameMapFunctionPointer(types, typeC);

        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointerC);
        var returnTypeC = CType(types, functionPointerC.ReturnType);
        var returnTypeCSharp = Type(types, returnTypeC);
        var parameters = FunctionPointerParameters(
            types,
            functionPointerC.Parameters);

        var result = new CSharpFunctionPointer(
            typeNameCSharp,
            originalCodeLocationComment,
            returnTypeCSharp,
            parameters);

        return result;
    }

    private ImmutableArray<CSharpFunctionPointerParameter> FunctionPointerParameters(
        ImmutableDictionary<string, CType> types,
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
                FunctionPointerParameter(types, functionPointerParameterC, parameterName);
            builder.Add(functionExternParameterCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointerParameter FunctionPointerParameter(
        ImmutableDictionary<string, CType> types,
        CFunctionPointerParameter functionPointerParameterC,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointerParameterC);
        var typeC = CType(types, functionPointerParameterC.Type);
        var typeCSharp = Type(types, typeC);

        var result = new CSharpFunctionPointerParameter(
            name,
            originalCodeLocationComment,
            typeCSharp);

        return result;
    }

    private ImmutableArray<CSharpStruct> Structs(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CRecord> records)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordC in records)
        {
            var structCSharp = Struct(types, recordC);

            if (_ignoredNames.Contains(structCSharp.Name))
            {
                continue;
            }

            builder.Add(structCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpStruct Struct(
        ImmutableDictionary<string, CType> types,
        CRecord recordC)
    {
        var originalCodeLocationComment = OriginalCodeLocationComment(recordC);
        var typeC = CType(types, recordC.Name);
        var typeCSharp = Type(types, typeC);
        var fields = StructFields(types, recordC.Fields);
        var nestedStructs = NestedStructs(types, recordC.NestedRecords);

        return new CSharpStruct(
            originalCodeLocationComment,
            typeCSharp,
            fields,
            nestedStructs);
    }

    private ImmutableArray<CSharpStructField> StructFields(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CRecordField> recordFields)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStructField>(recordFields.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordFieldC in recordFields)
        {
            var structFieldCSharp = StructField(types, recordFieldC);
            builder.Add(structFieldCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpStructField StructField(
        ImmutableDictionary<string, CType> types,
        CRecordField recordFieldC)
    {
        var name = SanitizeIdentifier(recordFieldC.Name);
        var codeLocationComment = OriginalCodeLocationComment(recordFieldC);
        var typeC = CType(types, recordFieldC.Type);

        CSharpType typeCSharp;
        if (typeC.Kind == CKind.FunctionPointer)
        {
            var functionPointerName = TypeNameMapFunctionPointer(types, typeC);
            typeCSharp = Type(types, typeC, functionPointerName);
        }
        else
        {
            typeCSharp = Type(types, typeC);
        }

        var offset = recordFieldC.Offset;
        var padding = recordFieldC.Padding;
        var isWrapped = typeCSharp.IsArray && !IsValidFixedBufferType(typeCSharp.Name ?? string.Empty);

        var result = new CSharpStructField(
            name,
            codeLocationComment,
            typeCSharp,
            offset,
            padding,
            isWrapped);

        return result;
    }

    private ImmutableArray<CSharpStruct> NestedStructs(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CRecord> records)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordC in records)
        {
            var structCSharp = Struct(types, recordC);

            if (_ignoredNames.Contains(structCSharp.Name))
            {
                continue;
            }

            builder.Add(structCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private ImmutableArray<CSharpOpaqueType> OpaqueDataTypes(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<COpaqueType> opaqueDataTypes)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(opaqueDataTypes.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var opaqueDataTypeC in opaqueDataTypes)
        {
            var opaqueDataTypeCSharp = OpaqueDataType(types, opaqueDataTypeC);

            if (_ignoredNames.Contains(opaqueDataTypeCSharp.Name))
            {
                continue;
            }

            builder.Add(opaqueDataTypeCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpOpaqueType OpaqueDataType(
        ImmutableDictionary<string, CType> types,
        COpaqueType opaqueTypeC)
    {
        var typeC = CType(types, opaqueTypeC.Name);
        var typeCSharp = Type(types, typeC);
        var name = typeCSharp.Name!;
        var originalCodeLocationComment = OriginalCodeLocationComment(opaqueTypeC);

        var opaqueTypeCSharp = new CSharpOpaqueType(
            name,
            originalCodeLocationComment);

        return opaqueTypeCSharp;
    }

    private ImmutableArray<CSharpTypedef> Typedefs(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CTypedef> typedefs)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTypedef>(typedefs.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var typedefC in typedefs)
        {
            var typedefCSharp = Typedef(types, typedefC);
            if (typedefCSharp == null)
            {
                continue;
            }

            if (_ignoredNames.Contains(typedefCSharp.Name))
            {
                continue;
            }

            builder.Add(typedefCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpTypedef Typedef(
        ImmutableDictionary<string, CType> types,
        CTypedef typedefC)
    {
        var name = typedefC.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(typedefC);
        var underlyingTypeC = CType(types, typedefC.UnderlyingType);
        var typeC = CType(types, typedefC.Name);
        if (typeC.IsSystem && underlyingTypeC.IsSystem)
        {
            var diagnostic = new SystemTypedefDiagnostic(name, typedefC.Location, underlyingTypeC.Name);
            _parameters.DiagnosticsSink.Add(diagnostic);
        }

        var underlyingTypeCSharp = Type(types, underlyingTypeC);

        var result = new CSharpTypedef(
            name,
            originalCodeLocationComment,
            underlyingTypeCSharp);

        return result;
    }

    private ImmutableArray<CSharpEnum> Enums(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var enumCSharp = Enum(types, enumC);

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
        ImmutableDictionary<string, CType> types,
        CEnum cEnum)
    {
        var name = cEnum.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(cEnum);
        var cIntegerType = CType(types, cEnum.IntegerType);
        var integerType = Type(types, cIntegerType);
        var values = EnumValues(cEnum.Values);

        var result = new CSharpEnum(
            name,
            originalCodeLocationComment,
            integerType,
            values);
        return result;
    }

    private ImmutableArray<CSharpPseudoEnum> PseudoEnums(
        ImmutableDictionary<string, CType> types,
        ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpPseudoEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var enumCSharp = PseudoEnum(types, enumC);

            if (_ignoredNames.Contains(enumCSharp.Name))
            {
                continue;
            }

            builder.Add(enumCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpPseudoEnum PseudoEnum(
        ImmutableDictionary<string, CType> types,
        CEnum @enum)
    {
        var name = @enum.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(@enum);
        originalCodeLocationComment =
            originalCodeLocationComment.Replace(
                "Enum ", $"Pseudo enum '{@enum.Name}' ", StringComparison.InvariantCulture);
        var cIntegerType = CType(types, @enum.IntegerType);
        var integerType = Type(types, cIntegerType);
        var values = EnumValues(@enum.Values);

        var result = new CSharpPseudoEnum(
            name,
            originalCodeLocationComment,
            integerType,
            values);
        return result;
    }

    private ImmutableArray<CSharpEnumValue> EnumValues(ImmutableArray<CEnumValue> enumValues)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnumValue>(enumValues.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var cEnumValue in enumValues)
        {
            var @enum = EnumValue(cEnumValue);
            builder.Add(@enum);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnumValue EnumValue(CEnumValue enumValue)
    {
        var name = enumValue.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(enumValue);
        var value = enumValue.Value;

        var result = new CSharpEnumValue(
            name,
            originalCodeLocationComment,
            value);

        return result;
    }

    private ImmutableArray<CSharpConstant> Constants(ImmutableArray<CMacroDefinition> constants)
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

            var constant = Constant(macroObject, lookup);
            if (constant == null)
            {
                if (macroObject.Name.EndsWith("API_DECL", StringComparison.InvariantCulture))
                {
                    // Silently swallow
                    continue;
                }

                var diagnostic = new MacroObjectNotTranspiledDiagnostic(macroObject.Name, macroObject.Location);
                _parameters.DiagnosticsSink.Add(diagnostic);
            }
            else if (lookup.ContainsKey(constant.Name))
            {
                var diagnostic = new MacroObjectAlreadyExistsDiagnostic(macroObject.Name, macroObject.Location);
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

    private CSharpConstant? Constant(CMacroDefinition macroDefinition, Dictionary<string, CSharpConstant> lookup)
    {
        var originalCodeLocationComment = OriginalCodeLocationComment(macroDefinition);
        var tokens = macroDefinition.Tokens.ToArray();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];

            foreach (var (typeName, typeNameAlias) in _parameters.SystemTypeNameAliases)
            {
                if (token == typeName)
                {
                    token = tokens[i] = typeNameAlias;
                }
            }

            foreach (var (typeName, typeNameAlias) in _userTypeNameAliases)
            {
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
                var possibleIntegerToken = token[..^3];

                if (possibleIntegerToken.StartsWith("0x", StringComparison.InvariantCulture))
                {
                    possibleIntegerToken = possibleIntegerToken[2..];
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
            macroDefinition.Name,
            originalCodeLocationComment,
            type,
            value);
        return result;
    }

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

        var dotNetPath = Terminal.DotNetPath();
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
        var mscorlib = MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "mscorlib.dll"));
        var privateCoreLib =
            MetadataReference.CreateFromFile(Path.Combine(dotNetPath, "System.Private.CoreLib.dll"));
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

    private CSharpType Type(
        ImmutableDictionary<string, CType> types,
        CType cType,
        string? typeName = null)
    {
        var typeName2 = typeName ?? TypeName(types, cType);
        var sizeOf = cType.SizeOf;
        var alignOf = cType.AlignOf ?? 0;
        var fixedBufferSize = cType.ArraySize ?? 0;

        var result = new CSharpType
        {
            Name = typeName2,
            OriginalName = cType.Name,
            SizeOf = sizeOf,
            AlignOf = alignOf,
            ArraySize = fixedBufferSize
        };

        return result;
    }

    private string TypeName(
        ImmutableDictionary<string, CType> types,
        CType type)
    {
        if (type.Kind == CKind.FunctionPointer)
        {
            return TypeNameMapFunctionPointer(types, type);
        }

        var name = type.Name;
        var elementTypeSize = type.ElementSize ?? type.SizeOf;
        string typeName;

        if (name.EndsWith("*", StringComparison.InvariantCulture) ||
            name.EndsWith("]", StringComparison.InvariantCulture))
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
            typeName = "nint";
        }

        return typeName;
    }

    private string TypeNameMapFunctionPointer(
        ImmutableDictionary<string, CType> types,
        CType typeC)
    {
        if (typeC.Kind == CKind.Typedef)
        {
            return typeC.Name;
        }

        if (typeC.Kind != CKind.FunctionPointer)
        {
            var up = new UseCaseException($"Expected type to be function pointer but type is '{typeC.Kind}'.");
            throw up;
        }

        if (_generatedFunctionPointersNamesByCNames.TryGetValue(typeC.Name, out var functionPointerNameCSharp))
        {
            return functionPointerNameCSharp;
        }

        var indexOfFirstParentheses = typeC.Name.IndexOf('(', StringComparison.InvariantCulture);
        var returnTypeStringC = typeC.Name.Substring(0, indexOfFirstParentheses)
            .Replace(" *", "*", StringComparison.InvariantCulture).Trim();
        var returnTypeC = CType(types, returnTypeStringC);
        var returnTypeCSharp = Type(types, returnTypeC);
        var returnTypeNameCSharpOriginal = returnTypeCSharp.Name ?? string.Empty;
        var returnTypeNameCSharp = returnTypeNameCSharpOriginal.Replace("*", "Ptr", StringComparison.InvariantCulture);
        var returnTypeStringCapitalized = char.ToUpper(returnTypeNameCSharp[0], CultureInfo.InvariantCulture) +
                                          returnTypeNameCSharp.Substring(1);

        var parameterStringsCSharp = new List<string>();
        var parameterStringsC = typeC.Name.Substring(indexOfFirstParentheses)
            .Trim('(', ')').Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Replace(" *", "*", StringComparison.InvariantCulture))
            .Select(x => x.Trim()).ToArray();
        foreach (var typeNameC in parameterStringsC)
        {
            var parameterTypeC = CType(types, typeNameC);
            var parameterTypeCSharp = Type(types, parameterTypeC);

            if (parameterTypeC.Name == "void" && parameterTypeC.IsSystem)
            {
                continue;
            }

            var typeNameCSharpOriginal = parameterTypeCSharp.Name ?? string.Empty;
            var typeNameCSharp = typeNameCSharpOriginal.Replace("*", "Ptr", StringComparison.InvariantCulture);
            var typeNameCSharpCapitalized =
                char.ToUpper(typeNameCSharp[0], CultureInfo.InvariantCulture) + typeNameCSharp[1..];
            parameterStringsCSharp.Add(typeNameCSharpCapitalized);
        }

        var parameterStringsCSharpJoined = string.Join('_', parameterStringsCSharp);
        functionPointerNameCSharp =
            $"FnPtr_{parameterStringsCSharpJoined}_{returnTypeStringCapitalized}"
                .Replace("__", "_", StringComparison.InvariantCulture);
        _generatedFunctionPointersNamesByCNames.Add(typeC.Name, functionPointerNameCSharp);

        return functionPointerNameCSharp;
    }

    private string TypeNameMapPointer(CType type, int sizeOf, bool isSystem)
    {
        var pointerTypeName = type.Name;

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
        var mappedElementTypeName = TypeNameMapElement(elementTypeName, sizeOf, isSystem);
        pointerTypeName = mappedElementTypeName + pointersTypeName;

        return pointerTypeName;
    }

    private string TypeNameMapElement(string typeName, int sizeOf, bool isSystem)
    {
        if (!isSystem)
        {
            if (_userTypeNameAliases.TryGetValue(typeName, out var aliasName))
            {
                return aliasName;
            }

            return typeName;
        }

        if (_parameters.SystemTypeNameAliases.TryGetValue(typeName, out var mappedSystemTypeName))
        {
            return mappedSystemTypeName;
        }

        switch (typeName)
        {
            case "char":
                return "CChar";
            case "wchar_t":
                return "CWideChar";
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
        if (location.IsBuiltin)
        {
            result = $"// {kindString} @ Builtin";
        }
        else
        {
            result = $"// {kindString} @ " + location;
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
