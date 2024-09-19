// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using c2ffi.Data;
using c2ffi.Data.Nodes;

namespace C2CS.WriteCodeCSharp.Mapper;

public class CSharpCodeMapperContext
{
    public readonly ImmutableSortedDictionary<string, CFunctionPointer> FunctionPointers;

    public readonly ImmutableSortedDictionary<string, CRecord> Records;

    public readonly ImmutableHashSet<string> EnumNames;

    public CSharpCodeMapperContext(CFfiCrossPlatform ffi)
    {
        Records = ffi.Records;
        FunctionPointers = ffi.FunctionPointers;
        EnumNames = GetEnumNames(ffi);
    }

    private static ImmutableHashSet<string> GetEnumNames(CFfiCrossPlatform ffi)
    {
        var result = ffi.Enums.Values.Select(GetEnumName).ToImmutableHashSet();
        return result;
    }

    private static string GetEnumName(CEnum @enum)
    {
        var name = @enum.Name;

        if (name.StartsWith("enum ", StringComparison.InvariantCulture))
        {
            name = name.ReplaceFirst("enum ", string.Empty, StringComparison.InvariantCulture);
        }

        return name.TrimEnd('_');
    }
}
