// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.UseCases.Bindgen
{
    public class UseCase : UseCase<Request, Response, State>
    {
        public UseCase()
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

        private static void ParseCCode(Request request, ref State state)
        {
            state.ClangTranslationUnit = ClangParser.ParseTranslationUnit(request.InputFilePath, request.ClangArgs);
        }

        private static void ExploreCCode(Request request, ref State state)
        {
            var clangExplorer = new ClangExplorer();
            state.ClangAbstractSyntaxTree = clangExplorer.VisitTranslationUnit(
                state.ClangTranslationUnit,
                request.PrintAbstractSyntaxTree,
                request.OpaqueTypes);
        }

        private static void MapCToCSharp(Request request, ref State state)
        {
            state.CSharpAbstractSyntaxTree = CSharpMapper.GetAbstractSyntaxTree(state.ClangAbstractSyntaxTree);
        }

        private static void GenerateCSharpCode(Request request, ref State state)
        {
            state.GeneratedCSharpCode = CSharpCodeGenerator.GenerateFile(
                request.ClassName, request.LibraryName, state.CSharpAbstractSyntaxTree);
        }

        private static void WriteCSharpCode(Request request, ref State state)
        {
            File.WriteAllText(request.OutputFilePath, state.GeneratedCSharpCode);
            Console.WriteLine(request.OutputFilePath);
        }

        protected override Response ReturnResult(
            Request request,
            State state)
        {
            return new(request.OutputFilePath);
        }
    }
}
