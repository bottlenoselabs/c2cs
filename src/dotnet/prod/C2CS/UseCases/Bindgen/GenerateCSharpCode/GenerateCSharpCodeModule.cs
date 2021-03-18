// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using C2CS.CSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Bindgen.GenerateCSharpCode
{
    public class GenerateCSharpCodeModule
    {
        private CSharpCodeGenerator _cSharpCodeGenerator = null!;

        private readonly List<MemberDeclarationSyntax> _members = new();

        public string GenerateCSharpCode(string libraryName, CSharpAbstractSyntaxTree abstractSyntaxTree)
        {
            _cSharpCodeGenerator = new CSharpCodeGenerator(libraryName);

            foreach (var functionExtern in abstractSyntaxTree.FunctionExterns)
            {
                TranspileFunctionExtern(functionExtern);
            }

            foreach (var functionPointer in abstractSyntaxTree.FunctionPointers)
            {
                TranspileFunctionPointer(functionPointer);
            }

            foreach (var @struct in abstractSyntaxTree.Structs)
            {
                TranspileRecord(@struct);
            }

            foreach (var @enum in abstractSyntaxTree.Enums)
            {
                TranspileEnum(@enum);
            }

            var className = Path.GetFileNameWithoutExtension(libraryName);
            var members = _members.ToImmutableArray();

            var @class = _cSharpCodeGenerator.CreatePInvokeClass(className, members);

            return @class.ToFullString();
        }

        private void TranspileFunctionPointer(CSharpFunctionPointer functionPointer)
        {
            var cSharpFunctionPointer = CSharpCodeGenerator.CreateFunctionPointer(functionPointer);
            _members.Add(cSharpFunctionPointer);
        }

        private void TranspileRecord(CSharpStruct @struct)
        {
            var cSharpStruct = _cSharpCodeGenerator.CreateStruct(@struct);
            _members.Add(cSharpStruct);
        }

        private void TranspileFunctionExtern(CSharpFunctionExtern functionExtern)
        {
            var cSharpMethod = _cSharpCodeGenerator.CreateExternMethod(functionExtern);
            _members.Add(cSharpMethod);
        }

        private void TranspileConstant(CXCursor clangConstant)
        {
            // var cSharpConstant = _cSharpCodeGenerator.CreateConstant(clangConstant);
            // _members.Add(cSharpConstant);
        }

        private void TranspileEnum(CSharpEnum @enum)
        {
            var cSharpEnum = _cSharpCodeGenerator.CreateEnum(@enum);
            _members.Add(cSharpEnum);
        }
    }
}
