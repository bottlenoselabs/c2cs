// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.UseCases.AbstractSyntaxTreeC;

namespace C2CS.UseCases.BindgenCSharp
{
    public class UseCase : UseCase<Request, Response>
    {
        protected override void Execute(Request request, Response response)
        {
            Validate(request);
            TotalSteps(5);

            var configuration = Step(
                "Load configuration from disk",
                request.InputFile.FullName,
                request.ConfigurationFile.FullName,
                LoadConfiguration);

            var abstractSyntaxTreeC = Step(
                "Load C abstract syntax tree from disk",
                request.InputFile.FullName,
                configuration,
                LoadAbstractSyntaxTree);

            var abstractSyntaxTreeCSharp = Step(
                "Map C abstract syntax tree to C#",
                abstractSyntaxTreeC,
                MapCToCSharp);

            var codeCSharp = Step(
                "Generate C# code",
                abstractSyntaxTreeCSharp,
                configuration,
                GenerateCSharpCode);

            Step(
                "Write C# code to disk",
                request.OutputFile.FullName,
                codeCSharp,
                WriteCSharpCode);
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

            return configuration;
        }

        private CAbstractSyntaxTree LoadAbstractSyntaxTree(string inputFilePath, Configuration configuration)
        {
            var fileContents = File.ReadAllText(inputFilePath);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
            var abstractSyntaxTree = JsonSerializer.Deserialize<CAbstractSyntaxTree>(fileContents, options)!;
            return abstractSyntaxTree;
        }

        private static CSharpAbstractSyntaxTree MapCToCSharp(
            CAbstractSyntaxTree abstractSyntaxTree)
        {
            var mapper = new CSharpMapper();
            return mapper.AbstractSyntaxTree(abstractSyntaxTree);
        }

        private static string GenerateCSharpCode(
            CSharpAbstractSyntaxTree abstractSyntaxTree, Configuration configuration)
        {
            var codeGenerator = new CSharpCodeGenerator(configuration);
            return codeGenerator.EmitCode(abstractSyntaxTree);
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
