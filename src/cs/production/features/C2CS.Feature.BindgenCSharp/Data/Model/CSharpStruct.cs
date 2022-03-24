// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpStruct : CSharpNode
{
    public readonly ImmutableArray<CSharpStructField> Fields;
    public readonly ImmutableArray<CSharpStruct> NestedStructs;
    public readonly CSharpType Type;

    public CSharpStruct(
        string codeLocationComment,
        int? sizeOf,
        CSharpType type,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<CSharpStruct> nestedStructs)
        : base(type.Name, codeLocationComment, sizeOf)
    {
        Fields = fields;
        NestedStructs = nestedStructs;
        Type = type;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpStruct other2)
        {
            return false;
        }

        return Type == other2.Type &&
               Fields.SequenceEqual(other2.Fields) &&
               NestedStructs.SequenceEqual(other2.NestedStructs);
    }
}
