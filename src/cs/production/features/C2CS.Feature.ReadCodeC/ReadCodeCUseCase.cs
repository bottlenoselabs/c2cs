// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using C2CS.Feature.ReadCodeC.Domain.ExploreCode;
using C2CS.Feature.ReadCodeC.Domain.InstallClang;
using C2CS.Feature.ReadCodeC.Domain.ParseCode;
using C2CS.Foundation.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ReadCodeC;

public sealed class ReadCodeCUseCase : UseCase<
    ReadCodeCConfiguration, ReadCodeCInput, ReadCodeCOutput>
{
    private readonly IServiceProvider _services;

    public override string Name => "Extract AST C";

    public ReadCodeCUseCase(ILogger logger, IServiceProvider services, ReadCodeCValidator validator)
        : base(logger, services, validator)
    {
        _services = services;
    }

    protected override void Execute(ReadCodeCInput input, ReadCodeCOutput output)
    {
        InstallClang(Native.OperatingSystem);

        var builder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeOptions>();

        foreach (var options in input.AbstractSyntaxTreesOptions)
        {
            var translationUnit = Parse(
                input.InputFilePath,
                options.IsEnabledFindSystemHeaders,
                options.IncludeDirectories,
                options.ClangDefines,
                options.TargetPlatform,
                options.ClangArguments);

            if (!translationUnit.HasValue)
            {
                continue;
            }

            var abstractSyntaxTreeC = Explore(
                translationUnit.Value,
                options.IncludeDirectories,
                options.ExcludedHeaderFiles,
                options.OpaqueTypeNames,
                options.FunctionNamesWhitelist,
                options.TargetPlatform);

            Write(options.OutputFilePath, abstractSyntaxTreeC, options.TargetPlatform);

            builder.Add(options);
        }

        output.AbstractSyntaxTreesOptions = builder.ToImmutable();
    }

    private void InstallClang(NativeOperatingSystem operatingSystem)
    {
        BeginStep($"Install Clang {operatingSystem}");

        var installer = _services.GetService<ClangInstaller>()!;
        installer.Install(operatingSystem);

        EndStep();
    }

    private CXTranslationUnit? Parse(
        string inputFilePath,
        bool automaticallyFindSystemHeaders,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        NativePlatform targetPlatform,
        ImmutableArray<string> clangArguments)
    {
        BeginStep($"Parse {targetPlatform}");

        var clangArgumentsBuilder = _services.GetService<ClangArgumentsBuilder>()!;

        var arguments = clangArgumentsBuilder.Build(
            automaticallyFindSystemHeaders,
            includeDirectories,
            defines,
            targetPlatform,
            clangArguments);

        if (arguments == null)
        {
            EndStep();
            return null;
        }

        var parser = _services.GetService<ClangTranslationUnitParser>()!;
        var result = parser.Parse(
            Diagnostics, inputFilePath, arguments.Value);

        EndStep();
        return result;
    }

    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> excludedHeaderFiles,
        ImmutableArray<string> opaqueTypeNames,
        ImmutableArray<string> functionNamesWhitelist,
        NativePlatform platform)
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
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree, NativePlatform platform)
    {
        BeginStep($"Write {platform}");
        var cJsonSerializer = _services.GetService<CJsonSerializer>()!;
        cJsonSerializer.Write(abstractSyntaxTree, outputFilePath);
        EndStep();
    }
}
