// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;
using C2CS.Foundation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public static class CSharpLibraryCompiler
{
    public static CSharpLibraryCompilerResult? Compile(
        CSharpProject project,
        CSharpCodeGeneratorOptions options,
        DiagnosticCollection diagnostics)
    {
        try
        {
            var cSharpDocuments = project.Documents.Where(x =>
                x.FileName.EndsWith(".cs", StringComparison.InvariantCulture));

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var cSharpDocument in cSharpDocuments)
            {
                var sharpSyntaxTree = CSharpSyntaxTree.ParseText(
                    cSharpDocument.Contents,
                    path: cSharpDocument.FileName,
                    encoding: Encoding.Default);
                syntaxTrees.Add(sharpSyntaxTree);
            }

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithPlatform(Platform.AnyCpu)
                .WithAllowUnsafe(true);

            var dotnetAssembliesDirectoryPath = Path.GetDirectoryName(typeof(Attribute).Assembly.Location)!;
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(dotnetAssembliesDirectoryPath, "System.Runtime.dll")),
            };
            if (!options.IsEnabledGenerateCSharpRuntimeCode)
            {
                references.Add(MetadataReference.CreateFromFile(typeof(bottlenoselabs.C2CS.Runtime.CBool).Assembly.Location));
            }

            var compilation = CSharpCompilation.Create(
                "TestAssemblyName",
                syntaxTrees,
                references,
                compilationOptions);
            using var dllStream = new MemoryStream();
            using var pdbStream = new MemoryStream();
            var emitResult = compilation.Emit(dllStream, pdbStream);
            var assembly = Assembly.Load(dllStream.ToArray());

            foreach (var diagnostic in emitResult.Diagnostics)
            {
                // Obviously errors should be considered, but should warnings be considered too? Yes, yes they should. Some warnings can be indicative of bindings which are not correct.
                var isErrorOrWarning = diagnostic.Severity is
                    Microsoft.CodeAnalysis.DiagnosticSeverity.Error or Microsoft.CodeAnalysis.DiagnosticSeverity.Warning;
                if (!isErrorOrWarning)
                {
                    continue;
                }

                diagnostics.Add(new CSharpCompileDiagnostic(diagnostic.Location.SourceTree?.FilePath ?? string.Empty, diagnostic));
            }

            return new CSharpLibraryCompilerResult(emitResult, assembly);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            diagnostics.Add(new DiagnosticPanic(e));
            return null;
        }
    }
}
