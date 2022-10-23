// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Text.RegularExpressions;
using C2CS.Data.C.Model;
using C2CS.Data.CSharp.Model;
using C2CS.WriteCodeCSharp.CodeGenerator.Diagnostics;

namespace C2CS.WriteCodeCSharp.Mapper;

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

        foreach (var typeAlias in options.TypeAliases)
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
            .ToImmutableHashSet();
    }

    public CSharpAbstractSyntaxTree Map(
        DiagnosticCollection diagnostics, ImmutableArray<CAbstractSyntaxTree> abstractSyntaxTrees)
    {
        var candidateNodesBuilder =
            ImmutableDictionary.CreateBuilder<string, ImmutableArray<PlatformCandidateNode>.Builder>();

        foreach (var cAbstractSyntaxTree in abstractSyntaxTrees)
        {
            AddCandidateNodes(cAbstractSyntaxTree, candidateNodesBuilder);
        }

        var platformsCandidateNodes = candidateNodesBuilder
            .Values.Select(x => x.ToImmutableArray()).ToImmutableArray();

        var functions = ImmutableArray.CreateBuilder<CSharpFunction>();
        var functionPointers = ImmutableArray.CreateBuilder<CSharpFunctionPointer>();
        var structs = ImmutableArray.CreateBuilder<CSharpStruct>();
        var aliasStructs = ImmutableArray.CreateBuilder<CSharpAliasStruct>();
        var opaqueStructs = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>();
        var enums = ImmutableArray.CreateBuilder<CSharpEnum>();
        var macroObjects = ImmutableArray.CreateBuilder<CSharpMacroObject>();
        var enumConstants = ImmutableArray.CreateBuilder<CSharpEnumConstant>();

        foreach (var platformCandidateNodes in platformsCandidateNodes)
        {
            var node = MergePlatformCandidateNodes(diagnostics, platformCandidateNodes);
            switch (node)
            {
                case CSharpFunction function:
                    functions.Add(function);
                    break;
                case CSharpFunctionPointer functionPointer:
                    functionPointers.Add(functionPointer);
                    break;
                case CSharpStruct @struct:
                    structs.Add(@struct);
                    break;
                case CSharpAliasStruct aliasStruct:
                    aliasStructs.Add(aliasStruct);
                    break;
                case CSharpOpaqueStruct opaqueStruct:
                    opaqueStructs.Add(opaqueStruct);
                    break;
                case CSharpEnum @enum:
                    enums.Add(@enum);
                    break;
                case CSharpMacroObject macroObject:
                    macroObjects.Add(macroObject);
                    break;
                case CSharpEnumConstant enumConstant:
                    enumConstants.Add(enumConstant);
                    break;
            }
        }

        var ast = new CSharpAbstractSyntaxTree
        {
            Functions = functions.ToImmutable(),
            FunctionPointers = functionPointers.ToImmutable(),
            Structs = structs.ToImmutable(),
            AliasStructs = aliasStructs.ToImmutable(),
            OpaqueStructs = opaqueStructs.ToImmutable(),
            Enums = enums.ToImmutable(),
            MacroObjects = macroObjects.ToImmutable(),
            EnumConstants = enumConstants.ToImmutable()
        };

        return ast;
    }

    private CSharpNode? MergePlatformCandidateNodes(
        DiagnosticCollection diagnostics, ImmutableArray<PlatformCandidateNode> platformNodes)
    {
        if (platformNodes.IsDefaultOrEmpty)
        {
            return null;
        }

        var allCanMerge = true;
        var firstPlatformNode = platformNodes.First();
        var firstNode = firstPlatformNode.CSharpNode;
        foreach (var platformNode in platformNodes)
        {
            var canMerge = platformNode.CSharpNode.Equals(firstNode);
            if (canMerge)
            {
                continue;
            }

            allCanMerge = false;
            canMerge = platformNode.CSharpNode.Equals(firstNode);
            break;
        }

        if (allCanMerge)
        {
            return MergeNodes(platformNodes);
        }

        var platforms = platformNodes
            .Select(x => x.Platform).ToArray();
        var diagnostic = new CSharpMergePlatformNodesDiagnostic(firstNode.Name, platforms);
        diagnostics.Add(diagnostic);
        return null;
    }

    private CSharpNode MergeNodes(ImmutableArray<PlatformCandidateNode> nodes)
    {
        var firstNode = nodes.First().CSharpNode;
        var platforms = nodes.Select(x => x.Platform).ToImmutableArray();
        var attributes = MergedAttributes(nodes);

        switch (firstNode)
        {
            case CSharpFunction:
                return MergeFunction(nodes, platforms, attributes);
            case CSharpFunctionPointer:
                return MergeFunctionPointer(nodes, platforms, attributes);
            case CSharpStruct:
                return MergeStruct(nodes, platforms, attributes);
            case CSharpAliasStruct:
                return MergeAliasStruct(nodes, platforms, attributes);
            case CSharpOpaqueStruct:
                return MergeOpaqueStruct(nodes, platforms, attributes);
            case CSharpEnum:
                return MergeEnum(nodes, platforms, attributes);
            case CSharpMacroObject:
                return MergeMacroObject(nodes, platforms, attributes);
            case CSharpEnumConstant:
                return MergeEnumConstant(nodes, platforms, attributes);
            default:
                throw new NotImplementedException();
        }
    }

    private CSharpFunction MergeFunction(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpFunction)nodes.First().CSharpNode;
        var mergedNode = new CSharpFunction(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf,
            node.CallingConvention,
            node.ReturnType,
            node.Parameters,
            attributes);
        return mergedNode;
    }

    private CSharpFunctionPointer MergeFunctionPointer(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpFunctionPointer)nodes.First().CSharpNode;
        var newNode = new CSharpFunctionPointer(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf,
            node.ReturnType,
            node.Parameters,
            attributes);
        return newNode;
    }

    private CSharpStruct MergeStruct(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpStruct)nodes.First().CSharpNode;
        var newNode = new CSharpStruct(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf!.Value,
            node.AlignOf,
            node.Fields,
            node.NestedStructs,
            node.Attributes);
        return newNode;
    }

    private CSharpAliasStruct MergeAliasStruct(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpAliasStruct)nodes.First().CSharpNode;
        var newNode = new CSharpAliasStruct(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf,
            node.UnderlyingType,
            attributes);
        return newNode;
    }

    private CSharpOpaqueStruct MergeOpaqueStruct(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpOpaqueStruct)nodes.First().CSharpNode;
        var newNode = new CSharpOpaqueStruct(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            attributes);
        return newNode;
    }

    private CSharpEnum MergeEnum(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpEnum)nodes.First().CSharpNode;
        var newNode = new CSharpEnum(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.IntegerType,
            node.Values,
            attributes);
        return newNode;
    }

    private CSharpMacroObject MergeMacroObject(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpMacroObject)nodes.First().CSharpNode;
        var newNode = new CSharpMacroObject(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf,
            node.Type,
            node.Value,
            node.IsConstant,
            attributes);
        return newNode;
    }

    private CSharpEnumConstant MergeEnumConstant(
        ImmutableArray<PlatformCandidateNode> nodes,
        ImmutableArray<TargetPlatform> platforms,
        ImmutableArray<Attribute> attributes)
    {
        var node = (CSharpEnumConstant)nodes.First().CSharpNode;
        var newNode = new CSharpEnumConstant(
            platforms,
            node.Name,
            node.CKind,
            node.CCodeLocation,
            node.SizeOf,
            node.Type,
            node.Value,
            attributes);
        return newNode;
    }

    private ImmutableArray<Attribute> MergedAttributes(ImmutableArray<PlatformCandidateNode> nodes)
    {
        var builder = ImmutableArray.CreateBuilder<Attribute>();

        foreach (var node in nodes)
        {
            builder.AddRange(node.CSharpNode.Attributes);

            var platformAttribute = new CNodeAttribute
            {
                Kind = node.CSharpNode.CKind,
                Platform = node.Platform.ToString(),
                Location = node.CSharpNode.CCodeLocation
            };
            builder.Add(platformAttribute);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private void AddCandidateNode(
        TargetPlatform platform,
        CSharpNode node,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder candidateNodes)
    {
        var candidateNode = new PlatformCandidateNode
        {
            Platform = platform,
            CSharpNode = node
        };

        var key = node.Name + ":" + node.GetHashCode();
        var isFirstTimeEncountered = !candidateNodes.TryGetValue(key, out var nodes);
        if (isFirstTimeEncountered)
        {
            nodes = ImmutableArray.CreateBuilder<PlatformCandidateNode>();
            candidateNodes.Add(key, nodes);
        }

        nodes!.Add(candidateNode);
    }

    private void AddCandidateNodes(
        CAbstractSyntaxTree ast,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        var context = new CSharpCodeMapperContext(ast.PlatformRequested, ast.Records, ast.FunctionPointers);
        var functionsC = ast.Functions.Values.ToImmutableArray();
        var functionNamesC = ast.Functions.Keys.ToImmutableHashSet();
        var functionPointersC = ast.FunctionPointers.Values.ToImmutableArray();
        var recordsC = ast.Records.Values.ToImmutableArray();
        var typeAliasesC = ast.TypeAliases.Values.ToImmutableArray();
        var opaqueTypesC = ast.OpaqueTypes.Values.ToImmutableArray();
        var enumsC = ast.Enums.Values.ToImmutableArray();
        var macroObjectsC = ast.MacroObjects.Values.ToImmutableArray();
        var enumConstantsC = ast.EnumConstants.Values.ToImmutableArray();

        var functions = Functions(context, functionsC);
        var structs = Structs(context, recordsC, functionNamesC);
        var aliasStructs = AliasStructs(context, typeAliasesC);
        var functionPointers = FunctionPointers(context, functionPointersC);
        var opaqueStructs = OpaqueStructs(context, opaqueTypesC);
        var enums = Enums(context, enumsC);
        var macroObjects = MacroObjects(context, macroObjectsC);
        var enumConstants = EnumConstants(context, enumConstantsC);

        var platform = ast.PlatformRequested;
        AddCandidateFunctions(platform, functions, builder);
        AddCandidateStructs(platform, structs, builder);
        AddCandidateAliasStructs(platform, aliasStructs, builder);
        AddCandidateFunctionPointers(platform, functionPointers, builder);
        AddCandidateOpaqueStructs(platform, opaqueStructs, builder);
        AddCandidateEnums(platform, enums, builder);
        AddCandidateMacroObjects(platform, macroObjects, builder);
        AddCandidateEnumConstants(platform, enumConstants, builder);
    }

    private void AddCandidateFunctions(
        TargetPlatform platform,
        ImmutableArray<CSharpFunction> functions,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in functions)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateFunctionPointers(
        TargetPlatform platform,
        ImmutableArray<CSharpFunctionPointer> functionPointers,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in functionPointers)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateStructs(
        TargetPlatform platform,
        ImmutableArray<CSharpStruct> structs,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in structs)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateAliasStructs(
        TargetPlatform platform,
        ImmutableArray<CSharpAliasStruct> aliasStructs,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in aliasStructs)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateOpaqueStructs(
        TargetPlatform platform,
        ImmutableArray<CSharpOpaqueStruct> opaqueDataTypes,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in opaqueDataTypes)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateEnums(
        TargetPlatform platform,
        ImmutableArray<CSharpEnum> enums,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in enums)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateMacroObjects(
        TargetPlatform platform,
        ImmutableArray<CSharpMacroObject> constants,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in constants)
        {
            AddCandidateNode(platform, value, builder);
        }
    }

    private void AddCandidateEnumConstants(
        TargetPlatform platform,
        ImmutableArray<CSharpEnumConstant> enumConstants,
        ImmutableDictionary<string, ImmutableArray<PlatformCandidateNode>.Builder>.Builder builder)
    {
        foreach (var value in enumConstants)
        {
            AddCandidateNode(platform, value, builder);
        }
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

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunction FunctionCSharp(CSharpCodeMapperContext context, CFunction cFunction)
    {
        var name = cFunction.Name;
        var cKindString = CKindString(cFunction);
        var cCodeLocation = CCodeLocation(cFunction);
        var returnTypeNameCSharp = TypeNameCSharp(context, cFunction.ReturnTypeInfo);
        var returnType = TypeCSharp(returnTypeNameCSharp, cFunction.ReturnTypeInfo);
        var callingConvention = CSharpFunctionCallingConvention(cFunction.CallingConvention);
        var parameters = CSharpFunctionParameters(context, name, cFunction.Parameters);
        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpFunction(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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
            CFunctionCallingConvention.Cdecl => Data.CSharp.Model.CSharpFunctionCallingConvention.Cdecl,
            CFunctionCallingConvention.StdCall => Data.CSharp.Model.CSharpFunctionCallingConvention.StdCall,
            CFunctionCallingConvention.FastCall => Data.CSharp.Model.CSharpFunctionCallingConvention.FastCall,
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
        var cKindString = CKindString(functionParameter);
        var cCodeLocation = CCodeLocation(functionParameter);
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
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointer? FunctionPointer(
        CSharpCodeMapperContext context,
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

        var cKindString = CKindString(functionPointer);
        var cCodeLocation = CCodeLocation(functionPointer);
        var returnTypeC = functionPointer.ReturnTypeInfo;
        var returnTypeNameCSharp = TypeNameCSharp(context, returnTypeC);
        var returnTypeCSharp = TypeCSharp(returnTypeNameCSharp, returnTypeC);

        var parameters = FunctionPointerParameters(context, functionPointer.Parameters);
        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpFunctionPointer(
            ImmutableArray.Create(context.Platform),
            typeNameCSharp,
            cKindString,
            cCodeLocation,
            functionPointerType.SizeOf,
            returnTypeCSharp,
            parameters,
            attributes);

        names.Add(typeNameCSharp, functionPointer);

        return result;
    }

    private ImmutableArray<CSharpFunctionPointerParameter> FunctionPointerParameters(
        CSharpCodeMapperContext context,
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
            var value =
                FunctionPointerParameter(context, functionPointerParameterC, parameterName);
            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpFunctionPointerParameter FunctionPointerParameter(
        CSharpCodeMapperContext context,
        CFunctionPointerParameter functionPointerParameter,
        string parameterName)
    {
        var name = SanitizeIdentifier(parameterName);
        var cKindString = CKindString(functionPointerParameter);
        var cCodeLocation = CCodeLocation(functionPointerParameter);
        var typeC = functionPointerParameter.TypeInfo;
        var nameCSharp = TypeNameCSharp(context, typeC);
        var typeCSharp = TypeCSharp(nameCSharp, typeC);

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpFunctionPointerParameter(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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
        var results = ImmutableArray.CreateBuilder<CSharpStruct>(records.Length);

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

            results.Add(value);
        }

        return results.ToImmutable();
    }

    private CSharpStruct Struct(
        CSharpCodeMapperContext context,
        CRecord record,
        ImmutableHashSet<string> functionNames)
    {
        var name = record.Name;
        if (functionNames.Contains(record.Name))
        {
            name = record.Name + "_";
        }

        var cKindString = CKindString(record);
        var cCodeLocation = CCodeLocation(record);
        var (fields, nestedRecords) = StructFields(context, record.Name, record.Fields);
        var nestedStructs = Structs(context, nestedRecords, functionNames);

        var attributes = ImmutableArray<Attribute>.Empty;

        return new CSharpStruct(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
            record.SizeOf,
            record.AlignOf,
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
        CRecordField field)
    {
        var name = SanitizeIdentifier(field.Name);
        var cKindString = CKindString(field);
        var codeLocationComment = CCodeLocation(field);
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

        var offsetOf = field.OffsetOf;
        var isWrapped = typeCSharp.IsArray && !IsValidFixedBufferType(typeCSharp.Name);

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpStructField(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            codeLocationComment,
            typeC.SizeOf,
            typeCSharp,
            offsetOf,
            isWrapped,
            attributes);

        return result;
    }

    private ImmutableArray<CSharpOpaqueStruct> OpaqueStructs(
        CSharpCodeMapperContext context,
        ImmutableArray<COpaqueType> opaqueDataTypes)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>(opaqueDataTypes.Length);

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var opaqueDataTypeC in opaqueDataTypes)
        {
            var value = OpaqueDataStruct(context, opaqueDataTypeC);

            if (_ignoredNames.Contains(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpOpaqueStruct OpaqueDataStruct(
        CSharpCodeMapperContext context,
        COpaqueType opaqueType)
    {
        var nameCSharp = TypeNameCSharpRaw(opaqueType.Name, opaqueType.SizeOf);
        var cKindString = CKindString(opaqueType);
        var cCodeLocation = CCodeLocation(opaqueType);

        var attributes = ImmutableArray<Attribute>.Empty;

        var opaqueTypeCSharp = new CSharpOpaqueStruct(
            ImmutableArray.Create(context.Platform),
            nameCSharp,
            cKindString,
            cCodeLocation,
            attributes);

        return opaqueTypeCSharp;
    }

    private ImmutableArray<CSharpAliasStruct> AliasStructs(
        CSharpCodeMapperContext context,
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

            var value = AliasStruct(context, typedef);
            if (_ignoredNames.Contains(value.Name))
            {
                continue;
            }

            builder.Add(value);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpAliasStruct AliasStruct(
        CSharpCodeMapperContext context,
        CTypeAlias typeAlias)
    {
        var name = typeAlias.Name;

        var cKindString = CKindString(typeAlias);
        var cCodeLocation = CCodeLocation(typeAlias);
        var underlyingTypeC = typeAlias.UnderlyingTypeInfo;
        var underlyingNameCSharp = TypeNameCSharp(context, underlyingTypeC);
        var underlyingTypeCSharp = TypeCSharp(underlyingNameCSharp, underlyingTypeC);

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpAliasStruct(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnum Enum(
        CSharpCodeMapperContext context,
        CEnum @enum)
    {
        var name = @enum.Name;
        var cKindString = CKindString(@enum);
        var cCodeLocation = CCodeLocation(@enum);
        var integerTypeC = @enum.IntegerTypeInfo;
        var integerNameCSharp = TypeNameCSharp(context, integerTypeC, true);
        var integerType = TypeCSharp(integerNameCSharp, integerTypeC);
        var values = EnumValues(context, @enum.Values);

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpEnum(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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
        CSharpCodeMapperContext context, CEnumValue enumValue)
    {
        var name = enumValue.Name;
        var cKindString = CKindString(enumValue);
        var cCodeLocation = CCodeLocation(enumValue);
        var value = enumValue.Value;

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpEnumValue(
            ImmutableArray.Create(context.Platform),
            name,
            cKindString,
            cCodeLocation,
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

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpMacroObject MacroObject(
        CSharpCodeMapperContext context,
        CMacroObject macro)
    {
        var cKindString = CKindString(macro);
        var cCodeLocation = CCodeLocation(macro);

        var typeKind = macro.Type.Kind;
        var isConstant = typeKind is CKind.Primitive;
        var typeSize = macro.Type.SizeOf;

        var typeName = TypeNameCSharp(context, macro.Type);
        if (typeName == "CString")
        {
            typeName = "string";
        }

        var value = macro.Value;
        if (typeName == "float")
        {
            value += "f";
        }

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpMacroObject(
            ImmutableArray.Create(context.Platform),
            macro.Name,
            cKindString,
            cCodeLocation,
            typeSize,
            typeName,
            value,
            isConstant,
            attributes);
        return result;
    }

    private ImmutableArray<CSharpEnumConstant> EnumConstants(
        CSharpCodeMapperContext context,
        ImmutableArray<CEnumConstant> enumConstants)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpEnumConstant>(enumConstants.Length);

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

        var result = builder.ToImmutable();
        return result;
    }

    private CSharpEnumConstant EnumConstant(CSharpCodeMapperContext context, CEnumConstant enumConstant)
    {
        var cKindString = CKindString(enumConstant);
        var cCodeLocation = CCodeLocation(enumConstant);
        var typeName = TypeNameCSharp(context, enumConstant.Type);

        var attributes = ImmutableArray<Attribute>.Empty;

        var result = new CSharpEnumConstant(
            ImmutableArray.Create(context.Platform),
            enumConstant.Name,
            cKindString,
            cCodeLocation,
            enumConstant.Type.SizeOf,
            typeName,
            enumConstant.Value,
            attributes);
        return result;
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
        CSharpCodeMapperContext context,
        CTypeInfo typeInfo,
        bool forceUnsigned = false)
    {
        if (typeInfo.Kind == CKind.FunctionPointer)
        {
            var functionPointer = context.FunctionPointers[typeInfo.Name];
            return TypeNameCSharpFunctionPointer(context, typeInfo.Name, functionPointer);
        }

        string result;
        if (typeInfo.Kind is CKind.Pointer or CKind.Array)
        {
            result = TypeNameCSharpPointer(typeInfo.Name, typeInfo.InnerTypeInfo!);
        }
        else
        {
            result = TypeNameCSharpRaw(typeInfo.Name, typeInfo.SizeOf, forceUnsigned);
        }

        // TODO: https://github.com/lithiumtoast/c2cs/issues/15
        if (result == "va_list")
        {
            result = "nint";
        }

        return result;
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

    private string TypeNameCSharpPointer(string typeName, CTypeInfo innerTypeInfo)
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

    private static string CCodeLocation(CNode node)
    {
        if (node is not CNodeWithLocation nodeWithLocation)
        {
            return string.Empty;
        }

        var location = nodeWithLocation.Location.ToString();
        return location;
    }

    private static string CKindString(CNode node)
    {
        var kindString = node switch
        {
            CRecord record => record.RecordKind.ToString(),
            _ => node.Kind.ToString()
        };
        return kindString;
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

    private record struct PlatformCandidateNode
    {
        public CSharpNode CSharpNode;
        public TargetPlatform Platform;
    }
}
