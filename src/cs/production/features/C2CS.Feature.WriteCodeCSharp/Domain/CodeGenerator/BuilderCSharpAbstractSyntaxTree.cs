// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.WriteCodeCSharp.Data;
using C2CS.Feature.WriteCodeCSharp.Data.Model;

namespace C2CS.Feature.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class BuilderCSharpAbstractSyntaxTree
{
    private readonly HashSet<NativePlatform> _platforms = new();
    private readonly Dictionary<string, List<PlatformCandidateNode>> _candidateNodesByName = new();

    private readonly ImmutableArray<CSharpFunction>.Builder _agnosticFunctions = ImmutableArray.CreateBuilder<CSharpFunction>();
    private readonly ImmutableArray<CSharpFunctionPointer>.Builder _agnosticFunctionPointers = ImmutableArray.CreateBuilder<CSharpFunctionPointer>();
    private readonly ImmutableArray<CSharpStruct>.Builder _agnosticStructs = ImmutableArray.CreateBuilder<CSharpStruct>();
    private readonly ImmutableArray<CSharpAliasStruct>.Builder _agnosticAliasStructs = ImmutableArray.CreateBuilder<CSharpAliasStruct>();
    private readonly ImmutableArray<CSharpOpaqueStruct>.Builder _agnosticOpaqueStructs = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>();
    private readonly ImmutableArray<CSharpEnum>.Builder _agnosticEnums = ImmutableArray.CreateBuilder<CSharpEnum>();
    private readonly ImmutableArray<CSharpConstant>.Builder _agnosticConstants = ImmutableArray.CreateBuilder<CSharpConstant>();

    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpFunction>.Builder> _functionsByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpFunctionPointer>.Builder> _functionPointersByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpStruct>.Builder> _structsByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpAliasStruct>.Builder> _aliasStructsByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpOpaqueStruct>.Builder> _opaqueStructsByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpEnum>.Builder> _enumsByPlatform = new();
    private readonly Dictionary<NativePlatform, ImmutableArray<CSharpConstant>.Builder> _constantsByPlatform = new();

    public void Add(NativePlatform platform, CSharpNodes nodes)
    {
        AddPlatform(platform);
        AddCandidateFunctions(platform, nodes.Functions);
        AddCandidateFunctionPointers(platform, nodes.FunctionPointers);
        AddCandidateStructs(platform, nodes.Structs);
        AddCandidateAliasStructs(platform, nodes.AliasStructs);
        AddOpaqueStructs(platform, nodes.OpaqueStructs);
        AddCandidateEnums(platform, nodes.Enums);
        AddCandidateConstants(platform, nodes.Constants);
    }

