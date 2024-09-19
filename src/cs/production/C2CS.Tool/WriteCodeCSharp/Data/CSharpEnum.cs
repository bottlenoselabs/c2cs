// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using C2CS.Foundation;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpEnum : CSharpNode
{
    public readonly ImmutableArray<CSharpEnumValue> Values;

    public CSharpEnum(
        string name,
        string className,
        string cName,
        int sizeOf,
        ImmutableArray<CSharpEnumValue> values)
        : base(name, className, cName, sizeOf)
    {
        Values = values;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpEnum other2)
        {
            return false;
        }

        return SizeOf == other2.SizeOf;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var sizeOfHashCode = SizeOf.GetHashCode();
        var valuesHashCode = Values.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, sizeOfHashCode, valuesHashCode);
        return hashCode;
    }
}
