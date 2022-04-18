// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.WriteCodeCSharp.Data.Model;

public sealed class CSharpFunctionParameter : CSharpNode
{
    public readonly CSharpType Type;

    public CSharpFunctionParameter(
        TargetPlatform platform,
        string name,
        string codeLocationComment,
        int? sizeOf,
        CSharpType type)
        : base(platform, name, codeLocationComment, sizeOf)
    {
        Type = type;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionParameter other2)
        {
            return false;
        }

        var result = Type == other2.Type;
        return result;
    }
}