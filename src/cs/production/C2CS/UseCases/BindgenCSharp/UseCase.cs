// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
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

            string className;
            if (string.IsNullOrEmpty(request.ClassName))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(request.OutputFilePath);
                var firstIndexOfPeriod = fileNameWithoutExtension.IndexOf('.');
                className = firstIndexOfPeriod == -1 ? fileNameWithoutExtension : fileNameWithoutExtension[..firstIndexOfPeriod];
            }
            else
            {
                className = request.ClassName;
            }

            var libraryName = string.IsNullOrEmpty(request.LibraryName) ? className : request.LibraryName;

            var abstractSyntaxTreeC = Step(
                "Load C abstract syntax tree from disk",
                request.InputFilePath,
				request.MoreManaged,
				LoadAbstractSyntaxTree);

            var abstractSyntaxTreeCSharp = Step(
                "Map C abstract syntax tree to C#",
                className,
                abstractSyntaxTreeC,
                request.TypeAliases,
                request.IgnoredNames,
                abstractSyntaxTreeC.Bitness,
                Diagnostics,
				request.MoreManaged,
                MapCToCSharp);

            var codeCSharp = Step(
                "Generate C# code",
                abstractSyntaxTreeCSharp,
                className,
                libraryName,
                request.UsingNamespaces,
				request.MoreManaged,
				GenerateCSharpCode);

            Step(
                "Write C# code to disk",
                request.OutputFilePath,
                codeCSharp,
                WriteCSharpCode);
        }

        private static void Validate(Request request)
        {
            if (!File.Exists(request.InputFilePath))
            {
                throw new UseCaseException($"File does not exist: `{request.InputFilePath}`.");
            }
        }

        private static CAbstractSyntaxTree LoadAbstractSyntaxTree(string inputFilePath, bool moreManaged)
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

			//This seems it is just easier to do it in this stupid way!
			//I dont know how much trouble this can make but this is collapsing all typedefs to their underlying types
			//Should we care much about typedefs in PInvoke?
			if (moreManaged)
			{
				var typedefs = abstractSyntaxTree.Typedefs;
				abstractSyntaxTree.Typedefs = new ImmutableArray<CTypedef>();
				var newfileContents = JsonSerializer.Serialize(abstractSyntaxTree, options);

				var newtypedefs = new List<CTypedef>();
				foreach (var typedef in typedefs)
				{
					//if pointer to udnerlying exist. It is best to ignore for now!
					if (newfileContents.Contains($"\"type\": \"{typedef.Name}*\""))
					{
						newtypedefs.Add(typedef);
					}

					newfileContents = newfileContents.Replace($"\"type\": \"{typedef.Name}\"", $"\"type\": \"{typedef.UnderlyingType}\"");

					if (!newfileContents.Contains($"\"returnType\": \"{typedef.Name}*\""))
						newfileContents = newfileContents.Replace($"\"returnType\": \"{typedef.Name}\"", $"\"returnType\": \"{typedef.UnderlyingType}\"");
				}

				abstractSyntaxTree = JsonSerializer.Deserialize<CAbstractSyntaxTree>(newfileContents, options)!;
				abstractSyntaxTree.Typedefs = ImmutableArray.Create(newtypedefs.ToArray());


				
			} 
            return abstractSyntaxTree;
        }

        private static CSharpAbstractSyntaxTree MapCToCSharp(
            string className,
            CAbstractSyntaxTree abstractSyntaxTree,
            ImmutableArray<CSharpTypeAlias> typeAliases,
            ImmutableArray<string> ignoredTypeNames,
            int bitness,
            DiagnosticsSink diagnostics,
			bool moreManaged)
        {
			ICSharpMapper mapper;
			if (moreManaged)
				mapper = new CSharpMapperManaged(className, typeAliases, ignoredTypeNames, bitness, diagnostics, moreManaged);
			else
				mapper = new CSharpMapper(className, typeAliases, ignoredTypeNames, bitness, diagnostics, moreManaged);

			return mapper.AbstractSyntaxTree(abstractSyntaxTree);
        }

        private static string GenerateCSharpCode(
            CSharpAbstractSyntaxTree abstractSyntaxTree, string className, string libraryName, ImmutableArray<string> usingNamespaces, bool moreManaged)
        {
			ICSharpCodeGenerator codeGenerator;
			if (moreManaged)
			{
				codeGenerator = new CSharpCodeGeneratorManaged(className, libraryName, usingNamespaces);
			}
			else
			{
				codeGenerator = new CSharpCodeGenerator(className, libraryName, usingNamespaces);
			} 
            
            return codeGenerator.EmitCode(abstractSyntaxTree);
        }

        private static void WriteCSharpCode(
            string outputFilePath, string codeCSharp)
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

            File.WriteAllText(outputFilePath, codeCSharp);
            Console.WriteLine(outputFilePath);
        }
    }
}
