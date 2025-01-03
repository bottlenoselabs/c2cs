// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using bottlenoselabs.Common.Diagnostics;
using C2CS.GenerateCSharpCode.Generators;
using c2ffi.Data;
using c2ffi.Data.Nodes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public sealed partial class CodeGenerator
{
    private readonly ILogger<CodeGenerator> _logger;

    private readonly CodeGeneratorDocumentPInvoke _codeGeneratorDocumentPInvoke;
    private readonly CodeGeneratorDocumentAssemblyAttributes _codeGeneratorDocumentAssemblyAttributes;
    private readonly CodeGeneratorDocumentInteropRuntime _codeGeneratorDocumentInteropRuntime;

    private readonly InputSanitized _input;
    private readonly ImmutableDictionary<Type, BaseGenerator> _nodeCodeGenerators;

    public CodeGenerator(
        IServiceProvider services,
        InputSanitized input)
    {
        _logger = services.GetRequiredService<ILogger<CodeGenerator>>();
        _input = input;
        _codeGeneratorDocumentPInvoke = services.GetRequiredService<CodeGeneratorDocumentPInvoke>();
        _codeGeneratorDocumentAssemblyAttributes = services.GetRequiredService<CodeGeneratorDocumentAssemblyAttributes>();
        _codeGeneratorDocumentInteropRuntime = services.GetRequiredService<CodeGeneratorDocumentInteropRuntime>();

        _nodeCodeGenerators = new Dictionary<Type, BaseGenerator>
        {
            { typeof(CEnum), services.GetRequiredService<GeneratorEnum>() },
            { typeof(CFunction), services.GetRequiredService<GeneratorFunction>() },
            { typeof(CFunctionPointer), services.GetRequiredService<GeneratorFunctionPointer>() },
            { typeof(CMacroObject), services.GetRequiredService<GeneratorMacroObject>() },
            { typeof(COpaqueType), services.GetRequiredService<GeneratorOpaqueType>() },
            { typeof(CRecord), services.GetRequiredService<GeneratorStruct>() },
            { typeof(CTypeAlias), services.GetRequiredService<GeneratorAliasType>() },
        }.ToImmutableDictionary();
    }

    public CodeProject GenerateCodeProject(CFfiCrossPlatform ffi, DiagnosticsSink diagnostics)
    {
        try
        {
            var options = new CodeGeneratorDocumentOptions(_input);
            var documents = ImmutableArray.CreateBuilder<CodeProjectDocument>();

            AddDocumentPInvoke(options, documents, ffi);
            AddDocumentInteropRuntime(options, documents);
            AddDocumentAssemblyAttributes(options, documents);

            return new CodeProject
            {
                Documents = documents.ToImmutable()
            };
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            diagnostics.Add(new DiagnosticPanic(e));
            return new CodeProject();
        }
    }

    private void AddDocumentPInvoke(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents,
        CFfiCrossPlatform ffi)
    {
        var context = new CodeGeneratorContext(_input, _nodeCodeGenerators);
        var document = _codeGeneratorDocumentPInvoke.Generate(options, context, ffi);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    private void AddDocumentInteropRuntime(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        if (!_input.IsEnabledGenerateCSharpRuntimeCode)
        {
            return;
        }

        var document = _codeGeneratorDocumentInteropRuntime.Generate(options);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    private void AddDocumentAssemblyAttributes(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        var document = _codeGeneratorDocumentAssemblyAttributes.Generate(options);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    [LoggerMessage(0, LogLevel.Information, "- '{FileName}': {LinesOfCodeCount} lines of code")]
    private partial void LogGeneratedCodeProjectDocument(string fileName, int linesOfCodeCount);
}
