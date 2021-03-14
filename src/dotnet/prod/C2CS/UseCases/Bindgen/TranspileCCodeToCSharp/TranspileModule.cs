// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using C2CS.Bindgen.ExploreCCode;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EnumCSharp = Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax;
using FieldCSharp = Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax;
using MethodCSharp = Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax;
using StructCSharp = Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax;

namespace C2CS.Bindgen.TranspileCCodeToCSharp
{
    public class TranspileModule
    {
        private CSharpCodeGenerator _cSharpCodeGenerator = null!;

        private readonly List<MemberDeclarationSyntax> _members = new();

        public string GenerateCSharpCode(string libraryName, CAbstractSyntaxTree abstractSyntaxTree)
        {
            _cSharpCodeGenerator = new CSharpCodeGenerator(libraryName);

            foreach (var cursor in abstractSyntaxTree.SystemTypes)
            {
                var name = abstractSyntaxTree.NamesByCursor[cursor];
                _cSharpCodeGenerator.AddSystemType(cursor, name);
            }

            foreach (var function in abstractSyntaxTree.Functions)
            {
                TranspileFunction(function);
            }

            foreach (var forwardType in abstractSyntaxTree.ForwardTypes)
            {
                TranspileForwardType(forwardType);
            }

            foreach (var functionPointer in abstractSyntaxTree.FunctionPointers)
            {
                TranspileFunctionPointer(functionPointer);
            }

            foreach (var opaqueType in abstractSyntaxTree.OpaqueTypes)
            {
                TranspileOpaqueType(opaqueType);
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

        private void TranspileForwardType(CForwardType forwardType)
        {
            var cSharpStruct = _cSharpCodeGenerator.CreateForwardStruct(forwardType);
            _members.Add(cSharpStruct);
        }

        private void TranspileFunctionPointer(CFunctionPointer functionPointer)
        {
            var cSharpFunctionPointer = _cSharpCodeGenerator.CreateFunctionPointer(functionPointer);
            _members.Add(cSharpFunctionPointer);
        }

        private void TranspileRecord(CStruct @struct)
        {
            var cSharpStruct = _cSharpCodeGenerator.CreateStruct(@struct);
            _members.Add(cSharpStruct);
        }

        private void TranspileFunction(CFunctionExtern functionExtern)
        {
            var cSharpMethod = _cSharpCodeGenerator.CreateExternMethod(functionExtern);
            _members.Add(cSharpMethod);
        }

        private void TranspileConstant(CXCursor clangConstant)
        {
            // var cSharpConstant = _cSharpCodeGenerator.CreateConstant(clangConstant);
            // _members.Add(cSharpConstant);
        }

        private void TranspileEnum(CEnum @enum)
        {
            var cSharpEnum = _cSharpCodeGenerator.CreateEnum(@enum);
            _members.Add(cSharpEnum);
        }

        private void TranspileOpaqueType(COpaqueType opaqueType)
        {
            var cSharpStruct = _cSharpCodeGenerator.CreateOpaqueStruct(opaqueType);
            _members.Add(cSharpStruct);
        }
    }
}
