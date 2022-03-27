// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ExploreCode;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.InstallClang;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ParseCode;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractAbstractSyntaxTreeUseCase : UseCase<
    ExtractAbstractSyntaxTreeRequest, ExtractAbstractSyntaxTreeInput, ExtractAbstractSyntaxTreeOutput>
{
    private readonly IServiceProvider _services;

    public override string Name => "Extract AST C";

    public ExtractAbstractSyntaxTreeUseCase(ILogger logger, IServiceProvider services, ExtractAbstractSyntaxTreeValidator validator)
        : base(logger, services, validator)
    {
        _services = services;
    }

    protected override void Execute(ExtractAbstractSyntaxTreeInput input, ExtractAbstractSyntaxTreeOutput output)
    {
        InstallClang();

        var translationUnit = Parse(
            input.InputFilePath,
            input.IsEnabledFindSdk,
            input.IncludeDirectories,
            input.ClangDefines,
            input.TargetPlatform,
            input.ClangArguments);

        var abstractSyntaxTreeC = Explore(
            translationUnit,
            input.IncludeDirectories,
            input.ExcludedHeaderFiles,
            input.OpaqueTypeNames,
            input.FunctionNamesWhitelist,
            input.TargetPlatform);

        Write(input.OutputFilePath, abstractSyntaxTreeC);
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
        RuntimePlatform targetPlatform,
        ImmutableArray<string> clangArguments)
    {
        BeginStep("Parse");

        var clangArgs = ClangArgumentsBuilder.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            defines,
            targetPlatform,
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
        RuntimePlatform targetPlatform)
    {
        BeginStep("Extract");

        var context = new ClangTranslationUnitExplorerContext(
            Diagnostics,
            includeDirectories,
            excludedHeaderFiles,
            opaqueTypeNames,
            functionNamesWhitelist,
            targetPlatform);
        var clangExplorer = _services.GetService<ClangTranslationUnitExplorer>()!;
        var result = clangExplorer.AbstractSyntaxTree(context, translationUnit);

        EndStep();
        return result;
    }

    private void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
    {
        BeginStep("Write");
        var cJsonSerializer = _services.GetService<CJsonSerializer>()!;
        cJsonSerializer.Write(abstractSyntaxTree, outputFilePath);
        EndStep();
    }
}
