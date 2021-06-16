// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class UseCase : UseCase<Request, Response>
    {
        protected override void Execute(Request request, Response response)
        {
            Validate(request);
            TotalSteps(4);

            var configuration = Step(
                "Load configuration from disk",
                request.InputFile.FullName,
                request.ConfigurationFile.FullName,
                LoadConfiguration);

            var translationUnit = Step(
                "Parse C code from disk",
                request.InputFile.FullName,
                configuration,
                Parse);

            var abstractSyntaxTreeC = Step(
                "Extract C abstract syntax tree",
                translationUnit,
                configuration,
                Explore);

            Step(
                "Write C abstract syntax tree to disk",
                request.OutputFile.FullName,
                abstractSyntaxTreeC,
                Write);
        }

        private static void Validate(Request request)
        {
            if (!request.InputFile.Exists)
            {
                throw new UseCaseException($"File does not exist: `{request.InputFile.FullName}`.");
            }
        }

        private static Configuration LoadConfiguration(string inputFilePath, string configurationFilePath)
        {
            if (string.IsNullOrEmpty(configurationFilePath))
            {
                return new Configuration();
            }

            var fileContents = File.ReadAllText(configurationFilePath);
            var configuration = JsonSerializer.Deserialize<Configuration>(fileContents)!;

            if (configuration.IncludeDirectories.IsDefaultOrEmpty)
            {
                var directoryPath = Path.GetDirectoryName(inputFilePath)!;
                configuration.IncludeDirectories = new[] { directoryPath }.ToImmutableArray();
            }
            else
            {
                configuration.IncludeDirectories = configuration.IncludeDirectories.Select(Path.GetFullPath).ToImmutableArray();
            }

            return configuration;
        }

        private static libclang.CXTranslationUnit Parse(string inputFilePath, Configuration configuration)
        {
            var clangArgs = ClangArgumentsBuilder.Build(
                configuration.AutomaticallyFindSoftwareDevelopmentKit,
                configuration.IncludeDirectories,
                configuration.Defines,
                configuration.ClangArguments);
            return ClangParser.ParseTranslationUnit(inputFilePath, clangArgs);
        }

        private CAbstractSyntaxTree Explore(
            libclang.CXTranslationUnit translationUnit, Configuration configuration)
        {
            var clangExplorer = new ClangExplorer(Diagnostics, configuration);
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
