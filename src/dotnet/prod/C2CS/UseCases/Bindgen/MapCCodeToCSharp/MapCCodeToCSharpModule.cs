// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.Bindgen.MapCCodeToCSharp
{
    public class MapCCodeToCSharpModule
    {
        private readonly CSharpMapper _mapper = new();

        public CSharpAbstractSyntaxTree GetAbstractSyntaxTree(
            ClangAbstractSyntaxTree clangAbstractSyntaxTree)
        {
            var functionExterns = _mapper.MapFunctionExterns(
                clangAbstractSyntaxTree.FunctionExterns);
            var functionPointers = _mapper.MapFunctionPointers(
                clangAbstractSyntaxTree.FunctionPointers);
            var structs = _mapper.MapStructs(
                clangAbstractSyntaxTree.Records,
                clangAbstractSyntaxTree.OpaqueDataTypes,
                clangAbstractSyntaxTree.ForwardDataTypes);
            var enums = _mapper.MapEnums(
                clangAbstractSyntaxTree.Enums);

            var result = new CSharpAbstractSyntaxTree(
                functionExterns,
                functionPointers,
                structs,
                enums);

            return result;
        }
    }
}
