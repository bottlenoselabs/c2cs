// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.BindgenCSharp;

public record CSharpFunctionPointerParameter(
        string Name,
        string CodeLocationComment,
        CSharpType Type)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly CSharpType Type = Type;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
