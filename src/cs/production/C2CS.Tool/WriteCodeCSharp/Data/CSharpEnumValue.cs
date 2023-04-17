// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpEnumValue : CSharpNode
{
    public readonly long Value;

    public CSharpEnumValue(
        string name,
        int? sizeOf,
        long value,
        ImmutableArray<Attribute> attributes)
        : base(name, sizeOf, attributes)
    {
        Value = value;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpEnumValue other2)
        {
            return false;
        }

        return Value == other2.Value;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, Value);
        return hashCode;
    }
}
