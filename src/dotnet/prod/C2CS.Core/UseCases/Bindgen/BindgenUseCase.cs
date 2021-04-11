// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.IO;
using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.Bindgen
{
    public class BindgenUseCase : UseCase<BindgenUseCaseRequest, BindgenUseCaseResponse, BindgenUseCaseState>
    {
        public BindgenUseCase()
            : base(new UseCaseStep[]
            {
                new("Parse C code from disk using libclang", ParseCCode),
                new("Extract Clang abstract syntax tree", ExploreCCode),
                new("Map Clang to C#", MapCAbstractSyntaxTreeToCSharp),
                new("Generate C# code", GenerateCSharpCode),
                new("Write C# code to disk", WriteCSharpCode)
            })
        {
        }

        private static void ParseCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            state.ClangTranslationUnit = ClangParser.ParseTranslationUnit(request.InputFilePath, request.ClangArgs);
        }

        private static void ExploreCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var clangExplorer = new ClangExplorer();
            state.ClangAbstractSyntaxTree = clangExplorer.ExtractAbstractSyntaxTree(state.ClangTranslationUnit);
        }

        public static void MapCAbstractSyntaxTreeToCSharp(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            state.CSharpAbstractSyntaxTree = CSharpMapper.GetAbstractSyntaxTree(state.ClangAbstractSyntaxTree);
        }

        private static void GenerateCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            state.GeneratedCSharpCode = CSharpCodeGenerator.GenerateFile(
                request.ClassName, request.LibraryName, state.CSharpAbstractSyntaxTree);
        }

        private static void WriteCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            File.WriteAllText(request.OutputFilePath, state.GeneratedCSharpCode);
            Console.WriteLine(request.OutputFilePath);
        }

        protected override BindgenUseCaseResponse ReturnResult(
            BindgenUseCaseRequest request,
            BindgenUseCaseState state)
        {
            return new(request.OutputFilePath);
        }
    }
}
