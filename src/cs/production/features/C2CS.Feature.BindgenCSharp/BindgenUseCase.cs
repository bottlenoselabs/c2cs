// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BindgenCSharp.Data;
using C2CS.Feature.BindgenCSharp.Domain;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.BindgenCSharp;

public class BindgenUseCase : UseCase<BindgenRequest, BindgenInput, BindgenResponse>
{
    private readonly CJsonSerializer _cJsonSerializer;

    public BindgenUseCase(ILogger logger, CJsonSerializer cJsonSerializer)
        : base("Bindgen C#", logger, new BindgenValidator())
    {
        _cJsonSerializer = cJsonSerializer;
    }

    protected override BindgenResponse Execute(BindgenInput input)
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

        var response = new BindgenResponse
        {
            Code = code
        };
        return response;
    }

    private ImmutableArray<CAbstractSyntaxTree> LoadCAbstractSyntaxTrees(ImmutableArray<string> filePaths)
    {
        BeginStep("Load");

        var builder = ImmutableArray.CreateBuilder<CAbstractSyntaxTree>();
        foreach (var filePath in filePaths)
        {
            var ast = _cJsonSerializer.Read(filePath);
            builder.Add(ast);
        }

        EndStep();

        return builder.ToImmutable();
    }

    private ImmutableDictionary<RuntimePlatform, CSharpNodes> MapCNodesToCSharpNodes(
        ImmutableArray<CAbstractSyntaxTree> abstractSyntaxTrees,
        ImmutableArray<CSharpTypeAlias> typeAliases,
        ImmutableArray<string> ignoredTypeNames,
        DiagnosticsSink diagnostics)
    {
        BeginStep("Map platform specific nodes");

        var mapperParameters = new CSharpMapperParameters(typeAliases, ignoredTypeNames, diagnostics);
        var mapper = new CSharpMapper(mapperParameters);
        var result = mapper.Map(abstractSyntaxTrees);

        EndStep();

        return result;
    }

    private CSharpAbstractSyntaxTree AbstractSyntaxTree(ImmutableDictionary<RuntimePlatform, CSharpNodes> nodesByPlatform)
    {
        BeginStep("Split/flatten platform specific nodes");

        var abstractSyntaxTreeBuilder = new BuilderCSharpAbstractSyntaxTree();
        foreach (var (platform, nodes) in nodesByPlatform)
        {
            abstractSyntaxTreeBuilder.Add(platform, nodes);
        }

        var result = abstractSyntaxTreeBuilder.Build();

        EndStep();

        return result;
    }

    private string GenerateCSharpCode(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        string className,
        string libraryName,
        string namespaceName,
        string headerCodeRegion,
        string footerCodeRegion)
    {
        BeginStep("Generate code");

        var codeGenerator = new GeneratorCSharpCode(
            className, libraryName, namespaceName, headerCodeRegion, footerCodeRegion);
        var result = codeGenerator.EmitCode(abstractSyntaxTree);

        EndStep();

        return result;
    }

    private void WriteCSharpCodeToFileStorage(
        string outputFilePath, string codeCSharp)
    {
        BeginStep("Write file");

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
