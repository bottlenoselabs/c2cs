// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Commands.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;

public class OpaqueTypeCodeGenerator : GenerateCodeHandler<CSharpOpaqueType>
{
    public OpaqueTypeCodeGenerator(
        ILogger<OpaqueTypeCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpOpaqueType node)
    {
        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{node.Name}}
                     {
                     }

                     """;

        return context.ParseMemberCode<StructDeclarationSyntax>(code);
    }
}
