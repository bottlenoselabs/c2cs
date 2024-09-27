// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using bottlenoselabs.Common.Diagnostics;
using c2ffi.Data;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.GenerateCSharpCode;

public sealed class CodeGenerator
{
    private readonly InputSanitized _input;

    private readonly CodeGeneratorDocumentPInvoke _codeGeneratorDocumentPInvoke;
    private readonly CodeGeneratorDocumentAssemblyAttributes _codeGeneratorDocumentAssemblyAttributes;
    private readonly CodeGeneratorDocumentInteropRuntime _codeGeneratorDocumentInteropRuntime;

    public CodeGenerator(
        IServiceProvider services,
        InputSanitized input)
    {
        _input = input;

        _codeGeneratorDocumentPInvoke = services.GetRequiredService<CodeGeneratorDocumentPInvoke>();
        _codeGeneratorDocumentAssemblyAttributes =
            services.GetRequiredService<CodeGeneratorDocumentAssemblyAttributes>();
        _codeGeneratorDocumentInteropRuntime = services.GetRequiredService<CodeGeneratorDocumentInteropRuntime>();
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
        var context = new CodeGeneratorDocumentPInvokeContext(_input);
        var documentPInvoke = _codeGeneratorDocumentPInvoke.Generate(options, context, ffi);
        documents.Add(documentPInvoke);
    }

    private void AddDocumentInteropRuntime(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        if (!_input.IsEnabledGenerateCSharpRuntimeCode)
        {
            return;
        }

        var runtimeCodeDocument = _codeGeneratorDocumentInteropRuntime.Generate(options);
        documents.Add(runtimeCodeDocument);
    }

    private void AddDocumentAssemblyAttributes(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        var documentAssemblyAttributes = _codeGeneratorDocumentAssemblyAttributes.Generate(options);
        documents.Add(documentAssemblyAttributes);
    }
}
