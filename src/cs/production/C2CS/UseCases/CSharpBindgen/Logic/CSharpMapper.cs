// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using C2CS.UseCases.CExtractAbstractSyntaxTree;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.UseCases.CSharpBindgen;

public class CSharpMapper
{
    private readonly DiagnosticsSink _diagnostics;
    private readonly string _className;
    private ImmutableDictionary<string, CType> _types = null!;
    private readonly ImmutableDictionary<string, string> _userTypeNameAliases;
    private readonly ImmutableHashSet<string> _builtinAliases;
    private readonly ImmutableHashSet<string> _ignoredNames;
    private readonly Dictionary<string, string> _generatedFunctionPointersNamesByCNames = new();
    private readonly Dictionary<string, string> _systemTypeNameAliases;
    private string _dotNetPath = string.Empty;

    private static Dictionary<string, string> SystemTypeNameAliases(int bitness)
    {
        var aliases = new Dictionary<string, string>();

        if (Runtime.OperatingSystem == RuntimeOperatingSystem.Windows)
        {
            aliases.Add("DWORD", "uint");
            aliases.Add("ULONG", "uint");
            aliases.Add("UINT_PTR", "UIntPtr");
            aliases.Add("INT_PTR", "IntPtr");
            aliases.Add("HANDLE", "nint");
            aliases.Add("SOCKET", "nint");
        }

        if (Runtime.OperatingSystem == RuntimeOperatingSystem.Linux)
        {
            aliases.Add("__gid_t", "uint");
            aliases.Add("__uid_t", "uint");
            aliases.Add("__pid_t", "int");
            aliases.Add("__socklen_t", "uint");

            if (bitness == 32)
            {
                aliases.Add("__time_t", "int");
            }
            else if (bitness == 64)
            {
                aliases.Add("__time_t", "long");
            }
            else
            {
                throw new NotImplementedException($"{bitness}-bit is not implemented.");
            }
        }

        if (Runtime.OperatingSystem == RuntimeOperatingSystem.macOS ||
            Runtime.OperatingSystem == RuntimeOperatingSystem.iOS ||
            Runtime.OperatingSystem == RuntimeOperatingSystem.tvOS)
        {
            aliases.Add("__uint32_t", "uint");
            aliases.Add("__uint16_t", "ushort");
            aliases.Add("__uint8_t", "byte");
            aliases.Add("__int32_t", "int");

            aliases.Add("__darwin_pthread_t", "nint");
            aliases.Add("__darwin_uid_t", "uint");
            aliases.Add("__darwin_pid_t", "int");
            aliases.Add("__darwin_gid_t", "uint");
            aliases.Add("__darwin_socklen_t", "uint");
            aliases.Add("_opaque_pthread_t", string.Empty); // remove
            aliases.Add("__darwin_pthread_handler_rec", string.Empty); // remove

            if (bitness == 32)
            {
                aliases.Add("__darwin_time_t", "int");
            }
            else if (bitness == 64)
            {
                aliases.Add("__darwin_time_t", "long");
            }
            else
            {
                throw new NotImplementedException($"{bitness}-bit is not implemented.");
            }
        }

        return aliases;
    }

