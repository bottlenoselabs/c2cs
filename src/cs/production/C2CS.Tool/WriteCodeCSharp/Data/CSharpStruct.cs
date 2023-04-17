// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Foundation;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpStruct : CSharpNode
{
    public readonly int AlignOf;
    public readonly ImmutableArray<CSharpStructField> Fields;
    public readonly ImmutableArray<CSharpStruct> NestedStructs;

    public CSharpStruct(
        string name,
        int sizeOf,
        int alignOf,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<CSharpStruct> nestedStructs,
        ImmutableArray<Attribute> attributes)
        : base(name, sizeOf, attributes)
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

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var fieldsHashCode = Fields.GetHashCodeMembers();
        var nestedStructsHashCode = NestedStructs.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, fieldsHashCode, nestedStructsHashCode);
        return hashCode;
    }
}
