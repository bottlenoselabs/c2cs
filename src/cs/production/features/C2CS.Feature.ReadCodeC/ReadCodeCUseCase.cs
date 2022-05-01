// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Model;
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

        foreach (var options in input.AbstractSyntaxTreesOptionsList)
        {
            var translationUnit = Parse(
                input.InputFilePath,
                options.TargetPlatform,
                options.ParseOptions);

            if (!translationUnit.HasValue)
            {
                continue;
            }

            var abstractSyntaxTreeC = Explore(
                translationUnit.Value,
                options.TargetPlatform,
                options.ExploreOptions,
                options.ParseOptions.UserIncludeDirectories);

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
        TargetPlatform targetPlatform,
        ParseOptions options)
    {
        BeginStep($"Parse {targetPlatform}");

        var clangArgumentsBuilder = _services.GetService<ClangArgumentsBuilder>()!;

        var arguments = clangArgumentsBuilder.Build(
            Diagnostics,
            targetPlatform,
            options);

        if (arguments == null)
        {
            EndStep();
            return null;
        }

        var parser = _services.GetService<TranslationUnitParser>()!;
        var result = parser.Parse(
            Diagnostics, inputFilePath, arguments.Value);

        clangArgumentsBuilder.Cleanup();

        EndStep();
        return result;
    }

    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        TargetPlatform platform,
        ExploreOptions options,
        ImmutableArray<string> userIncludeDirectories)
    {
        BeginStep($"Extract {platform}");

        var context = new ExplorerContext(Diagnostics, platform, options, userIncludeDirectories);
        var clangExplorer = _services.GetService<TranslationUnitExplorer>()!;
        var result = clangExplorer.AbstractSyntaxTree(
            context, translationUnit);

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
