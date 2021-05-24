// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.UseCases.Bindgen
{
    public class BindgenUseCase : UseCase<BindgenInput, BindgenOutput, BindgenState>
    {
        public BindgenUseCase()
            : base(new UseCaseStep[]
            {
                new("Parse C code from disk", ParseCCode),
                new("Extract C abstract syntax tree", ExploreCCode),
                new("Transpile C to C#", MapCToCSharp),
                new("Generate C# code", GenerateCSharpCode),
                new("Write C# code to disk", WriteCSharpCode)
            })
        {
        }

        private static void ParseCCode(BindgenInput input, ref BindgenState state, DiagnosticsSink diagnostics)
        {
            state.ClangTranslationUnit = ClangParser.ParseTranslationUnit(input.InputFilePath, input.ClangArgs);
        }

        private static void ExploreCCode(BindgenInput input, ref BindgenState state, DiagnosticsSink diagnostics)
        {
            var clangExplorer = new ClangExplorer(diagnostics);
            state.ClangAbstractSyntaxTree = clangExplorer.AbstractSyntaxTree(
                state.ClangTranslationUnit,
                input.PrintAbstractSyntaxTree,
                input.OpaqueTypes);
        }

        private static void MapCToCSharp(BindgenInput input, ref BindgenState state, DiagnosticsSink diagnostics)
        {
            state.CSharpAbstractSyntaxTree = CSharpMapper.GetAbstractSyntaxTree(state.ClangAbstractSyntaxTree);
        }

        private static void GenerateCSharpCode(BindgenInput input, ref BindgenState state, DiagnosticsSink diagnostics)
        {
            state.GeneratedCSharpCode = CSharpCodeGenerator.GenerateFile(
                input.ClassName, input.LibraryName, state.CSharpAbstractSyntaxTree);
        }

        private static void WriteCSharpCode(BindgenInput input, ref BindgenState state, DiagnosticsSink diagnostics)
        {
            File.WriteAllText(input.OutputFilePath, state.GeneratedCSharpCode);
            Console.WriteLine(input.OutputFilePath);
        }

        protected override void Finish(BindgenInput input, BindgenOutput output, BindgenState state)
        {
            output.OutputFilePath = input.OutputFilePath;
        }
    }
}
