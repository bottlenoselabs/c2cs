// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Commands.WriteCodeCSharp.Data;

public sealed class CSharpStructField : CSharpNode
{
    public readonly string BackingFieldName;
    public readonly int OffsetOf;
    public readonly CSharpType Type;

    public CSharpStructField(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpType type,
        int offsetOf)
        : base(name, className, cName, sizeOf)
    {
        Type = type;
        OffsetOf = offsetOf;
        BackingFieldName = name.StartsWith('@') ? $"_{name[1..]}" : $"_{name}";
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpStructField other2)
        {
            return false;
        }

        return BackingFieldName == other2.BackingFieldName &&
               OffsetOf == other2.OffsetOf &&
               Type == other2.Type;
    }

    public override int GetHashCode()
    {
        var baseHashCOde = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCOde, BackingFieldName, OffsetOf, Type);
        return hashCode;
    }
}
