// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed class CSharpFunctionParameter : CSharpNode
{
    public readonly string TypeName;

    public CSharpFunctionParameter(
        TargetPlatform platform,
        string name,
        string codeLocationComment,
        int? sizeOf,
        string typeName)
        : base(platform, name, codeLocationComment, sizeOf)
    {
        TypeName = typeName;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionParameter other2)
        {
            return false;
        }

        var result = TypeName == other2.TypeName;
        return result;
    }
}
