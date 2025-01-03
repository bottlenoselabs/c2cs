// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common.Extensions;
using Microsoft.CodeAnalysis;

namespace C2CS.Tests.Common.Model;

public class CSharpTestType
{
    public string Name { get; }

    public int? SizeOf { get; }

    public CSharpTestType? InnerType { get; }

    public CSharpTestType(ITypeSymbol typeSymbol)
    {
        _ = typeSymbol.IsUnmanagedType.Should().BeTrue();
        _ = typeSymbol.TypeKind.Should().BeOneOf(
            TypeKind.Struct, TypeKind.Pointer);

        Name = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        SizeOf = typeSymbol.SizeOf();

        if (typeSymbol.Kind == SymbolKind.PointerType)
        {
            var typeSymbolPointer = (IPointerTypeSymbol)typeSymbol;
            InnerType = new CSharpTestType(typeSymbolPointer.PointedAtType);
        }
        else
        {
            InnerType = null;
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
