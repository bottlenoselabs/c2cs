// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class UseCase : UseCase<Request, Response>
    {
        protected override void Execute(Request request, Response response)
        {
            TotalSteps(4);

            var configuration = Step(
                "Load configuration from disk",
                request.InputFile.FullName,
                request.ConfigurationFile?.FullName ?? string.Empty,
                request.IncludeDirectories,
                LoadConfiguration);

            var translationUnit = Step(
                "Parse C code from disk",
                request.InputFile.FullName,
                configuration,
                request.IncludeDirectories,
                Parse);

            var abstractSyntaxTreeC = Step(
                "Extract C abstract syntax tree",
                translationUnit,
                request.IncludeDirectories,
                request.IgnoredFiles,
                request.OpaqueTypes,
                Explore);

            Step(
                "Write C abstract syntax tree to disk",
                request.OutputFile.FullName,
                abstractSyntaxTreeC,
                Write);
        }

        private static Configuration LoadConfiguration(
            string inputFilePath, string configurationFilePath, ImmutableArray<string> includeDirectories)
        {
            if (string.IsNullOrEmpty(configurationFilePath))
            {
                return new Configuration();
            }

            var fileContents = File.ReadAllText(configurationFilePath);
            var configuration = JsonSerializer.Deserialize<Configuration>(fileContents)!;

            return configuration;
        }

        private static clang.CXTranslationUnit Parse(
            string inputFilePath, Configuration configuration, ImmutableArray<string> includeDirectories)
        {
            var clangArgs = ClangArgumentsBuilder.Build(
                configuration.AutomaticallyFindSoftwareDevelopmentKit,
                includeDirectories,
                configuration.Defines,
                configuration.ClangArguments);
            return ClangParser.ParseTranslationUnit(inputFilePath, clangArgs);
        }

        private CAbstractSyntaxTree Explore(
            clang.CXTranslationUnit translationUnit,
            ImmutableArray<string> includeDirectories,
            ImmutableArray<string> ignoredFiles,
            ImmutableArray<string> opaqueTypes)
        {
            var clangExplorer = new ClangExplorer(Diagnostics, includeDirectories, ignoredFiles, opaqueTypes);
            return clangExplorer.AbstractSyntaxTree(translationUnit);
        }

        private static void Write(
            string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
        {
            var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
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
