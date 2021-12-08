// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static clang;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

public class ClangTranslationUnitExplorerNode
{
    public readonly ClangLocation Location;
    public readonly CXCursor Cursor;
    public readonly CKind Kind;
    public readonly string? Name;
    public readonly string? TypeName;
    public readonly CXType OriginalType;
    public readonly ClangTranslationUnitExplorerNode? Parent;
    public readonly CXType Type;

    public ClangTranslationUnitExplorerNode(
        CKind kind,
        ClangLocation location,
        ClangTranslationUnitExplorerNode? parent,
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
                // Primitives don't have a location
                Location = new ClangLocation
                {
                    FilePath = string.Empty,
                    FileName = "Builtin",
                    LineColumn = 0,
                    LineNumber = 0,
                    IsBuiltin = true
                };
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
