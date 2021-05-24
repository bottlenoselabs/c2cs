// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.CSharp;
using C2CS.Languages.C;
using static libclang;

namespace C2CS.UseCases.Bindgen
{
    public struct BindgenState
    {
        public CXTranslationUnit ClangTranslationUnit;
        public ClangAbstractSyntaxTree ClangAbstractSyntaxTree;
        public CSharpAbstractSyntaxTree CSharpAbstractSyntaxTree;
        public string GeneratedCSharpCode;
    }
}
