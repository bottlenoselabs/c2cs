// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.WriteCodeCSharp.Data;
using C2CS.Feature.WriteCodeCSharp.Data.Model;
using C2CS.Feature.WriteCodeCSharp.Domain;
using C2CS.Feature.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Feature.WriteCodeCSharp.Domain.Mapper;
using C2CS.Foundation.Diagnostics;
using C2CS.Foundation.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.WriteCodeCSharp;

public sealed class WriteCodeCSharpUseCase : UseCase<WriteCodeCSharpConfiguration, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    public override string Name => "Bindgen C#";

    public WriteCodeCSharpUseCase(ILogger logger, IServiceProvider services, WriteCodeCSharpValidator validator)
        : base(logger, services, validator)
    {
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
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
    }

    private ImmutableArray<CAbstractSyntaxTree> LoadCAbstractSyntaxTrees(ImmutableArray<string> filePaths)
    {
        BeginStep("Load");

        var cJsonSerializer = Services.GetService<CJsonSerializer>()!;

        var builder = ImmutableArray.CreateBuilder<CAbstractSyntaxTree>();
        foreach (var filePath in filePaths)
        {
            var ast = cJsonSerializer.Read(filePath);
            builder.Add(ast);
        }

        EndStep();

        return builder.ToImmutable();
    }

    private ImmutableDictionary<TargetPlatform, CSharpNodes> MapCNodesToCSharpNodes(
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

    private CSharpAbstractSyntaxTree AbstractSyntaxTree(ImmutableDictionary<TargetPlatform, CSharpNodes> nodesByPlatform)
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

        File.WriteAllText(outputFilePath, codeCSharp);
        Console.WriteLine(outputFilePath);

        EndStep();
    }
}
