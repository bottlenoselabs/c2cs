// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Model;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

internal sealed class ClangTranslationUnitExplorerNode
{
    public readonly CXCursor Cursor;
    public readonly CKind Kind;
    public readonly CLocation Location;
    public readonly string? Name;
    public readonly ClangTranslationUnitExplorerNode? Parent;
    public readonly CXType Type;
    public readonly string? TypeName;

    public ClangTranslationUnitExplorerNode(
        CKind kind,
        CLocation location,
        ClangTranslationUnitExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        string? name,
        string? typeName)
    {
        Kind = kind;
        Location = location;
        Parent = parent;
        Cursor = cursor;
        Type = type;
        Name = name;
        TypeName = typeName;
    }

    public override string ToString()
    {
        return Location.ToString();
    }
}
