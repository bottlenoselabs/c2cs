// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpPseudoEnumValue : CSharpNode
{
    public readonly long Value;

    public CSharpPseudoEnumValue(
        string name,
        string codeLocationComment,
        int? sizeOf,
        long value)
        : base(name, codeLocationComment, sizeOf)
    {
        Value = value;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpPseudoEnumValue other2)
        {
            return false;
        }

        return Value == other2.Value;
    }
}
