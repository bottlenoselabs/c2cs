// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Model;

public class ClangExplorerNode
{
    public readonly CXCursor Cursor;
    public readonly CKind Kind;
    public readonly CLocation Location;
    public readonly string? Name;
    public readonly CXType OriginalType;
    public readonly ClangExplorerNode? Parent;
    public readonly CXType Type;
    public readonly string? TypeName;

    public ClangExplorerNode(
        CKind kind,
        CLocation location,
        ClangExplorerNode? parent,
        CXCursor cursor,
        CXType type,
        CXType originalType,
        string? name,
        string? typeName)
    {
        Kind = kind;

        if (string.IsNullOrEmpty(location.FileName))
        {
            if (type.IsPrimitive())
            {
                Location = CLocation.BuiltIn;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        else
        {
            Location = location;
        }

        Parent = parent;
        Cursor = cursor;
        Type = type;
        OriginalType = originalType;
        Name = name;
        TypeName = typeName;
    }

    public override string ToString()
    {
        return Location.ToString();
    }
}
