// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Data.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Contexts.WriteCodeCSharp.Domain;
using C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Contexts.WriteCodeCSharp.Domain.Mapper;
using C2CS.Foundation.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.WriteCodeCSharp;

public sealed class UseCase : UseCase<WriteCodeCSharpConfiguration, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    public UseCase(
        ILogger<UseCase> logger, IServiceProvider services, WriteCodeCSharpValidator validator)
        : base(logger, services, validator)
    {
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
    {
        var abstractSyntaxTreesC = LoadCAbstractSyntaxTrees(input.InputFilePaths);

        var nodesPerPlatform = MapCNodesToCSharpNodes(
            abstractSyntaxTreesC,
            input.TypeAliases,
            input.IgnoredNames);

        var abstractSyntaxTreeCSharp = AbstractSyntaxTree(nodesPerPlatform);
        var code = GenerateCSharpCode(abstractSyntaxTreeCSharp, input.Options);

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
        ImmutableArray<string> ignoredTypeNames)
    {
        BeginStep("Map platform specific nodes");

        var mapperParameters = new CSharpMapperOptions(typeAliases, ignoredTypeNames);
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

    private string GenerateCSharpCode(CSharpAbstractSyntaxTree abstractSyntaxTree, CSharpCodeGeneratorOptions options)
    {
        BeginStep("Generate code");

        var codeGenerator = new CSharpCodeGenerator(options);
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
