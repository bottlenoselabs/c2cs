// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Foundation;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpEnum : CSharpNode
{
    public readonly CSharpTypeInfo IntegerTypeInfo;

    public readonly ImmutableArray<CSharpEnumValue> Values;

    public CSharpEnum(
        string name,
        CSharpTypeInfo integerTypeInfo,
        ImmutableArray<CSharpEnumValue> values,
        ImmutableArray<Attribute> attributes)
        : base(name, integerTypeInfo.SizeOf, attributes)
    {
        IntegerTypeInfo = integerTypeInfo;
        Values = values;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpEnum other2)
        {
            return false;
        }

        return IntegerTypeInfo == other2.IntegerTypeInfo &&
               Values.SequenceEqual(other2.Values);
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var integerTypeHashCode = IntegerTypeInfo.GetHashCode();
        var valuesHashCode = Values.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, integerTypeHashCode, valuesHashCode);
        return hashCode;
    }
}
