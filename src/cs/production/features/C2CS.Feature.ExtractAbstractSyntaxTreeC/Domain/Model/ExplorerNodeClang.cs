// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;

public class ExplorerNodeClang
{
    public readonly CXCursor Cursor;
    public readonly CKind Kind;
    public readonly CLocation Location;
    public readonly string? Name;
    public readonly CXType OriginalType;
    public readonly ExplorerNodeClang? Parent;
    public readonly CXType Type;
    public readonly string? TypeName;

    public ExplorerNodeClang(
        CKind kind,
        CLocation location,
        ExplorerNodeClang? parent,
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
