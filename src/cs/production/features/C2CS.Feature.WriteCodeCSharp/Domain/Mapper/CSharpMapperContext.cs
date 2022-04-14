// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data;

namespace C2CS.Feature.WriteCodeCSharp.Domain.Mapper;

public class CSharpMapperContext
{
    public readonly TargetPlatform Platform;

    public readonly ImmutableDictionary<string, CType> TypesByName;

    public CSharpMapperContext(TargetPlatform platform, ImmutableArray<CType> types)
    {
        Platform = platform;

        var typesByNameBuilder = ImmutableDictionary.CreateBuilder<string, CType>();
        foreach (var type in types)
        {
            typesByNameBuilder.Add(type.Name, type);
        }

        TypesByName = typesByNameBuilder.ToImmutable();
    }
}
