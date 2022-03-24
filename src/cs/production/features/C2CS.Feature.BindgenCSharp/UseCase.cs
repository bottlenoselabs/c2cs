// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using C2CS.Feature.BindgenCSharp.Data;
using C2CS.Feature.BindgenCSharp.Domain;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

namespace C2CS.Feature.BindgenCSharp;

public class UseCase : UseCase<RequestBindgenCSharp, Input, Response>
{
    public UseCase()
        : base(new Validator())
    {
    }

    protected override Response Execute(Input input)
    {
        var abstractSyntaxTreesC = LoadCAbstractSyntaxTrees(input.InputFilePaths);

        var nodesPerPlatform = MapCNodesToCSharpNodes(
            abstractSyntaxTreesC,
            input.TypeAliases,
            input.IgnoredNames,
            Diagnostics);

        var abstractSyntaxTreeCSharp = AbstractSyntaxTree(nodesPerPlatform);

        var code = GenerateCSharpCode(
            abstractSyntaxTreeCSharp,
            input.ClassName,
            input.LibraryName,
            input.NamespaceName,
            input.HeaderCodeRegion,
            input.FooterCodeRegion);

        WriteCSharpCodeToFileStorage(input.OutputFilePath, code);

        var response = new Response();
        return response;
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

        var mapperParameters = new MapperCToCSharp.Parameters(typeAliases, ignoredTypeNames, diagnostics);
        var mapper = new MapperCToCSharp(mapperParameters);
        var result = mapper.Map(abstractSyntaxTrees);

        EndStep();

        return result;
    }

    [UseCaseStep("Split or flatten platform specific C# nodes into a C# abstract syntax tree")]
    private CSharpAbstractSyntaxTree AbstractSyntaxTree(ImmutableDictionary<RuntimePlatform, CSharpNodes> nodesByPlatform)
    {
        BeginStep();

        var abstractSyntaxTreeBuilder = new BuilderCSharpAbstractSyntaxTree();
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

        var codeGenerator = new GeneratorCSharpCode(
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