    private void AddPlatform(NativePlatform platform)
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
        _constantsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpConstant>();
        _opaqueStructsByPlatform[platform] = ImmutableArray.CreateBuilder<CSharpOpaqueStruct>();
    }

    public CSharpAbstractSyntaxTree Build()
    {
        foreach (var (_, nodes) in _candidateNodesByName)
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
        var firstNode = platformNodes.First().CSharpNode;
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
        if (!firstNode.Equals(secondNode))
        {
            return false;
        }

        return true;
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
            Constants = _agnosticConstants.ToImmutable()
        };

        return sharedNodes;
    }

    private ImmutableArray<(NativePlatform Platform, CSharpNodes Nodes)> PlatformSpecificNodes()
    {
        var builder = ImmutableArray.CreateBuilder<(NativePlatform, CSharpNodes)>();
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

    private CSharpNodes? BuildPlatformNodes(NativePlatform platform)
    {
        var functions = _functionsByPlatform[platform].ToImmutableArray();
        var functionPointers = _functionPointersByPlatform[platform].ToImmutableArray();
        var structs = _structsByPlatform[platform].ToImmutableArray();
        var aliasStructs = _aliasStructsByPlatform[platform].ToImmutableArray();
        var opaqueStructs = _opaqueStructsByPlatform[platform].ToImmutableArray();
        var enums = _enumsByPlatform[platform].ToImmutableArray();
        var constants = _constantsByPlatform[platform].ToImmutableArray();

        if (functions.IsDefaultOrEmpty &&
            functionPointers.IsDefaultOrEmpty &&
            structs.IsDefaultOrEmpty &&
            aliasStructs.IsDefaultOrEmpty &&
            opaqueStructs.IsDefaultOrEmpty &&
            enums.IsDefaultOrEmpty &&
            constants.IsDefaultOrEmpty)
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
            Constants = constants
        };

        return nodes;
    }

    private void AddCandidateFunctions(
        NativePlatform platform, ImmutableArray<CSharpFunction> functions)
    {
        foreach (var function in functions)
        {
            AddCandidateNode(platform, function);
        }
    }

    private void AddCandidateFunctionPointers(
        NativePlatform platform, ImmutableArray<CSharpFunctionPointer> functionPointers)
    {
        foreach (var functionPointer in functionPointers)
        {
            AddCandidateNode(platform, functionPointer);
        }
    }

    private void AddCandidateStructs(
        NativePlatform platform, ImmutableArray<CSharpStruct> structs)
    {
        foreach (var @struct in structs)
        {
            AddCandidateNode(platform, @struct);
        }
    }

    private void AddCandidateAliasStructs(
        NativePlatform platform, ImmutableArray<CSharpAliasStruct> aliasStructs)
    {
        foreach (var aliasStruct in aliasStructs)
        {
            AddCandidateNode(platform, aliasStruct);
        }
    }

    private void AddOpaqueStructs(
        NativePlatform platform, ImmutableArray<CSharpOpaqueStruct> opaqueDataTypes)
    {
        foreach (var opaqueType in opaqueDataTypes)
        {
            AddCandidateNode(platform, opaqueType);
        }
    }

    private void AddCandidateEnums(
        NativePlatform platform, ImmutableArray<CSharpEnum> enums)
    {
        foreach (var @enum in enums)
        {
            AddCandidateNode(platform, @enum);
        }
    }

    private void AddCandidateConstants(
        NativePlatform platform, ImmutableArray<CSharpConstant> constants)
    {
        foreach (var constant in constants)
        {
            AddCandidateNode(platform, constant);
        }
    }

    private void AddCandidateNode(NativePlatform platform, CSharpNode node)
    {
        var candidateNode = new PlatformCandidateNode
        {
            Platform = platform,
            CSharpNode = node
        };

        var isFirstTimeEncounteredName = !_candidateNodesByName.TryGetValue(node.Name, out var nodes);
        if (isFirstTimeEncounteredName)
        {
            nodes = new List<PlatformCandidateNode> { candidateNode };
            _candidateNodesByName.Add(node.Name, nodes);
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
            case CSharpConstant constant:
                AddNodeConstant(null, constant);
                break;
        }
    }

    private void CreatePlatformSpecificNode(NativePlatform platform, CSharpNode node)
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
            case CSharpConstant constant:
                AddNodeConstant(platform, constant);
                break;
        }
    }

    private void AddNodeFunction(NativePlatform? platform, CSharpFunction node)
    {
        var builder = platform != null ? _functionsByPlatform[platform.Value] : _agnosticFunctions;
        builder.Add(node);
    }

    private void AddNodeFunctionPointer(NativePlatform? platform, CSharpFunctionPointer node)
    {
        var builder = platform != null ? _functionPointersByPlatform[platform.Value] : _agnosticFunctionPointers;
        builder.Add(node);
    }

    private void AddNodeStruct(NativePlatform? platform, CSharpStruct node)
    {
        var builder = platform != null ? _structsByPlatform[platform.Value] : _agnosticStructs;
        builder.Add(node);
    }

    private void AddNodeAliasStruct(NativePlatform? platform, CSharpAliasStruct node)
    {
        var builder = platform != null ? _aliasStructsByPlatform[platform.Value] : _agnosticAliasStructs;
        builder.Add(node);
    }

    private void AddNodeOpaqueStruct(NativePlatform? platform, CSharpOpaqueStruct node)
    {
        var builder = platform != null ? _opaqueStructsByPlatform[platform.Value] : _agnosticOpaqueStructs;
        builder.Add(node);
    }

    private void AddNodeEnum(NativePlatform? platform, CSharpEnum node)
    {
        var builder = platform != null ? _enumsByPlatform[platform.Value] : _agnosticEnums;
        builder.Add(node);
    }

    private void AddNodeConstant(NativePlatform? platform, CSharpConstant node)
    {
        var builder = platform != null ? _constantsByPlatform[platform.Value] : _agnosticConstants;
        builder.Add(node);
    }

    private record struct PlatformCandidateNode
    {
        public NativePlatform Platform;
        public CSharpNode CSharpNode;
    }
}
