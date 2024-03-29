// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using CAstFfi.Data;

namespace C2CS.Features.WriteCodeCSharp.Domain.Mapper;

public class CSharpCodeMapperContext
{
    public readonly ImmutableDictionary<string, CFunctionPointer> FunctionPointers;

    public readonly ImmutableDictionary<string, CRecord> Records;

    public readonly ImmutableHashSet<string> EnumNames;

    public CSharpCodeMapperContext(
        ImmutableDictionary<string, CRecord> records,
        ImmutableDictionary<string, CFunctionPointer> functionPointers,
        ImmutableHashSet<string> enumNames)
    {
        Records = records;
        FunctionPointers = functionPointers;
        EnumNames = enumNames;
    }
}
