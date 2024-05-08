// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Commands.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;

public class AliasTypeCodeGenerator : GenerateCodeHandler<CSharpAliasType>
{
    public AliasTypeCodeGenerator(
        ILogger<AliasTypeCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpAliasType node)
    {
        var code = $$"""

                     [StructLayout(LayoutKind.Explicit, Size = {{node.UnderlyingType.SizeOf}}, Pack = {{node.UnderlyingType.AlignOf}})]
                     public struct {{node.Name}}
                     {
                     	[FieldOffset(0)]
                         public {{node.UnderlyingType.Name}} Data;

                     	public static implicit operator {{node.UnderlyingType.Name}}({{node.Name}} data) => data.Data;
                     	public static implicit operator {{node.Name}}({{node.UnderlyingType.Name}} data) => new {{node.Name}}() {Data = data};
                     }

                     """;

        var member = context.ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }
}
