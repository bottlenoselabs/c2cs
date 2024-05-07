// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Commands.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;

public sealed class FunctionPointerCodeGenerator : GenerateCodeHandler<CSharpFunctionPointer>
{
    public FunctionPointerCodeGenerator(
        ILogger<FunctionPointerCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpFunctionPointer node)
    {
        string code;
        if (context.Options.IsEnabledFunctionPointers)
        {
            code = GenerateCodeFunctionPointer(context, node);
        }
        else
        {
            code = GenerateCodeDelegate(context, node);
        }

        var result = context.ParseMemberCode<StructDeclarationSyntax>(code);
        return result;
    }

    private string GenerateCodeFunctionPointer(CSharpCodeGeneratorContext context, CSharpFunctionPointer node)
    {
        var parameterTypesString = context.GenerateCodeParameters(node.Parameters, false);
        var parameterTypesAndReturnTypeString = string.IsNullOrEmpty(parameterTypesString) ? node.ReturnType.FullName : $"{parameterTypesString}, {node.ReturnType.FullName}";

        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{node.Name}}
                     {
                     	public delegate* unmanaged<{{parameterTypesAndReturnTypeString}}> Pointer;

                     	public FnPtr_CString_Void(delegate* unmanaged<{{parameterTypesAndReturnTypeString}}> pointer)
                        {
                            Pointer = pointer;
                        }
                     }

                     """;
        return code;
    }

    private string GenerateCodeDelegate(CSharpCodeGeneratorContext context, CSharpFunctionPointer node)
    {
        var parameterTypesString = context.GenerateCodeParameters(node.Parameters);

        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{node.Name}}
                     {
                         [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                         public unsafe delegate {{node.ReturnType.FullName}} @delegate({{parameterTypesString}});

                         public IntPtr Pointer;

                         public {{node.Name}}(@delegate d)
                         {
                             Pointer = Marshal.GetFunctionPointerForDelegate(d);
                         }
                     }

                     """;
        return code;
    }
}
