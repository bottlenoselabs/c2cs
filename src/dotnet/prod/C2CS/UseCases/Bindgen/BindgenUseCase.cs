// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using C2CS.Bindgen.GenerateCSharpCode;
using C2CS.Bindgen.WriteCSharpCode;

namespace C2CS.Bindgen
{
    public class BindgenUseCase : UseCase<BindgenUseCaseRequest, BindgenUseCaseResponse, BindgenUseCaseState>
    {
        public BindgenUseCase()
            : base(new UseCaseStep[]
            {
                new("Parse C code from disk using libclang", ParseCCode),
                new("Extract Clang AST", ExploreCCode),
                new("Map Clang to C#", MapCAbstractSyntaxTreeToCSharp),
                new("Generate C# code", GenerateCSharpCode),
                new("Write C# code to disk", WriteCSharpCode)
            })
        {
        }

        private static void ParseCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new ParseCCode.ParseCCodeModule();
            state.ClangTranslationUnit = obj.ParseClangTranslationUnit(request.InputFilePath, request.ClangArgs);
        }

        private static void ExploreCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new ExploreCCode.ExploreCCodeModule();
            state.ClangAbstractSyntaxTree = obj.ExtractAbstractSyntaxTree(state.ClangTranslationUnit);
        }

        public static void MapCAbstractSyntaxTreeToCSharp(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new MapCCodeToCSharp.MapCCodeToCSharpModule();
            state.CSharpAbstractSyntaxTree = obj.GetAbstractSyntaxTree(state.ClangAbstractSyntaxTree);
        }

        private static void GenerateCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new GenerateCSharpCodeModule();
            state.GeneratedCSharpCode = obj.GenerateCSharpCode(request.LibraryName, state.CSharpAbstractSyntaxTree);
        }

        private static void WriteCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new WriteCSharpCode.WriteCSharpCodeModule();
            WriteCSharpCodeModule.WriteCSharpToDisk(request.OutputFilePath, state.GeneratedCSharpCode);
        }

        protected override BindgenUseCaseResponse ReturnResult(
            BindgenUseCaseRequest request,
            BindgenUseCaseState state)
        {
            return new(request.OutputFilePath);
        }
    }
}
