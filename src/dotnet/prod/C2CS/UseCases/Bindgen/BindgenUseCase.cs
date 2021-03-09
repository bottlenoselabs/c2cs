// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen
{
    public class BindgenUseCase : UseCase<BindgenUseCaseRequest, BindgenUseCaseResponse, BindgenUseCaseState>
    {
        public BindgenUseCase()
            : base(new UseCaseStep[]
            {
                new("Parse C code from disk using libclang", ParseCCode),
                new("Extract AST", ExploreCCode),
                new("Generate C# code", TranspileCCodeToCSharp),
                new("Write C# code to disk", WriteCSharpCode)
            })
        {
        }

        private static void ParseCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new ParseCCode.MainModule();
            state.ClangTranslationUnit = obj.ParseClangTranslationUnit(request.InputFilePath, request.ClangArgs);
        }

        private static void ExploreCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new ExploreCCode.ExploreCCodeModule();
            state.CAbstractSyntaxTree = obj.ExtractClangAbstractSyntaxTree(state.ClangTranslationUnit);
        }

        private static void TranspileCCodeToCSharp(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new TranspileCCodeToCSharp.TranspileModule();
            state.GeneratedCSharpCode = obj.GenerateCSharpCode(request.LibraryName, state.CAbstractSyntaxTree);
        }

        private static void WriteCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new WriteCSharpCode.MainModule();
            obj.WriteCSharpToDisk(request.OutputFilePath, state.GeneratedCSharpCode);
        }

        protected override BindgenUseCaseResponse ReturnResult(
            BindgenUseCaseRequest request,
            BindgenUseCaseState state)
        {
            return new(request.OutputFilePath);
        }
    }
}
