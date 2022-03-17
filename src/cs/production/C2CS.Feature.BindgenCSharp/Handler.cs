// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using C2CS.Feature.BindgenCSharp.Data.Model;
using C2CS.Feature.BindgenCSharp.Domain.Logic;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;

namespace C2CS.Feature.BindgenCSharp;

public class Handler : UseCaseHandler<Input, Output>
{
    protected override void Execute(Input input, Output output)
    {
        Validate(input);

        var abstractSyntaxTrees = LoadCAbstractSyntaxTrees(input.InputFilePaths);

        var cSharpNodesPerPlatform = MapCNodesToCSharpNodes(
            abstractSyntaxTrees,
            input.TypeAliases,
            input.IgnoredNames,
            Diagnostics);

        var abstractSyntaxTree = Map(cSharpNodesPerPlatform);

        var codeCSharp = GenerateCSharpCode(
            abstractSyntaxTree,
            input.ClassName,
            input.LibraryName,
            input.NamespaceName,
            input.HeaderCodeRegion,
            input.FooterCodeRegion);

        WriteCSharpCodeToFileStorage(input.OutputFilePath, codeCSharp);
    }

    private static void Validate(Input request)
    {
        foreach (var inputFilePath in request.InputFilePaths)
        {
            if (!File.Exists(inputFilePath))
            {
                throw new UseCaseException($"File does not exist: `{inputFilePath}`.");
            }
        }
    }

    [UseCaseStep("Load C abstract syntax trees from file storage")]
    private ImmutableArray<CAbstractSyntaxTree> LoadCAbstractSyntaxTrees(ImmutableArray<string> filePaths)
    {
        BeginStep();

        var builder = ImmutableArray.CreateBuilder<CAbstractSyntaxTree>();
        foreach (var filePath in filePaths)
        {
            var fileContents = File.ReadAllText(filePath);
            var serializerContext = CJsonSerializerContext.Create();
            var abstractSyntaxTree = JsonSerializer.Deserialize(fileContents, serializerContext.CAbstractSyntaxTree)!;
            builder.Add(abstractSyntaxTree);
        }

        EndStep();

        return builder.ToImmutable();
    }

    [UseCaseStep("Map each C abstract syntax tree into to C# platform specific nodes")]
    private ImmutableDictionary<RuntimePlatform, CSharpNodes> MapCNodesToCSharpNodes(
        ImmutableArray<CAbstractSyntaxTree> abstractSyntaxTrees,
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        DiagnosticsSink diagnostics)
    {
        BeginStep();

        var mapperParameters = new CSharpMapper.Parameters(typeAliases, ignoredTypeNames, diagnostics);
        var mapper = new CSharpMapper(mapperParameters);
        var result = mapper.Map(abstractSyntaxTrees);

        EndStep();

        return result;
    }

    [UseCaseStep("Split or flatten platform specific C# nodes into a C# abstract syntax tree")]
    private CSharpAbstractSyntaxTree Map(ImmutableDictionary<RuntimePlatform, CSharpNodes> nodesByPlatform)
    {
        BeginStep();

        var abstractSyntaxTreeBuilder = new CSharpAbstractSyntaxTreeBuilder();
        foreach (var (platform, nodes) in nodesByPlatform)
        {
            abstractSyntaxTreeBuilder.Add(platform, nodes);
        }

        var result = abstractSyntaxTreeBuilder.Build();

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
        EndStep();

        Console.WriteLine(outputFilePath);
    }
}
