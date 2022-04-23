// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Model;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

internal sealed class ClangTranslationUnitExplorerNode
{
    public readonly CKind Kind;
    public readonly CLocation Location;
    public readonly ClangTranslationUnitExplorerNode? Parent;
    public readonly CXCursor Cursor;
    public readonly CXType Type;
    public readonly string? CursorName;
    public readonly string? TypeName;

    public ClangTranslationUnitExplorerNode(
        CKind kind,
        CLocation location,
        ClangTranslationUnitExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        string? cursorName,
        string? typeName)
    {
        Kind = kind;
        Location = location;
        Parent = parent;
        Cursor = cursor;
        Type = type;
        CursorName = cursorName;
        TypeName = typeName;
    }

    public override string ToString()
    {
        return Location.ToString();
    }
}
