// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed class CSharpStruct : CSharpNode
{
    public readonly ImmutableArray<CSharpStructField> Fields;
    public readonly ImmutableArray<CSharpStruct> NestedStructs;

    public int AlignOf;

    public CSharpStruct(
        string name,
        TargetPlatform platform,
        string codeLocationComment,
        int sizeOf,
        int alignOf,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<CSharpStruct> nestedStructs)
        : base(platform, name, codeLocationComment, sizeOf)
    {
        AlignOf = alignOf;
        Fields = fields;
        NestedStructs = nestedStructs;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpStruct other2)
        {
            return false;
        }

        return Fields.SequenceEqual(other2.Fields) &&
               NestedStructs.SequenceEqual(other2.NestedStructs);
    }
}