    public CSharpMapper(
        string className,
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        int bitness,
        DiagnosticsSink diagnostics)
    {
        _diagnostics = diagnostics;
        _className = className;
        _systemTypeNameAliases = SystemTypeNameAliases(bitness);

        var userAliases = new Dictionary<string, string>();
        var builtinAliases = new HashSet<string>();

        foreach (var typeAlias in typeAliases)
        {
            userAliases.Add(typeAlias.From, typeAlias.To);

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

        _userTypeNameAliases = userAliases.ToImmutableDictionary();
        _builtinAliases = builtinAliases.ToImmutableHashSet();
        _ignoredNames = ignoredTypeNames
            .Concat(_systemTypeNameAliases.Keys)
            .ToImmutableHashSet();
    }

    public CSharpAbstractSyntaxTree AbstractSyntaxTree(CAbstractSyntaxTree abstractSyntaxTree)
    {
        _types = abstractSyntaxTree.Types.ToImmutableDictionary(x => x.Name);

        var functionExterns = Functions(
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

        var structs = Structs(recordsBuilder.ToImmutable());

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
        var typedefs = Typedefs(typedefsBuilder.ToImmutable());

        var functionPointers = FunctionPointers(
            abstractSyntaxTree.FunctionPointers);
        var opaqueDataTypes = OpaqueDataTypes(
            abstractSyntaxTree.OpaqueTypes);
        var enums = Enums(abstractSyntaxTree.Enums);
        var pseudoEnums = PseudoEnums(abstractSyntaxTree.PseudoEnums);
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

        var up = new CSharpMapperException($"Expected a type with the name '{typeName}' but it was not found.");
        throw up;
    }

    private static CSharpFunctionCallingConvention CSharpFunctionCallingConvention(
        CFunctionCallingConvention cFunctionCallingConvention)
    {
        var result = cFunctionCallingConvention switch
        {
            CFunctionCallingConvention.C => CSharpBindgen.CSharpFunctionCallingConvention.Cdecl,
            _ => throw new ArgumentOutOfRangeException(
                nameof(cFunctionCallingConvention), cFunctionCallingConvention, null)
        };

        return result;
    }

    private ImmutableArray<CSharpFunctionParameter> CSharpFunctionParameters(
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
                FunctionParameter(functionExternParameterC, parameterName);
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
        CFunctionParameter functionParameter, string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionParameter);
        var typeC = CType(functionParameter.Type);
        var typeCSharp = Type(typeC);

        var functionParameterCSharp = new CSharpFunctionParameter(
            name,
            originalCodeLocationComment,
            typeCSharp);

        return functionParameterCSharp;
    }

    private ImmutableArray<CSharpFunctionPointer> FunctionPointers(
        ImmutableArray<CFunctionPointer> functionPointers)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpFunctionPointer>(functionPointers.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var functionPointerC in functionPointers)
        {
            var functionPointerCSharp = FunctionPointer(functionPointerC);
            builder.Add(functionPointerCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointer FunctionPointer(CFunctionPointer functionPointerC)
    {
        var typeName = string.IsNullOrEmpty(functionPointerC.Name) ? functionPointerC.Type : functionPointerC.Name;
        var typeC = CType(typeName);
        var typeNameCSharp = TypeNameMapFunctionPointer(typeC);

        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointerC);
        var returnTypeC = CType(functionPointerC.ReturnType);
        var returnTypeCSharp = Type(returnTypeC);
        var parameters = FunctionPointerParameters(functionPointerC.Parameters);

        var result = new CSharpFunctionPointer(
            typeNameCSharp,
            originalCodeLocationComment,
            returnTypeCSharp,
            parameters);

        return result;
    }

    private ImmutableArray<CSharpFunctionPointerParameter> FunctionPointerParameters(
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
                FunctionPointerParameter(functionPointerParameterC, parameterName);
            builder.Add(functionExternParameterCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointerParameter FunctionPointerParameter(
        CFunctionPointerParameter functionPointerParameterC, string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var originalCodeLocationComment = OriginalCodeLocationComment(functionPointerParameterC);
        var typeC = CType(functionPointerParameterC.Type);
        var typeCSharp = Type(typeC);

        var result = new CSharpFunctionPointerParameter(
            name,
            originalCodeLocationComment,
            typeCSharp);

        return result;
    }

    private ImmutableArray<CSharpStruct> Structs(ImmutableArray<CRecord> records)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordC in records)
        {
            var structCSharp = Struct(recordC);

            if (_ignoredNames.Contains(structCSharp.Name))
            {
                continue;
            }

            builder.Add(structCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpStruct Struct(CRecord recordC)
    {
        var originalCodeLocationComment = OriginalCodeLocationComment(recordC);
        var typeC = CType(recordC.Name);
        var typeCSharp = Type(typeC);
        var fields = StructFields(recordC.Fields);
        var nestedStructs = NestedStructs(recordC.NestedRecords);

        return new CSharpStruct(
            originalCodeLocationComment,
            typeCSharp,
            fields,
            nestedStructs);
    }

    private ImmutableArray<CSharpStructField> StructFields(
        ImmutableArray<CRecordField> recordFields)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStructField>(recordFields.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordFieldC in recordFields)
        {
            var structFieldCSharp = StructField(recordFieldC);
            builder.Add(structFieldCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpStructField StructField(CRecordField recordFieldC)
    {
        var name = SanitizeIdentifier(recordFieldC.Name);
        var codeLocationComment = OriginalCodeLocationComment(recordFieldC);
        var typeC = CType(recordFieldC.Type);

        CSharpType typeCSharp;
        if (typeC.Kind == CKind.FunctionPointer)
        {
            var functionPointerName = TypeNameMapFunctionPointer(typeC);
            typeCSharp = Type(typeC, functionPointerName);
        }
        else
        {
            typeCSharp = Type(typeC);
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

    private ImmutableArray<CSharpStruct> NestedStructs(ImmutableArray<CRecord> records)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var recordC in records)
        {
            var structCSharp = Struct(recordC);

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
        ImmutableArray<COpaqueType> opaqueDataTypes)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpOpaqueType>(opaqueDataTypes.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var opaqueDataTypeC in opaqueDataTypes)
        {
            var opaqueDataTypeCSharp = OpaqueDataType(opaqueDataTypeC);

            if (_ignoredNames.Contains(opaqueDataTypeCSharp.Name))
            {
                continue;
            }

            builder.Add(opaqueDataTypeCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpOpaqueType OpaqueDataType(COpaqueType opaqueTypeC)
    {
        var name = opaqueTypeC.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(opaqueTypeC);

        var opaqueTypeCSharp = new CSharpOpaqueType(
            name,
            originalCodeLocationComment);

        return opaqueTypeCSharp;
    }

    private ImmutableArray<CSharpTypedef> Typedefs(ImmutableArray<CTypedef> typedefs)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTypedef>(typedefs.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var typedefC in typedefs)
        {
            var typedefCSharp = Typedef(typedefC);
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

    private CSharpTypedef? Typedef(CTypedef typedefC)
    {
        var name = typedefC.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(typedefC);
        var underlyingTypeC = CType(typedefC.UnderlyingType);
        var typeC = CType(typedefC.Name);
        if (typeC.IsSystem && underlyingTypeC.IsSystem)
        {
            var diagnostic = new DiagnosticSystemTypedef(name, typedefC.Location, underlyingTypeC.Name);
            _diagnostics.Add(diagnostic);
        }

        var underlyingTypeCSharp = Type(underlyingTypeC);

        var result = new CSharpTypedef(
            name,
            originalCodeLocationComment,
            underlyingTypeCSharp);

        return result;
    }

    private ImmutableArray<CSharpEnum> Enums(ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var enumCSharp = Enum(enumC);

            if (_ignoredNames.Contains(enumCSharp.Name))
            {
                continue;
            }

            builder.Add(enumCSharp);
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

    private ImmutableArray<CSharpPseudoEnum> PseudoEnums(ImmutableArray<CEnum> enums)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpPseudoEnum>(enums.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var enumC in enums)
        {
            var enumCSharp = PseudoEnum(enumC);

            if (_ignoredNames.Contains(enumCSharp.Name))
            {
                continue;
            }

            builder.Add(enumCSharp);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpPseudoEnum PseudoEnum(CEnum @enum)
    {
        var name = @enum.Name;
        var originalCodeLocationComment = OriginalCodeLocationComment(@enum);
        originalCodeLocationComment =
            originalCodeLocationComment.Replace("Enum ", $"Pseudo enum '{@enum.Name}' ", StringComparison.InvariantCulture);
        var cIntegerType = CType(@enum.IntegerType);
        var integerType = Type(cIntegerType);
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
                var diagnostic = new DiagnosticMacroObjectNotTranspiled(macroObject.Name, macroObject.Location);
                _diagnostics.Add(diagnostic);
            }
            else if (lookup.ContainsKey(constant.Name))
            {
                var diagnostic = new DiagnosticMacroObjectAlreadyExists(macroObject.Name, macroObject.Location);
                _diagnostics.Add(diagnostic);
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

            foreach (var (typeName, typeNameAlias) in _systemTypeNameAliases)
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

        if (string.IsNullOrEmpty(_dotNetPath))
        {
            _dotNetPath = Terminal.DotNetPath();
            if (!Directory.Exists(_dotNetPath))
            {
                return null;
            }
        }

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
        var privatecorelib =
            MetadataReference.CreateFromFile(Path.Combine(_dotNetPath, "System.Private.CoreLib.dll"));
        var compilation = CSharpCompilation.Create("Assembly")
            .AddReferences(mscorlib, privatecorelib)
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

    private CSharpType Type(CType cType, string? typeName = null)
    {
        var typeName2 = typeName ?? TypeName(cType);
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

    private string TypeName(CType type)
    {
        if (type.Kind == CKind.FunctionPointer)
        {
            return TypeNameMapFunctionPointer(type);
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

    private string TypeNameMapFunctionPointer(CType typeC)
    {
        if (typeC.Kind == CKind.Typedef)
        {
            return typeC.Name;
        }

        if (typeC.Kind != CKind.FunctionPointer)
        {
            var up = new CSharpMapperException($"Expected type to be function pointer but type is '{typeC.Kind}'.");
            throw up;
        }

        if (_generatedFunctionPointersNamesByCNames.TryGetValue(typeC.Name, out var functionPointerNameCSharp))
        {
            return functionPointerNameCSharp;
        }

        var indexOfFirstParentheses = typeC.Name.IndexOf('(', StringComparison.InvariantCulture);
        var returnTypeStringC = typeC.Name.Substring(0, indexOfFirstParentheses).Replace(" *", "*", StringComparison.InvariantCulture).Trim();
        var returnTypeC = CType(returnTypeStringC);
        var returnTypeCSharp = Type(returnTypeC);
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
            var parameterTypeC = CType(typeNameC);
            var parameterTypeCSharp = Type(parameterTypeC);

            if (parameterTypeC.Name == "void" && parameterTypeC.IsSystem)
            {
                continue;
            }

            var typeNameCSharpOriginal = parameterTypeCSharp.Name ?? string.Empty;
            var typeNameCSharp = typeNameCSharpOriginal.Replace("*", "Ptr", StringComparison.InvariantCulture);
            var typeNameCSharpCapitalized = char.ToUpper(typeNameCSharp[0], CultureInfo.InvariantCulture) + typeNameCSharp[1..];
            parameterStringsCSharp.Add(typeNameCSharpCapitalized);
        }

        var className = _className.ToUpper(CultureInfo.InvariantCulture);
        var parameterStringsCSharpJoined = string.Join('_', parameterStringsCSharp);
        functionPointerNameCSharp =
            $"FnPtr_{className}_{parameterStringsCSharpJoined}_{returnTypeStringCapitalized}".Replace("__", "_", StringComparison.InvariantCulture);
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

        if (_systemTypeNameAliases.TryGetValue(typeName, out var mappedSystemTypeName))
        {
            return mappedSystemTypeName;
        }

        switch (typeName)
        {
            case "char":
                return "byte";
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
