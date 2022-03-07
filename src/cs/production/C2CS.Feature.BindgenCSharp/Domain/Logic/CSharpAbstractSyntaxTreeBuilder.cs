// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BindgenCSharp.Data.Model;

namespace C2CS.Feature.BindgenCSharp.Domain.Logic;

public class CSharpAbstractSyntaxTreeBuilder
{
    private readonly HashSet<RuntimePlatform> _platforms = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpFunction Function)?> _sharedFunctionCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpFunction>> _functionsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpFunctionPointer FunctionPointer)?> _sharedFunctionPointerCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpFunctionPointer>> _functionPointersByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpStruct Struct)?> _sharedStructCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpStruct>> _structsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpAliasStruct AliasStruct)?> _sharedAliasStructsCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpAliasStruct>> _aliasStructsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpOpaqueStruct OpaqueStruct)?> _sharedOpaqueStructCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpOpaqueStruct>> _opaqueStructsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpEnum Enum)?> _sharedEnumCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpEnum>> _enumsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpPseudoEnum PseudoEnum)?> _sharedPseudoEnumCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpPseudoEnum>> _pseudoEnumsByPlatform = new();

    private readonly Dictionary<string, (RuntimePlatform Platform, CSharpConstant Constant)?> _sharedConstantCandidates = new();
    private readonly Dictionary<RuntimePlatform, List<CSharpConstant>> _constantsByPlatform = new();

    public void Add(RuntimePlatform platform, CSharpNodes nodes)
    {
        _platforms.Add(platform);
        AddFunctions(platform, nodes.Functions);
        AddFunctionPointers(platform, nodes.FunctionPointers);
        AddStructs(platform, nodes.Structs);
        AddAliasStructs(platform, nodes.AliasStructs);
        AddOpaqueStructs(platform, nodes.OpaqueStructs);
        AddEnums(platform, nodes.Enums);
        AddPseudoEnums(platform, nodes.PseudoEnums);
        AddConstants(platform, nodes.Constants);
    }

    public CSharpAbstractSyntaxTree Build()
    {
        var sharedNodes = BuildSharedNodes();
        var platformSpecificNodes = PlatformSpecificNodes();

        var ast = new CSharpAbstractSyntaxTree
        {
            SharedNodes = sharedNodes,
            PlatformSpecificNodes = platformSpecificNodes,
        };

        return ast;
    }

    private CSharpNodes BuildSharedNodes()
    {
        var functions = _sharedFunctionCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.Function).ToImmutableArray();
        var functionPointers = _sharedFunctionPointerCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.FunctionPointer).ToImmutableArray();
        var structs = _sharedStructCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.Struct).ToImmutableArray();
        var aliasStructs = _sharedAliasStructsCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.AliasStruct).ToImmutableArray();
        var opaqueStructs = _sharedOpaqueStructCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.OpaqueStruct).ToImmutableArray();
        var enums = _sharedEnumCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.Enum).ToImmutableArray();
        var pseudoEnums = _sharedPseudoEnumCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.PseudoEnum).ToImmutableArray();
        var constants = _sharedConstantCandidates
            .Where(x => x.Value != null).Select(x => x.Value!.Value.Constant).ToImmutableArray();

        var sharedNodes = new CSharpNodes
        {
            Functions = functions,
            FunctionPointers = functionPointers,
            Structs = structs,
            AliasStructs = aliasStructs,
            OpaqueStructs = opaqueStructs,
            Enums = enums,
            PseudoEnums = pseudoEnums,
            Constants = constants
        };

        return sharedNodes;
    }

    private ImmutableArray<(RuntimePlatform Platform, CSharpNodes Nodes)> PlatformSpecificNodes()
    {
        var builder = ImmutableArray.CreateBuilder<(RuntimePlatform, CSharpNodes)>();
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

    private CSharpNodes? BuildPlatformNodes(RuntimePlatform platform)
    {
        var functions = _functionsByPlatform[platform].ToImmutableArray();
        var functionPointers = _functionPointersByPlatform[platform].ToImmutableArray();
        var structs = _structsByPlatform[platform].ToImmutableArray();
        var aliasStructs = _aliasStructsByPlatform[platform].ToImmutableArray();
        var opaqueStructs = _opaqueStructsByPlatform[platform].ToImmutableArray();
        var enums = _enumsByPlatform[platform].ToImmutableArray();
        var pseudoEnums = _pseudoEnumsByPlatform[platform].ToImmutableArray();
        var constants = _constantsByPlatform[platform].ToImmutableArray();

        if (functions.IsDefaultOrEmpty &&
            functionPointers.IsDefaultOrEmpty &&
            structs.IsDefaultOrEmpty &&
            aliasStructs.IsDefaultOrEmpty &&
            opaqueStructs.IsDefaultOrEmpty &&
            enums.IsDefaultOrEmpty &&
            pseudoEnums.IsDefaultOrEmpty &&
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
            PseudoEnums = pseudoEnums,
            Constants = constants
        };

        return nodes;
    }

    private void AddFunctions(
        RuntimePlatform platform, ImmutableArray<CSharpFunction> functions)
    {
        _functionsByPlatform.Add(platform, new List<CSharpFunction>());

        foreach (var function in functions)
        {
            AddFunction(platform, function);
        }
    }

    private void AddFunction(RuntimePlatform platform, CSharpFunction function)
    {
        var sharedCandidates = _sharedFunctionCandidates;
        if (!sharedCandidates.TryGetValue(function.Name, out var entry))
        {
            sharedCandidates.Add(function.Name, (platform, function));
        }
        else
        {
            _functionsByPlatform[platform].Add(function);

            if (entry == null)
            {
                return;
            }

            _functionsByPlatform[entry.Value.Platform].Add(entry.Value.Function);
            sharedCandidates[function.Name] = null;
        }
    }

    private void AddFunctionPointers(
        RuntimePlatform platform, ImmutableArray<CSharpFunctionPointer> functionPointers)
    {
        _functionPointersByPlatform.Add(platform, new List<CSharpFunctionPointer>());

        foreach (var functionPointer in functionPointers)
        {
            AddFunctionPointer(platform, functionPointer);
        }
    }

    private void AddFunctionPointer(RuntimePlatform platform, CSharpFunctionPointer functionPointer)
    {
        var sharedCandidates = _sharedFunctionPointerCandidates;
        if (!sharedCandidates.TryGetValue(functionPointer.Name, out var entry))
        {
            sharedCandidates.Add(functionPointer.Name, (platform, functionPointer));
        }
        else
        {
            _functionPointersByPlatform[platform].Add(functionPointer);

            if (entry == null)
            {
                return;
            }

            _functionPointersByPlatform[entry.Value.Platform].Add(entry.Value.FunctionPointer);
            sharedCandidates[functionPointer.Name] = null;
        }
    }

    private void AddStructs(RuntimePlatform platform, ImmutableArray<CSharpStruct> structs)
    {
        _structsByPlatform.Add(platform, new List<CSharpStruct>());

        foreach (var @struct in structs)
        {
            AddStruct(platform, @struct);
        }
    }

    private void AddStruct(RuntimePlatform platform, CSharpStruct @struct)
    {
        var sharedCandidates = _sharedStructCandidates;
        if (!sharedCandidates.TryGetValue(@struct.Name, out var entry))
        {
            sharedCandidates.Add(@struct.Name, (platform, @struct));
        }
        else
        {
            _structsByPlatform[platform].Add(@struct);

            if (entry == null)
            {
                return;
            }

            _structsByPlatform[entry.Value.Platform].Add(entry.Value.Struct);
            sharedCandidates[@struct.Name] = null;
        }
    }

    private void AddAliasStructs(RuntimePlatform platform, ImmutableArray<CSharpAliasStruct> typedefs)
    {
        _aliasStructsByPlatform.Add(platform, new List<CSharpAliasStruct>());

        foreach (var typedef in typedefs)
        {
            AddAliasStruct(platform, typedef);
        }
    }

    private void AddAliasStruct(RuntimePlatform platform, CSharpAliasStruct aliasStruct)
    {
        var sharedCandidates = _sharedAliasStructsCandidates;
        if (!sharedCandidates.TryGetValue(aliasStruct.Name, out var entry))
        {
            sharedCandidates.Add(aliasStruct.Name, (platform, aliasStruct));
        }
        else
        {
            _aliasStructsByPlatform[platform].Add(aliasStruct);

            if (entry == null)
            {
                return;
            }

            _aliasStructsByPlatform[entry.Value.Platform].Add(entry.Value.AliasStruct);
            sharedCandidates[aliasStruct.Name] = null;
        }
    }

    private void AddOpaqueStructs(RuntimePlatform platform, ImmutableArray<CSharpOpaqueStruct> opaqueDataTypes)
    {
        _opaqueStructsByPlatform.Add(platform, new List<CSharpOpaqueStruct>());

        foreach (var opaqueType in opaqueDataTypes)
        {
            AddOpaqueStruct(platform, opaqueType);
        }
    }

    private void AddOpaqueStruct(RuntimePlatform platform, CSharpOpaqueStruct opaqueStruct)
    {
        var sharedStructOpaqueCandidates = _sharedOpaqueStructCandidates;
        if (!sharedStructOpaqueCandidates.TryGetValue(opaqueStruct.Name, out var entry))
        {
            sharedStructOpaqueCandidates.Add(opaqueStruct.Name, (platform, opaqueStruct));
        }
        else
        {
            _opaqueStructsByPlatform[platform].Add(opaqueStruct);

            if (entry == null)
            {
                return;
            }

            _opaqueStructsByPlatform[entry.Value.Platform].Add(entry.Value.OpaqueStruct);
            sharedStructOpaqueCandidates[opaqueStruct.Name] = null;
        }
    }

    private void AddEnums(RuntimePlatform platform, ImmutableArray<CSharpEnum> enums)
    {
        _enumsByPlatform.Add(platform, new List<CSharpEnum>());

        foreach (var @enum in enums)
        {
            AddEnum(platform, @enum);
        }
    }

    private void AddEnum(RuntimePlatform platform, CSharpEnum @enum)
    {
        var sharedEnumsCandidates = _sharedEnumCandidates;
        if (!sharedEnumsCandidates.TryGetValue(@enum.Name, out var entry))
        {
            sharedEnumsCandidates.Add(@enum.Name, (platform, @enum));
        }
        else
        {
            _enumsByPlatform[platform].Add(@enum);

            if (entry == null)
            {
                return;
            }

            _enumsByPlatform[entry.Value.Platform].Add(entry.Value.Enum);
            sharedEnumsCandidates[@enum.Name] = null;
        }
    }

    private void AddPseudoEnums(RuntimePlatform platform, ImmutableArray<CSharpPseudoEnum> pseudoEnums)
    {
        _pseudoEnumsByPlatform.Add(platform, new List<CSharpPseudoEnum>());

        foreach (var pseudoEnum in pseudoEnums)
        {
            AddPseudoEnum(platform, pseudoEnum);
        }
    }

    private void AddPseudoEnum(RuntimePlatform platform, CSharpPseudoEnum pseudoEnum)
    {
        var sharedPseudoEnumsCandidates = _sharedPseudoEnumCandidates;
        if (!sharedPseudoEnumsCandidates.TryGetValue(pseudoEnum.Name, out var entry))
        {
            sharedPseudoEnumsCandidates.Add(pseudoEnum.Name, (platform, pseudoEnum));
        }
        else
        {
            _pseudoEnumsByPlatform[platform].Add(pseudoEnum);

            if (entry == null)
            {
                return;
            }

            _pseudoEnumsByPlatform[entry.Value.Platform].Add(entry.Value.PseudoEnum);
            sharedPseudoEnumsCandidates[pseudoEnum.Name] = null;
        }
    }

    private void AddConstants(RuntimePlatform platform, ImmutableArray<CSharpConstant> constants)
    {
        _constantsByPlatform.Add(platform, new List<CSharpConstant>());

        foreach (var constant in constants)
        {
            AddConstant(platform, constant);
        }
    }

    private void AddConstant(RuntimePlatform platform, CSharpConstant constant)
    {
        var sharedConstantCandidates = _sharedConstantCandidates;
        if (!sharedConstantCandidates.TryGetValue(constant.Name, out var entry))
        {
            sharedConstantCandidates.Add(constant.Name, (platform, constant));
        }
        else
        {
            _constantsByPlatform[platform].Add(constant);

            if (entry == null)
            {
                return;
            }

            _constantsByPlatform[entry.Value.Platform].Add(entry.Value.Constant);
            sharedConstantCandidates[constant.Name] = null;
        }
    }
}
