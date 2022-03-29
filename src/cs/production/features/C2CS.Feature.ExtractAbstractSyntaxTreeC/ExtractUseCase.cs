// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.ExploreCode;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.InstallClang;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.ParseCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractUseCase : UseCase<
    ExtractRequest, ExtractInput, ExtractOutput>
{
    private readonly IServiceProvider _services;

    public override string Name => "Extract AST C";

    public ExtractUseCase(ILogger logger, IServiceProvider services, ExtractValidator validator)
        : base(logger, services, validator)
    {
        _services = services;
    }

    protected override void Execute(ExtractInput input, ExtractOutput output)
    {
        InstallClang();

        foreach (var inputAbstractSyntaxTree in input.InputAbstractSyntaxTrees)
        {
            var translationUnit = Parse(
                input.InputFilePath,
                inputAbstractSyntaxTree.IsEnabledFindSdk,
                inputAbstractSyntaxTree.IncludeDirectories,
                inputAbstractSyntaxTree.ClangDefines,
                inputAbstractSyntaxTree.Platform,
                inputAbstractSyntaxTree.ClangArguments);

            var abstractSyntaxTreeC = Explore(
                translationUnit,
                inputAbstractSyntaxTree.IncludeDirectories,
                inputAbstractSyntaxTree.ExcludedHeaderFiles,
                inputAbstractSyntaxTree.OpaqueTypeNames,
                inputAbstractSyntaxTree.FunctionNamesWhitelist,
                inputAbstractSyntaxTree.Platform);

            Write(inputAbstractSyntaxTree.OutputFilePath, abstractSyntaxTreeC, inputAbstractSyntaxTree.Platform);
        }
    }

    private void InstallClang()
    {
        BeginStep("Install Clang");

        var installer = _services.GetService<ClangInstaller>()!;
        installer.Install();

        EndStep();
    }

    private CXTranslationUnit Parse(
        string inputFilePath,
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        TargetPlatform platform,
        ImmutableArray<string> clangArguments)
    {
        BeginStep($"Parse {platform}");

        var clangArgumentsBuilder = _services.GetService<ClangArgumentsBuilder>()!;
        var clangArgs = clangArgumentsBuilder.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            defines,
            platform,
            clangArguments);

        var parser = _services.GetService<ClangTranslationUnitParser>()!;
        var result = parser.Parse(
            Diagnostics, inputFilePath, clangArgs);

        EndStep();
        return result;
    }

    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> excludedHeaderFiles,
        ImmutableArray<string> opaqueTypeNames,
        ImmutableArray<string> functionNamesWhitelist,
        TargetPlatform platform)
    {
        BeginStep($"Extract {platform}");

        var context = new ClangTranslationUnitExplorerContext(
            Diagnostics,
            includeDirectories,
            excludedHeaderFiles,
            opaqueTypeNames,
            functionNamesWhitelist,
            platform);
        var clangExplorer = _services.GetService<ClangTranslationUnitExplorer>()!;
        var result = clangExplorer.AbstractSyntaxTree(context, translationUnit);

        EndStep();
        return result;
    }

    private void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree, TargetPlatform platform)
    {
        BeginStep($"Write {platform}");
        var cJsonSerializer = _services.GetService<CJsonSerializer>()!;
        cJsonSerializer.Write(abstractSyntaxTree, outputFilePath);
        EndStep();
    }
}
