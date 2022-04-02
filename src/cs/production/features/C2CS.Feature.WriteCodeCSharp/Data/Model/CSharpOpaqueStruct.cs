// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.WriteCodeCSharp.Data.Model;

public sealed class CSharpOpaqueStruct : CSharpNode
{
    public CSharpOpaqueStruct(
        string name,
        string codeLocationComment,
        int? sizeOf)
        : base(name, codeLocationComment, sizeOf)
    {
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpOpaqueStruct)
        {
            return false;
        }

        return true;
    }
}
