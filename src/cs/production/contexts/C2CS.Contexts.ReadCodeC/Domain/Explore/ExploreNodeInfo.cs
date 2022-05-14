// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;

#pragma warning disable CA1815
public class ExploreInfoNode
#pragma warning restore CA1815
{
    public CKind Kind { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TypeName { get; set; } = string.Empty;

    public CXCursor Cursor { get; set; }

    public CXType Type { get; set; }

    public CLocation Location { get; set; }

    public int SizeOf { get; set; }

    public int AlignOf { get; set; }

    public ExploreInfoNode? Parent { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
