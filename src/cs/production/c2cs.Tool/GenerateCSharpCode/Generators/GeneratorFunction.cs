// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Linq;
using c2ffi.Data;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorFunction(ILogger<GeneratorFunction> logger)
    : BaseGenerator<CFunction>(logger)
{
    public override string GenerateCode(CodeGeneratorContext context, string nameCSharp, CFunction function)
    {
        var returnTypeNameCSharp = context.NameMapper.GetTypeNameCSharp(function.ReturnType);
        var parametersStringCSharp = string.Join(',', function.Parameters.Select(
            (x, i) => ParameterStringCSharp(context.NameMapper, x, i)));

        var code = context.Input.IsEnabledLibraryImportAttribute ?
            GenerateCodeLibraryImport(nameCSharp, function, parametersStringCSharp, returnTypeNameCSharp) :
            GenerateCodeDllImport(nameCSharp, function, returnTypeNameCSharp, parametersStringCSharp);

        return code;
    }

    private static string GenerateCodeLibraryImport(
        string nameCSharp,
        CFunction node,
        string parametersStringCSharp,
        string returnTypeNameCSharp)
    {
        var callingConvention = FunctionCallingConventionLibraryImport(node.CallingConvention);
        var libraryImportParameters = new List<string> { "LibraryName", $"EntryPoint = \"{nameCSharp}\"" };
        if (node.ReturnType.Name == "string" ||
            parametersStringCSharp.Contains("string", StringComparison.InvariantCulture))
        {
            libraryImportParameters.Add("StringMarshalling = StringMarshalling.Utf8");
        }

        var libraryImportParametersString = string.Join(",", libraryImportParameters);

        var code = $$"""
                     [LibraryImport({{libraryImportParametersString}})]
                     [UnmanagedCallConv(CallConvs = new[] { typeof({{callingConvention}}) })]
                     public static partial {{returnTypeNameCSharp}} {{nameCSharp}}({{parametersStringCSharp}});
                     """;
        return code;
    }

    private static string GenerateCodeDllImport(
        string nameCSharp,
        CFunction node,
        string returnTypeNameCSharp,
        string parametersStringCSharp)
    {
        var callingConvention = FunctionCallingConventionDllImport(node.CallingConvention);
        var dllImportParametersString = string.Join(',', "LibraryName", $"EntryPoint = \"{nameCSharp}\"", callingConvention);

        var code = $"""
                    [DllImport({dllImportParametersString})]
                    public static extern {returnTypeNameCSharp} {nameCSharp}({parametersStringCSharp});
                    """;
        return code;
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

    private string ParameterStringCSharp(NameMapper nameMapper, CFunctionParameter parameter, int parameterIndex)
    {
        var parameterName = nameMapper.GetIdentifierCSharp(
            !string.IsNullOrEmpty(parameter.Name) ? parameter.Name : $"unnamed{parameterIndex}");
        var parameterTypeC = parameter.Type;
        var parameterTypeNameCSharp = nameMapper.GetTypeNameCSharp(parameterTypeC);
        return $"{parameterTypeNameCSharp} {parameterName}";
    }
}
