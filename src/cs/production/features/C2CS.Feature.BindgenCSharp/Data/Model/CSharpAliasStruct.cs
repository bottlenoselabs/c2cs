// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpAliasStruct : CSharpNode
{
    public readonly CSharpType UnderlyingType;

    public CSharpAliasStruct(
        string name,
        string codeLocationComment,
        int? sizeOf,
        CSharpType underlyingType)
        : base(name, codeLocationComment, sizeOf)
    {
        UnderlyingType = underlyingType;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpAliasStruct other2)
        {
            return false;
        }

        return UnderlyingType == other2.UnderlyingType;
    }
}
