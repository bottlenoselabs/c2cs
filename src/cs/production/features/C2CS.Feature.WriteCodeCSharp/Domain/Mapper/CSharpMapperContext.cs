// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data.Model;

namespace C2CS.Feature.WriteCodeCSharp.Domain.Mapper;

public class CSharpMapperContext
{
    public readonly TargetPlatform Platform;

    public readonly ImmutableDictionary<string, CType> Types;

    public readonly ImmutableDictionary<string, CRecord> Records;

    public readonly ImmutableDictionary<string, CFunctionPointer> FunctionPointers;

    public CSharpMapperContext(
        TargetPlatform platform,
        ImmutableDictionary<string, CType> types,
        ImmutableDictionary<string, CRecord> records,
        ImmutableDictionary<string, CFunctionPointer> functionPointers)
    {
        Platform = platform;
        Types = types;
        Records = records;
        FunctionPointers = functionPointers;
    }
}
