// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.CSharpBindgen;

public readonly struct CSharpType
{
    public readonly string Name;
    public readonly string OriginalName;
    public readonly int SizeOf;
    public readonly int AlignOf;
    public readonly int ArraySize;

    public bool IsArray => ArraySize > 0;

    public CSharpType(
        string name,
        string originalName,
        int sizeOf,
        int alignOf,
        int arraySize)
    {
        Name = name;
        OriginalName = originalName;
        SizeOf = sizeOf;
        AlignOf = alignOf;
        ArraySize = arraySize;
    }

    public override string ToString()
    {
        return Name;
    }
}
