// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
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
            TotalSteps(4);

            var className = Path.GetFileNameWithoutExtension(request.OutputFile.FullName);
            var libraryName = string.IsNullOrEmpty(request.LibraryName) ? className : request.LibraryName;

            var abstractSyntaxTreeC = Step(
                "Load C abstract syntax tree from disk",
                request.InputFile.FullName,
                LoadAbstractSyntaxTree);

            var abstractSyntaxTreeCSharp = Step(
                "Map C abstract syntax tree to C#",
                className,
                abstractSyntaxTreeC,
                request.TypeAliases,
                request.IgnoredTypeNames,
                MapCToCSharp);

            var codeCSharp = Step(
                "Generate C# code",
                abstractSyntaxTreeCSharp,
                className,
                libraryName,
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

        private static CAbstractSyntaxTree LoadAbstractSyntaxTree(string inputFilePath)
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
            string className,
            CAbstractSyntaxTree abstractSyntaxTree,
            ImmutableArray<CSharpTypeAlias> typeAliases,
            ImmutableArray<string> ignoredTypeNames)
        {
            var mapper = new CSharpMapper(className, typeAliases, ignoredTypeNames);
            return mapper.AbstractSyntaxTree(abstractSyntaxTree);
        }

        private static string GenerateCSharpCode(
            CSharpAbstractSyntaxTree abstractSyntaxTree, string className, string libraryName)
        {
            var codeGenerator = new CSharpCodeGenerator(className, libraryName);
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
