// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common;

public static class Extensions
{
    public static AttributeSyntax? TryGetAttribute(this MemberDeclarationSyntax syntaxNode, string name)
    {
        AttributeSyntax? result = null;

        foreach (var attributeList in syntaxNode.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == name)
                {
                    result = attribute;
                }
            }
        }

        return result;
    }

    public static int? SizeOf(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.Kind == SymbolKind.PointerType)
        {
            return IntPtr.Size;
        }

        int? sizeOf = 0;
        var typeMembers = typeSymbol.GetTypeMembers();
        if (typeMembers.IsDefaultOrEmpty)
        {
#pragma warning disable IDE0010
            switch (typeSymbol.SpecialType)
#pragma warning restore IDE0010
            {
                case SpecialType.System_Void:
                    sizeOf = null;
                    break;
                case SpecialType.System_Byte:
                    sizeOf = 1;
                    break;
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                    sizeOf += 2;
                    break;
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                    sizeOf += 4;
                    break;
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                    sizeOf += 8;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        else
        {
            foreach (var typeMember in typeMembers)
            {
                var typeMemberSizeOf = SizeOf(typeMember);
                sizeOf += typeMemberSizeOf;
            }
        }

        return sizeOf;
    }
}
