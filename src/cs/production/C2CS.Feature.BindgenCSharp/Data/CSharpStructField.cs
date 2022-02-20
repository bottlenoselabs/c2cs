// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data;

public record CSharpStructField : CSharpNode
{
    public readonly string BackingFieldName;
    public readonly bool IsWrapped;
    public readonly int Offset;
    public readonly int Padding;
    public readonly CSharpType Type;

    public CSharpStructField(
        string name,
        string codeLocationComment,
        CSharpType type,
        int offset,
        int padding,
        bool isWrapped)
        : base(name, codeLocationComment)
    {
        Type = type;
        Offset = offset;
        Padding = padding;
        IsWrapped = isWrapped;
        BackingFieldName = name.StartsWith("@", StringComparison.InvariantCulture) ? $"_{name[1..]}" : $"_{name}";
    }

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
