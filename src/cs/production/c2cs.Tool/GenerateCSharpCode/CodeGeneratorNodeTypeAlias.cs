// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public class CodeGeneratorNodeTypeAlias : CodeGeneratorNodeBase<CTypeAlias>
{
    public CodeGeneratorNodeTypeAlias(
        ILogger<CodeGeneratorNodeTypeAlias> logger,
        NameMapper nameMapper)
        : base(logger, nameMapper)
    {
    }

    protected override SyntaxNode GenerateCode(
        string nameCSharp, CodeGeneratorDocumentPInvokeContext context, CTypeAlias node)
    {
        var code = $$"""

                     [StructLayout(LayoutKind.Explicit, Size = {{node.UnderlyingType.SizeOf}}, Pack = {{node.UnderlyingType.AlignOf}})]
                     public struct {{nameCSharp}}
                     {
                     	[FieldOffset(0)]
                         public {{node.UnderlyingType.Name}} Data;

                     	public static implicit operator {{node.UnderlyingType.Name}}({{nameCSharp}} data) => data.Data;
                     	public static implicit operator {{nameCSharp}}({{node.UnderlyingType.Name}} data) => new {{nameCSharp}}() {Data = data};
                     }

                     """;

        return ParseMemberCode<StructDeclarationSyntax>(code);
    }
}
