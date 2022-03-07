// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data.Model;

public record CSharpPseudoEnumValue(
        string Name,
        string CodeLocationComment,
        long Value)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly long Value = Value;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
