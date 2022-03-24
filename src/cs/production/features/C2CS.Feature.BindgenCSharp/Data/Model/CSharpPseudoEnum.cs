// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpPseudoEnum : CSharpNode
{
    public readonly CSharpType IntegerType;
    public readonly ImmutableArray<CSharpEnumValue> Values;

    public CSharpPseudoEnum(
        string name,
        string codeLocationComment,
        int? sizeOf,
        CSharpType integerType,
        ImmutableArray<CSharpEnumValue> values)
        : base(name, codeLocationComment, sizeOf)
    {
        IntegerType = integerType;
        Values = values;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpPseudoEnum other2)
        {
            return false;
        }

        return IntegerType == other2.IntegerType &&
               Values.SequenceEqual(other2.Values);
    }
}
