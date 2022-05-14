// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed class CSharpStructField : CSharpNode
{
    public readonly string BackingFieldName;
    public readonly bool IsWrapped;
    public readonly int Offset;
    public readonly int Padding;
    public readonly CSharpType Type;

    public CSharpStructField(
        TargetPlatform platform,
        string name,
        string codeLocationComment,
        int? sizeOf,
        CSharpType type,
        int offset,
        int padding,
        bool isWrapped)
        : base(platform, name, codeLocationComment, sizeOf)
    {
        Type = type;
        Offset = offset;
        Padding = padding;
        IsWrapped = isWrapped;
        BackingFieldName = name.StartsWith("@", StringComparison.InvariantCulture) ? $"_{name.Substring(1)}" : $"_{name}";
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpStructField other2)
        {
            return false;
        }

        return BackingFieldName == other2.BackingFieldName &&
               IsWrapped == other2.IsWrapped &&
               Offset == other2.Offset &&
               Padding == other2.Padding &&
               Type == other2.Type;
    }
}
