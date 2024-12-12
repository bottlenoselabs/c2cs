// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using c2ffi.Data;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeFunction(
    ILogger<CodeGeneratorNodeFunction> logger,
    NameMapper nameMapper) : CodeGeneratorNodeBase<CFunction>(logger, nameMapper)
{
    protected override SyntaxNode GenerateCode(
        string nameCSharp,
        CodeGeneratorDocumentPInvokeContext context,
        CFunction node)
    {
        string code;

        var returnTypeNameCSharp = NameMapper.GetTypeNameCSharp(node.ReturnType);
        var parametersStringCSharp = string.Join(',', node.Parameters.Select(ParameterStringCSharp));

        if (!context.IsEnabledLibraryImportAttribute)
        {
            var callingConvention = FunctionCallingConventionDllImport(node.CallingConvention);
            var dllImportParametersString = string.Join(',', "LibraryName", $"EntryPoint = \"{nameCSharp}\"", callingConvention);

            code = $"""

                    [DllImport({dllImportParametersString})]
                    public static extern {returnTypeNameCSharp} {nameCSharp}({parametersStringCSharp});

                    """;
        }
        else
        {
            var callingConvention = FunctionCallingConventionLibraryImport(node.CallingConvention);

            var libraryImportParameters = new List<string> { "LibraryName", $"EntryPoint = \"{nameCSharp}\"" };
            if (node.ReturnType.Name == "string" ||
                parametersStringCSharp.Contains("string", StringComparison.InvariantCulture))
            {
                libraryImportParameters.Add("StringMarshalling = StringMarshalling.Utf8");
            }

            var libraryImportParametersString = string.Join(",", libraryImportParameters);

            code = $$"""

                     [LibraryImport({{libraryImportParametersString}})]
                     [UnmanagedCallConv(CallConvs = new[] { typeof({{callingConvention}}) })]
                     public static partial {{returnTypeNameCSharp}} {{nameCSharp}}({{parametersStringCSharp}});

                     """;
        }

        return ParseMemberCode<MethodDeclarationSyntax>(code);
    }

    private static string FunctionCallingConventionDllImport(CFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CFunctionCallingConvention.Cdecl => "CallingConvention = CallingConvention.Cdecl",
            CFunctionCallingConvention.StdCall => "CallingConvention = CallingConvention.StdCall",
            CFunctionCallingConvention.FastCall => "CallingConvention = CallingConvention.FastCall",
            CFunctionCallingConvention.Unknown => throw new NotImplementedException(),
            _ => string.Empty
        };
        return result;
    }

    private static string FunctionCallingConventionLibraryImport(CFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CFunctionCallingConvention.Cdecl => "CallConvCdecl",
            CFunctionCallingConvention.StdCall => "CallConvStdcall",
            CFunctionCallingConvention.FastCall => "CallConvFastcall",
            CFunctionCallingConvention.Unknown => throw new NotImplementedException(),
            _ => string.Empty
        };
        return result;
    }

    private string ParameterStringCSharp(CFunctionParameter parameter)
    {
        var parameterTypeC = parameter.Type;
        var parameterTypeNameCSharp = NameMapper.GetTypeNameCSharp(parameterTypeC);
        return $"{parameterTypeNameCSharp} {parameter.Name}";
    }
}
