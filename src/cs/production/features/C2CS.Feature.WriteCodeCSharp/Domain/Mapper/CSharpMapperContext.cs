// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data.Model;

namespace C2CS.Feature.WriteCodeCSharp.Domain.Mapper;

public class CSharpMapperContext
{
    public readonly TargetPlatform Platform;

    public readonly ImmutableDictionary<string, CType> TypesByName;

    public readonly ImmutableDictionary<string, CRecord> RecordsByName;

    public readonly ImmutableDictionary<string, CFunctionPointer> FunctionPointersByName;

    public CSharpMapperContext(
        TargetPlatform platform,
        ImmutableArray<CType> types,
        ImmutableArray<CRecord> records,
        ImmutableArray<CFunctionPointer> functionPointers)
    {
        Platform = platform;
        TypesByName = types
            .ToImmutableDictionary(x => x.Name);
        RecordsByName = records
            .ToImmutableDictionary(x => x.Name);
        FunctionPointersByName = functionPointers
            .ToImmutableDictionary(x => x.Name);
    }
}
