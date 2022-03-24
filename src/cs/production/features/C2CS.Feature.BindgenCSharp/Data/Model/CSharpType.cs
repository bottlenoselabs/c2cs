// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed record CSharpType
{
    public string? Name { get; init; }

    public string? OriginalName { get; init; }

    public int SizeOf { get; init; }

    public int AlignOf { get; init; }

    public int ArraySize { get; init; }

    public bool IsArray => ArraySize > 0;

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? string.Empty : Name;
    }
}
