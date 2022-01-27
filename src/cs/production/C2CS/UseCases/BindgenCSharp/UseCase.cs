// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.UseCases.ExtractAbstractSyntaxTreeC;

namespace C2CS.UseCases.BindgenCSharp;

public class UseCase : UseCase<Request, Response>
{
    protected override void Execute(Request request, Response response)
    {
        Validate(request);

        var abstractSyntaxTree = LoadCAbstractSyntaxTreeFromFileStorage(request.InputFilePath);

        var abstractSyntaxTreeCSharp = MapCAbstractSyntaxTreeToCSharp(
            abstractSyntaxTree,
            request.TypeAliases,
            request.IgnoredTypeNames,
            abstractSyntaxTree.Bitness,
            Diagnostics);

        var codeCSharp = GenerateCSharpCode(
            abstractSyntaxTreeCSharp,
            request.ClassName,
            request.LibraryName,
            request.NamespaceName,
            request.HeaderCodeRegion,
            request.FooterCodeRegion);

        WriteCSharpCodeToFileStorage(request.OutputFilePath, codeCSharp);
    }

    private static void Validate(Request request)
    {
        if (!File.Exists(request.InputFilePath))
        {
            throw new UseCaseException($"File does not exist: `{request.InputFilePath}`.");
        }
    }

    [UseCaseStep("Load C abstract syntax tree from file storage.")]
    private CAbstractSyntaxTree LoadCAbstractSyntaxTreeFromFileStorage(string inputFilePath)
    {
        BeginStep();
        var fileContents = File.ReadAllText(inputFilePath);
        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var serializerContext = new CJsonSerializerContext(serializerOptions);
        var abstractSyntaxTree = JsonSerializer.Deserialize(fileContents, serializerContext.CAbstractSyntaxTree)!;
        EndStep();

        return abstractSyntaxTree;
    }

    [UseCaseStep("Map C abstract syntax tree to C#")]
    private CSharpAbstractSyntaxTree MapCAbstractSyntaxTreeToCSharp(
        CAbstractSyntaxTree abstractSyntaxTree,
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        int bitness,
        DiagnosticsSink diagnostics)
    {
        BeginStep();
        var mapperParameters = new CSharpMapperParameters(
            typeAliases, ignoredTypeNames, bitness, diagnostics);
        var mapper = new CSharpMapper(mapperParameters);
        var result = mapper.AbstractSyntaxTree(abstractSyntaxTree);
        EndStep();

        return result;
    }

    [UseCaseStep("Generate C# code")]
    private string GenerateCSharpCode(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        string className,
        string libraryName,
        string namespaceName,
        string headerCodeRegion,
        string footerCodeRegion)
    {
        BeginStep();
        var codeGenerator = new CSharpCodeGenerator(
            className, libraryName, namespaceName, headerCodeRegion, footerCodeRegion);
        var result = codeGenerator.EmitCode(abstractSyntaxTree);
        EndStep();

        return result;
    }

    [UseCaseStep("Write C# code to file storage")]
    private void WriteCSharpCodeToFileStorage(
        string outputFilePath, string codeCSharp)
    {
        BeginStep();
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
        EndStep();
    }
}
