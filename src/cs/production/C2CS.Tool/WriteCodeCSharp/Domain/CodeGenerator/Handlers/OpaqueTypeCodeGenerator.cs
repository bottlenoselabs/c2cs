// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.Domain.CodeGenerator.Handlers;

public class OpaqueTypeCodeGenerator : GenerateCodeHandler<CSharpOpaqueType>
{
    public OpaqueTypeCodeGenerator(
        ILogger<OpaqueTypeCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpOpaqueType node)
    {
        var attributesString = context.GenerateCodeAttributes(node.Attributes);

        var code = $@"
{attributesString}
[StructLayout(LayoutKind.Sequential)]
public struct {node.Name}
{{
}}
";

        return context.ParseMemberCode<StructDeclarationSyntax>(code);
    }
}
