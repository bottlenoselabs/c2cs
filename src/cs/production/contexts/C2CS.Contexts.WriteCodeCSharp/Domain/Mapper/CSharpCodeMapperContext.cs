// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.Mapper;

public class CSharpCodeMapperContext
{
    public readonly TargetPlatform Platform;

    public readonly ImmutableDictionary<string, CRecord> Records;

    public readonly ImmutableDictionary<string, CFunctionPointer> FunctionPointers;

    public CSharpCodeMapperContext(
        TargetPlatform platform,
        ImmutableDictionary<string, CRecord> records,
        ImmutableDictionary<string, CFunctionPointer> functionPointers)
    {
        Platform = platform;
        Records = records;
        FunctionPointers = functionPointers;
    }
}
