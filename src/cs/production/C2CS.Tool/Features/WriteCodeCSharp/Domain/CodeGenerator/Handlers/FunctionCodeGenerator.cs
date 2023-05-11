// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using C2CS.Features.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Handlers;

public class FunctionCodeGenerator : GenerateCodeHandler<CSharpFunction>
{
    public FunctionCodeGenerator(
        ILogger<FunctionCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpFunction node)
    {
        string code;

        var parameterStrings = node.Parameters
            .Select(x => ParameterSelector(context, x));
        var parametersString = string.Join(',', parameterStrings);

        if (!context.Options.IsEnabledLibraryImportAttribute)
        {
            var callingConvention = FunctionCallingConventionDllImport(node.CallingConvention);
            var dllImportParametersString = string.Join(',', "LibraryName", $"EntryPoint = \"{node.CName}\"", callingConvention);

            var attributesString = context.GenerateCodeAttributes(node.Attributes);

            code = $@"
{attributesString}
[DllImport({dllImportParametersString})]
public static extern {node.ReturnTypeInfo.FullName} {node.Name}({parametersString});
";
        }
        else
        {
            var callingConvention = FunctionCallingConventionLibraryImport(node.CallingConvention);
            var attributesString = context.GenerateCodeAttributes(node.Attributes);

            var libraryImportParameters = new List<string> { "LibraryName", $"EntryPoint = \"{node.CName}\"" };
            if (node.ReturnTypeInfo.Name == "string" ||
                parametersString.Contains("string", StringComparison.InvariantCulture))
            {
                libraryImportParameters.Add("StringMarshalling = StringMarshalling.Utf8");
            }

            var libraryImportParametersString = string.Join(",", libraryImportParameters);

            code = $@"
{attributesString}
[LibraryImport({libraryImportParametersString})]
[UnmanagedCallConv(CallConvs = new[] {{ typeof({callingConvention}) }})]
public static partial {node.ReturnTypeInfo.FullName} {node.Name}({parametersString});
";
        }

        var member = context.ParseMemberCode<MethodDeclarationSyntax>(code);
        return member;
    }

    private static string FunctionCallingConventionDllImport(CSharpFunctionCallingConvention callingConvention)
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

    private static string FunctionCallingConventionLibraryImport(CSharpFunctionCallingConvention callingConvention)
    {
        var result = callingConvention switch
        {
            CSharpFunctionCallingConvention.Cdecl => "CallConvCdecl",
            CSharpFunctionCallingConvention.StdCall => "CallConvStdcall",
            CSharpFunctionCallingConvention.FastCall => "CallConvFastcall",
            _ => string.Empty
        };
        return result;
    }

    private static string ParameterSelector(CSharpCodeGeneratorContext context, CSharpFunctionParameter parameter)
    {
        if (!context.Options.IsEnabledPointersAsReferences)
        {
            return $@"{parameter.TypeName} {parameter.Name}";
        }

        var firstPointerIndex = parameter.TypeName.IndexOf('*', StringComparison.InvariantCulture);
        var lastPointerIndex = parameter.TypeName.LastIndexOf('*');
        if ((firstPointerIndex == -1 && lastPointerIndex == -1) || firstPointerIndex != lastPointerIndex)
        {
            return $@"{parameter.TypeName} {parameter.Name}";
        }

        var identifier = parameter.TypeName[..lastPointerIndex];
        if (identifier == "void")
        {
            return $@"{parameter.TypeName} {parameter.Name}";
        }

        var result = $@"ref {identifier} {parameter.Name}";
        return result;
    }
}
