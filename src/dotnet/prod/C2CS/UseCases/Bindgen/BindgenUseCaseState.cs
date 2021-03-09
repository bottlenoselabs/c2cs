// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using C2CS.Bindgen.ExploreCCode;
using ClangSharp.Interop;

namespace C2CS.Bindgen
{
    public struct BindgenUseCaseState
    {
        public CXTranslationUnit ClangTranslationUnit;
        public CAbstractSyntaxTree CAbstractSyntaxTree;
        public string GeneratedCSharpCode;
    }
}
