// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Data.CSharp.Model;

public sealed class CSharpEnum : CSharpNode
{
    public readonly CSharpType IntegerType;

    public readonly ImmutableArray<CSharpEnumValue> Values;

    public CSharpEnum(
        ImmutableArray<TargetPlatform> platforms,
        string name,
        string cKind,
        string cCodeLocation,
        CSharpType integerType,
        ImmutableArray<CSharpEnumValue> values,
        ImmutableArray<Attribute> attributes)
        : base(platforms, name, cKind, cCodeLocation, integerType.SizeOf, attributes)
    {
        IntegerType = integerType;
        Values = values;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpEnum other2)
        {
            return false;
        }

        return IntegerType == other2.IntegerType &&
               Values.SequenceEqual(other2.Values);
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var integerTypeHashCode = IntegerType.GetHashCode();
        var valuesHashCode = Values.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, integerTypeHashCode, valuesHashCode);
        return hashCode;
    }
}
