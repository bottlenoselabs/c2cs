// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.BindgenCSharp;

public record CSharpNode(
    string? Name,
    string? CodeLocationComment)
{
    public readonly string CodeLocationComment =
        string.IsNullOrEmpty(CodeLocationComment) ? string.Empty : CodeLocationComment;

    public readonly string Name = string.IsNullOrEmpty(Name) ? string.Empty : Name;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return $"{Name} {CodeLocationComment}";
    }
}
