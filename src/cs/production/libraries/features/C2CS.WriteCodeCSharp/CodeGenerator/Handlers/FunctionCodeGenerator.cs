// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data.CSharp.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.CodeGenerator.Handlers;

public class FunctionCodeGenerator : GenerateCodeHandler<CSharpFunction>
{
    public FunctionCodeGenerator(
        ILogger<FunctionCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpFunction node)
    {
        var callingConvention = FunctionCallingConvention(node.CallingConvention);
        var dllImportParameters = string.Join(',', "LibraryName", callingConvention);

        var parameterStrings = node.Parameters.Select(
            x => $@"{x.TypeName} {x.Name}");
        var parameters = string.Join(',', parameterStrings);

        var attributesString = context.GenerateCodeAttributes(node.Attributes);

        var code = $@"
{attributesString}
[DllImport({dllImportParameters})]
public static extern {node.ReturnTypeInfo.Name} {node.Name}({parameters});
";

        var member = context.ParseMemberCode<MethodDeclarationSyntax>(code);
        return member;
    }

    private static string FunctionCallingConvention(CSharpFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CSharpFunctionCallingConvention.Cdecl => "CallingConvention = CallingConvention.Cdecl",
            CSharpFunctionCallingConvention.StdCall => "CallingConvention = CallingConvention.StdCall",
            CSharpFunctionCallingConvention.FastCall => "CallingConvention = CallingConvention.FastCall",
            _ => string.Empty
        };
        return result;
    }
}
