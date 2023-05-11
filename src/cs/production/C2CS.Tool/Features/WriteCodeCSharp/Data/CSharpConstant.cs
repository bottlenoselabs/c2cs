// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpConstant : CSharpNode
{
    public readonly string Type;

    public readonly string Value;

    public CSharpConstant(
        string name,
        string className,
        string cName,
        int? sizeOf,
        string type,
        string value,
        ImmutableArray<Attribute> attributes)
        : base(name, className, cName, sizeOf, attributes)
    {
        Type = type;
        Value = value;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpConstant other2)
        {
            return false;
        }

        return Type == other2.Type &&
               Value == other2.Value;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, Type, Value);
        return hashCode;
    }
}
