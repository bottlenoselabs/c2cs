// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BindgenCSharp.Data.Model;

public record CSharpStruct(
        string CodeLocationComment,
        CSharpType Type,
        ImmutableArray<CSharpStructField> Fields,
        ImmutableArray<CSharpStruct> NestedStructs)
    : CSharpNode(Type.Name, CodeLocationComment)
{
    public readonly ImmutableArray<CSharpStructField> Fields = Fields;
    public readonly ImmutableArray<CSharpStruct> NestedStructs = NestedStructs;
    public readonly CSharpType Type = Type;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
