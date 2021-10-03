// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class UseCase : UseCase<Request, Response>
    {
        protected override void Execute(Request request, Response response)
        {
            Validate(request);
            TotalSteps(3);

            var translationUnit = Step(
                "Parse C code from disk",
                request.InputFilePath,
                request.AutomaticallyFindSoftwareDevelopmentKit,
                request.IncludeDirectories,
                request.Defines,
                request.Bitness,
                request.ClangArgs,
                Parse);

            var abstractSyntaxTreeC = Step(
                "Extract C abstract syntax tree",
                translationUnit,
                request.IncludeDirectories,
                request.IgnoredFiles,
                request.OpaqueTypes,
                request.WhitelistFunctionNames,
                request.Bitness ?? (RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.X64 ? 64 : 32),
                Explore);

            Step(
                "Write C abstract syntax tree to disk",
                request.OutputFilePath,
                abstractSyntaxTreeC,
                Write);
        }

        private static void Validate(Request request)
        {
            if (!File.Exists(request.InputFilePath))
            {
                throw new UseCaseException($"File does not exist: `{request.InputFilePath}`.");
            }
        }

        private static clang.CXTranslationUnit Parse(
            string inputFilePath,
            bool automaticallyFindSoftwareDevelopmentKit,
            ImmutableArray<string> includeDirectories,
            ImmutableArray<string> defines,
            int? bitness,
            ImmutableArray<string> clangArguments)
        {
            var clangArgs = ClangArgumentsBuilder.Build(
                automaticallyFindSoftwareDevelopmentKit,
                includeDirectories,
                defines,
                bitness,
                clangArguments);
            return ClangTranslationUnitParser.Parse(inputFilePath, clangArgs);
        }

        private CAbstractSyntaxTree Explore(
            clang.CXTranslationUnit translationUnit,
            ImmutableArray<string> includeDirectories,
            ImmutableArray<string> ignoredFiles,
            ImmutableArray<string> opaqueTypes,
            ImmutableArray<string> whitelistFunctionNames,
            int bitness)
        {
            var clangExplorer = new ClangTranslationUnitExplorer(
                Diagnostics, includeDirectories, ignoredFiles, opaqueTypes, whitelistFunctionNames);
            return clangExplorer.AbstractSyntaxTree(translationUnit, bitness);
        }

        private static void Write(
            string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
        {
            var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
            if (string.IsNullOrEmpty(outputDirectory))
            {
                outputDirectory = AppContext.BaseDirectory;
                outputFilePath = Path.Combine(Environment.CurrentDirectory, outputFilePath);
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            if (File.Exists(outputFilePath))
            {
                File.Delete(outputFilePath);
            }

            var fileContents = JsonSerializer.Serialize(abstractSyntaxTree, options);

            // File.WriteAllText doesn't flush until process exits on macOS .NET 5 lol
            using var fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate);
            using var textWriter = new StreamWriter(fileStream);
            textWriter.Write(fileContents);
            textWriter.Close();
            fileStream.Close();

            Console.WriteLine(outputFilePath);
        }
    }
}
