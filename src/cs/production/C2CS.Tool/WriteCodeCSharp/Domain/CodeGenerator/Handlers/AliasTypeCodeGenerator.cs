// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.Domain.CodeGenerator.Handlers;

public class AliasTypeCodeGenerator : GenerateCodeHandler<CSharpAliasType>
{
    public AliasTypeCodeGenerator(
        ILogger<AliasTypeCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpAliasType node)
    {
        var attributesString = context.GenerateCodeAttributes(node.Attributes);

        var code = $@"
{attributesString}
[StructLayout(LayoutKind.Explicit, Size = {node.UnderlyingTypeInfo.SizeOf}, Pack = {node.UnderlyingTypeInfo.AlignOf})]
public struct {node.Name}
{{
	[FieldOffset(0)]
    public {node.UnderlyingTypeInfo.Name} Data;

	public static implicit operator {node.UnderlyingTypeInfo.Name}({node.Name} data) => data.Data;
	public static implicit operator {node.Name}({node.UnderlyingTypeInfo.Name} data) => new() {{Data = data}};
}}
";

        var member = context.ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }
}
