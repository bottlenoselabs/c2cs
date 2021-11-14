// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.CSharpBindgen;

public record CSharpEnumValue : CSharpNode
{
    public readonly long Value;

    public CSharpEnumValue(
        string name,
        string locationComment,
        long value)
        : base(name, locationComment)
    {
        Value = value;
    }

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
