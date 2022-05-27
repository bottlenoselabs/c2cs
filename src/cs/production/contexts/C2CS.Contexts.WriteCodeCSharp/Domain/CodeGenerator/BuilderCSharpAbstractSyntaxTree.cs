// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class BuilderCSharpAbstractSyntaxTree
{
    private readonly HashSet<TargetPlatform> _platforms = new();
    private readonly Dictionary<string, List<PlatformCandidateNode>> _candidateNodes = new();

    private readonly ImmutableArray<CSharpFunction>.Builder _agnosticFunctions = ImmutableArray.CreateBuilder<CSharpFunction>();
    private readonly ImmutableArray<CSharpFunctionPointer>.Builder _agnosticFunctionPointers = ImmutableArray.CreateBuilder<CSharpFunctionPointer>();
    private readonly ImmutableArray<CSharpStruct>.Builder _agnosticStructs = ImmutableArray.CreateBuilder<CSharpStruct>();
    private readonly ImmutableArray<CSharpAliasStruct>.Builder _agnosticAliasStructs = ImmutableArray.CreateBuilder<CSharpAliasStruct>();
    private readonly ImmutableArray<CSharpOpaqueStruct>.Builder _agnosticOpaqueStructs = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>();
    private readonly ImmutableArray<CSharpEnum>.Builder _agnosticEnums = ImmutableArray.CreateBuilder<CSharpEnum>();
    private readonly ImmutableArray<CSharpMacroObject>.Builder _agnosticMacroObjects = ImmutableArray.CreateBuilder<CSharpMacroObject>();
    private readonly ImmutableArray<CSharpEnumConstant>.Builder _agnosticEnumConstants = ImmutableArray.CreateBuilder<CSharpEnumConstant>();

    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpFunction>.Builder> _functionsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpFunctionPointer>.Builder> _functionPointersByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpStruct>.Builder> _structsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpAliasStruct>.Builder> _aliasStructsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpOpaqueStruct>.Builder> _opaqueStructsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpEnum>.Builder> _enumsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpMacroObject>.Builder> _macroObjectsByPlatform = new();
    private readonly Dictionary<TargetPlatform, ImmutableArray<CSharpEnumConstant>.Builder> _enumConstantsByPlatform = new();

    public void Add(TargetPlatform platform, CSharpNodes nodes)
    {
        AddPlatform(platform);
        AddCandidateFunctions(platform, nodes.Functions);
        AddCandidateFunctionPointers(platform, nodes.FunctionPointers);
        AddCandidateStructs(platform, nodes.Structs);
        AddCandidateAliasStructs(platform, nodes.AliasStructs);
        AddOpaqueStructs(platform, nodes.OpaqueStructs);
        AddCandidateEnums(platform, nodes.Enums);
        AddCandidateMacroObjects(platform, nodes.MacroObjects);
        AddCandidateEnumConstants(platform, nodes.EnumConstants);
    }

    private void AddPlatform(TargetPlatform platform)
    {
        var alreadyAdded = _platforms.Contains(platform);
        if (alreadyAdded)
        {
            return;
        }

        _platforms.Add(platform);
        _functionsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpFunction>();
        _functionPointersByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpFunctionPointer>();
        _structsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpStruct>();
        _aliasStructsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpAliasStruct>();
        _enumsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpEnum>();
        _macroObjectsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpMacroObject>();
        _opaqueStructsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>();
        _enumConstantsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpEnumConstant>();
    }

    public CSharpAbstractSyntaxTree Build()
    {
        foreach (var (_, nodes) in _candidateNodes)
        {
            CreateNodes(nodes);
        }

        var ast = new CSharpAbstractSyntaxTree
        {
            PlatformAgnosticNodes = PlatformAgnosticNodes(),
            PlatformSpecificNodes = PlatformSpecificNodes()
        };

        return ast;
    }

    private void CreateNodes(List<PlatformCandidateNode> platformNodes)
    {
        if (platformNodes.Count == 0)
        {
            return;
        }

        var allAreSame = true;
        var firstPlatform = platformNodes.First();
        var firstNode = firstPlatform.CSharpNode;
        foreach (var platformNode in platformNodes)
        {
            var canBeMerged = CanMergeNodes(platformNode.CSharpNode, firstNode);
            if (canBeMerged)
            {
                continue;
            }

            allAreSame = false;
            break;
        }

        if (allAreSame)
        {
            CreatePlatformAgnosticNode(firstNode);
        }
        else
        {
            foreach (var platformNode in platformNodes)
            {
                CreatePlatformSpecificNode(platformNode.Platform, platformNode.CSharpNode);
            }
        }
    }

    private bool CanMergeNodes(CSharpNode firstNode, CSharpNode secondNode)
    {
        return firstNode.Equals(secondNode);
    }

    private CSharpNodes PlatformAgnosticNodes()
    {
        var sharedNodes = new CSharpNodes
        {
            Functions = _agnosticFunctions.ToImmutable(),
            FunctionPointers = _agnosticFunctionPointers.ToImmutable(),
            Structs = _agnosticStructs.ToImmutable(),
            AliasStructs = _agnosticAliasStructs.ToImmutable(),
            OpaqueStructs = _agnosticOpaqueStructs.ToImmutable(),
            Enums = _agnosticEnums.ToImmutable(),
            MacroObjects = _agnosticMacroObjects.ToImmutable(),
            EnumConstants = _agnosticEnumConstants.ToImmutable()
        };

        return sharedNodes;
    }

    private ImmutableArray<(TargetPlatform Platform, CSharpNodes Nodes)> PlatformSpecificNodes()
    {
        var builder = ImmutableArray.CreateBuilder<(TargetPlatform, CSharpNodes)>();
        foreach (var platform in _platforms)
        {
            var platformNodes = BuildPlatformNodes(platform);
            if (platformNodes == null)
            {
                continue;
            }

            builder.Add((platform, platformNodes));
        }

        var platformSpecificNodes = builder.ToImmutable();
        return platformSpecificNodes;
    }

    private CSharpNodes? BuildPlatformNodes(TargetPlatform platform)
    {
        var functions = _functionsByPlatform[platform].ToImmutableArray();
        var functionPointers = _functionPointersByPlatform[platform].ToImmutableArray();
        var structs = _structsByPlatform[platform].ToImmutableArray();
        var aliasStructs = _aliasStructsByPlatform[platform].ToImmutableArray();
        var opaqueStructs = _opaqueStructsByPlatform[platform].ToImmutableArray();
        var enums = _enumsByPlatform[platform].ToImmutableArray();
        var macroObjects = _macroObjectsByPlatform[platform].ToImmutableArray();
        var enumConstants = _enumConstantsByPlatform[platform].ToImmutableArray();

        if (functions.IsDefaultOrEmpty &&
            functionPointers.IsDefaultOrEmpty &&
            structs.IsDefaultOrEmpty &&
            aliasStructs.IsDefaultOrEmpty &&
            opaqueStructs.IsDefaultOrEmpty &&
            enums.IsDefaultOrEmpty &&
            macroObjects.IsDefaultOrEmpty &&
            enumConstants.IsDefaultOrEmpty)
        {
            return null;
        }

        var nodes = new CSharpNodes
        {
            Functions = functions,
            FunctionPointers = functionPointers,
            Structs = structs,
            AliasStructs = aliasStructs,
            OpaqueStructs = opaqueStructs,
            Enums = enums,
            MacroObjects = macroObjects,
            EnumConstants = enumConstants
        };

        return nodes;
    }

    private void AddCandidateFunctions(
        TargetPlatform platform, ImmutableArray<CSharpFunction> functions)
    {
        foreach (var value in functions)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateFunctionPointers(
        TargetPlatform platform, ImmutableArray<CSharpFunctionPointer> functionPointers)
    {
        foreach (var value in functionPointers)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateStructs(
        TargetPlatform platform, ImmutableArray<CSharpStruct> structs)
    {
        foreach (var value in structs)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateAliasStructs(
        TargetPlatform platform, ImmutableArray<CSharpAliasStruct> aliasStructs)
    {
        foreach (var value in aliasStructs)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddOpaqueStructs(
        TargetPlatform platform, ImmutableArray<CSharpOpaqueStruct> opaqueDataTypes)
    {
        foreach (var value in opaqueDataTypes)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateEnums(
        TargetPlatform platform, ImmutableArray<CSharpEnum> enums)
    {
        foreach (var value in enums)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateMacroObjects(
        TargetPlatform platform, ImmutableArray<CSharpMacroObject> constants)
    {
        foreach (var value in constants)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateEnumConstants(
        TargetPlatform platform, ImmutableArray<CSharpEnumConstant> enumConstants)
    {
        foreach (var value in enumConstants)
        {
            AddCandidateNode(platform, value);
        }
    }

    private void AddCandidateNode(TargetPlatform platform, CSharpNode node)
    {
        var candidateNode = new PlatformCandidateNode
        {
            Platform = platform,
            CSharpNode = node
        };

        var key = node.Name + ":" + node.GetHashCode();
        var isFirstTimeEncountered = !_candidateNodes.TryGetValue(key, out var nodes);
        if (isFirstTimeEncountered)
        {
            nodes = new List<PlatformCandidateNode> { candidateNode };
            _candidateNodes.Add(key, nodes);
        }
        else
        {
            nodes!.Add(candidateNode);
        }
    }

    private void CreatePlatformAgnosticNode(CSharpNode node)
    {
        switch (node)
        {
            case CSharpFunction function:
                AddNodeFunction(null, function);
                break;
            case CSharpFunctionPointer functionPointer:
                AddNodeFunctionPointer(null, functionPointer);
                break;
            case CSharpStruct @struct:
                AddNodeStruct(null, @struct);
                break;
            case CSharpAliasStruct aliasStruct:
                AddNodeAliasStruct(null, aliasStruct);
                break;
            case CSharpOpaqueStruct opaqueStruct:
                AddNodeOpaqueStruct(null, opaqueStruct);
                break;
            case CSharpEnum @enum:
                AddNodeEnum(null, @enum);
                break;
            case CSharpMacroObject macroObject:
                AddNodeMacroObject(null, macroObject);
                break;
            case CSharpEnumConstant enumConstant:
                AddNodeEnumConstant(null, enumConstant);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void CreatePlatformSpecificNode(TargetPlatform platform, CSharpNode node)
    {
        switch (node)
        {
            case CSharpFunction function:
                AddNodeFunction(platform, function);
                break;
            case CSharpFunctionPointer functionPointer:
                AddNodeFunctionPointer(platform, functionPointer);
                break;
            case CSharpStruct @struct:
                AddNodeStruct(platform, @struct);
                break;
            case CSharpAliasStruct aliasStruct:
                AddNodeAliasStruct(platform, aliasStruct);
                break;
            case CSharpOpaqueStruct opaqueStruct:
                AddNodeOpaqueStruct(platform, opaqueStruct);
                break;
            case CSharpEnum @enum:
                AddNodeEnum(platform, @enum);
                break;
            case CSharpMacroObject macroObject:
                AddNodeMacroObject(platform, macroObject);
                break;
            case CSharpEnumConstant enumConstant:
                AddNodeEnumConstant(platform, enumConstant);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void AddNodeFunction(TargetPlatform? platform, CSharpFunction node)
    {
        var builder = platform != null ? _functionsByPlatform[platform.Value] : _agnosticFunctions;
        builder.Add(node);
    }

    private void AddNodeFunctionPointer(TargetPlatform? platform, CSharpFunctionPointer node)
    {
        var builder = platform != null ? _functionPointersByPlatform[platform.Value] : _agnosticFunctionPointers;
        builder.Add(node);
    }

    private void AddNodeStruct(TargetPlatform? platform, CSharpStruct node)
    {
        var builder = platform != null ? _structsByPlatform[platform.Value] : _agnosticStructs;
        builder.Add(node);
    }

    private void AddNodeAliasStruct(TargetPlatform? platform, CSharpAliasStruct node)
    {
        var builder = platform != null ? _aliasStructsByPlatform[platform.Value] : _agnosticAliasStructs;
        builder.Add(node);
    }

    private void AddNodeOpaqueStruct(TargetPlatform? platform, CSharpOpaqueStruct node)
    {
        var builder = platform != null ? _opaqueStructsByPlatform[platform.Value] : _agnosticOpaqueStructs;
        builder.Add(node);
    }

    private void AddNodeEnum(TargetPlatform? platform, CSharpEnum node)
    {
        var builder = platform != null ? _enumsByPlatform[platform.Value] : _agnosticEnums;
        builder.Add(node);
    }

    private void AddNodeMacroObject(TargetPlatform? platform, CSharpMacroObject node)
    {
        var builder = platform != null ? _macroObjectsByPlatform[platform.Value] : _agnosticMacroObjects;
        builder.Add(node);
    }

    private void AddNodeEnumConstant(TargetPlatform? platform, CSharpEnumConstant node)
    {
        var builder = platform != null ? _enumConstantsByPlatform[platform.Value] : _agnosticEnumConstants;
        builder.Add(node);
    }

    private record struct PlatformCandidateNode
    {
        public TargetPlatform Platform;
        public CSharpNode CSharpNode;
    }
}
