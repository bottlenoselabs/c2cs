// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpFunctionPointerParameter : CSharpNode
{
    public readonly CSharpType Type;

    public CSharpFunctionPointerParameter(string name, string codeLocationComment, int? sizeOf, CSharpType type)
        : base(name, codeLocationComment, sizeOf)
    {
        Type = type;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionPointerParameter other2)
        {
            return false;
        }

        return Type == other2.Type;
    }
}
