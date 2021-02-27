// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS
{
    public class BindgenUseCase : UseCase<BindgenUseCaseRequest, BindgenUseCaseResponse, BindgenUseCaseState>
    {
        public BindgenUseCase()
            : base(new UseCaseStep[]
            {
                new("Parse C code from disk using libclang", ParseCCode),
                new("Extract Clang abstract syntax tree", ExploreCCode),
                new("Generate C# code", TranspileCCodeToCSharp),
                new("Write C# code to disk", WriteCSharpCode)
            })
        {
        }

        private static void ParseCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new BindgenParseCCode();
            state.ClangTranslationUnit = obj.ParseClangTranslationUnit(request.InputFilePath, request.ClangArgs);
        }

        private static void ExploreCCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new BindgenExploreCCode();
            state.ClangExtractedCursors = obj.ExtractClangAbstractSyntaxTree(state.ClangTranslationUnit);
        }

        private static void TranspileCCodeToCSharp(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new BindgenTranspileCCodeToCSharp();
            state.GeneratedCSharpCode = obj.GenerateCSharpCode(request.LibraryName, state.ClangExtractedCursors);
        }

        private static void WriteCSharpCode(BindgenUseCaseRequest request, ref BindgenUseCaseState state)
        {
            var obj = new BindgenWriteCSharpCode();
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
