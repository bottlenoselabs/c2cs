// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.UseCases.Bindgen
{
    public class BindgenUseCase : UseCase<BindgenRequest, BindgenResponse>
    {
        protected override void Execute(BindgenRequest request, BindgenResponse response)
        {
            TotalSteps(5);

            var translationUnit = Step(
                "Parse C code from disk",
                request.Configuration.InputFilePath,
                request.Configuration.CPreferencesParse,
                ParseCCode);

            var abstractSyntaxTreeC = Step(
                "Extract C abstract syntax tree",
                translationUnit,
                request.Configuration.CPreferencesExplore,
                ExploreCCode);

            var abstractSyntaxTreeCSharp = Step(
                "Map C abstract syntax tree to C#",
                abstractSyntaxTreeC,
                MapCToCSharp);

            var codeCSharp = Step(
                "Generate C# code",
                abstractSyntaxTreeCSharp,
                request.Configuration.CSharpPreferencesGenerate,
                GenerateCSharpCode);

            Step(
                "Write C# code to disk",
                request.Configuration.OutputFilePath,
                codeCSharp,
                WriteCSharpCode);
        }

        private libclang.CXTranslationUnit ParseCCode(
            string headerFilePath, CPreferencesParse preferences)
        {
            var clangArgs = ClangArgumentsBuilder.Build(
                preferences.AutomaticallyFindSoftwareDevelopmentKit,
                preferences.IncludeDirectories,
                preferences.Defines,
                preferences.ClangArguments);
            return ClangParser.ParseTranslationUnit(headerFilePath, clangArgs);
        }

        private CAbstractSyntaxTree ExploreCCode(
            libclang.CXTranslationUnit translationUnit, CPreferencesExplore cPreferences)
        {
            var clangExplorer = new ClangExplorer(Diagnostics);
            return clangExplorer.AbstractSyntaxTree(
                translationUnit,
                cPreferences);
        }

        private static CSharpAbstractSyntaxTree MapCToCSharp(
            CAbstractSyntaxTree abstractSyntaxTree)
        {
            return CSharpMapper.AbstractSyntaxTree(abstractSyntaxTree);
        }

        private static string GenerateCSharpCode(
            CSharpAbstractSyntaxTree abstractSyntaxTree, CSharpPreferencesGenerate cSharpPreferences)
        {
            return CSharpCodeGenerator.Generate(abstractSyntaxTree, cSharpPreferences);
        }

        private static void WriteCSharpCode(
            string outputFilePath, string codeCSharp)
        {
            var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            File.WriteAllText(outputFilePath, codeCSharp);
            Console.WriteLine(outputFilePath);
        }
    }
}
