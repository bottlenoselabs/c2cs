// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using C2CS.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Bindgen.GenerateCSharpCode
{
    public class GenerateCSharpCodeModule
    {
        private CSharpCodeGenerator _cSharpCodeGenerator = null!;

        public string GenerateCSharpCode(string libraryName, CSharpAbstractSyntaxTree abstractSyntaxTree)
        {
            _cSharpCodeGenerator = new CSharpCodeGenerator(libraryName);

            var members = new List<MemberDeclarationSyntax>();

            foreach (var functionExtern in abstractSyntaxTree.FunctionExterns)
            {
                var member = _cSharpCodeGenerator.CreateExternMethod(functionExtern);
                members.Add(member);
            }

            foreach (var functionPointer in abstractSyntaxTree.FunctionPointers)
            {
                var member = CSharpCodeGenerator.CreateFunctionPointer(functionPointer);
                members.Add(member);
            }

            foreach (var @struct in abstractSyntaxTree.Structs)
            {
                var member = _cSharpCodeGenerator.CreateStruct(@struct);
                members.Add(member);
            }

            foreach (var opaqueDataType in abstractSyntaxTree.OpaqueDataTypes)
            {
                var member = _cSharpCodeGenerator.CreateOpaqueStruct(opaqueDataType);
                members.Add(member);
            }

            foreach (var @enum in abstractSyntaxTree.Enums)
            {
                var member = _cSharpCodeGenerator.CreateEnum(@enum);
                members.Add(member);
            }

            var className = Path.GetFileNameWithoutExtension(libraryName);
            var @class = _cSharpCodeGenerator.CreatePInvokeClass(className, members.ToImmutableArray());
            return @class.ToFullString();
        }
    }
}
