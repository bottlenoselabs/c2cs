// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestStructLayout
{
    public string LayoutKind { get; }

    public int? SizeOf { get; }

    public int? PackOf { get; }

    public CSharpTestStructLayout(StructDeclarationSyntax syntaxNode)
    {
        var attribute = syntaxNode.TryGetAttribute("StructLayout");
        var arguments = attribute?.ArgumentList?.Arguments;
        _ = arguments.Should().NotBeNull();

        int? sizeOf;
        int? packOf;

        LayoutKind = arguments!.Value[0].Expression.ToFullString();
        if (LayoutKind == "LayoutKind.Explicit")
        {
            var sizeOfString = arguments.Value[1].Expression.ToFullString();
            sizeOf = int.Parse(sizeOfString, CultureInfo.InvariantCulture);
            var packOfString = arguments.Value[2].Expression.ToFullString();
            packOf = int.Parse(packOfString, CultureInfo.InvariantCulture);
        }
        else
        {
            sizeOf = null;
            packOf = null;
        }

        SizeOf = sizeOf;
        PackOf = packOf;
    }

    public override string ToString()
    {
        return LayoutKind;
    }
}
