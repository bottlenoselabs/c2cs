// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Explore;
using C2CS.Contexts.ReadCodeC.Parse;
using C2CS.Data.C.Model;
using C2CS.Data.C.Serialization;
using C2CS.Foundation.UseCases;
using C2CS.Options;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC;

public sealed class UseCaseReadCodeC : UseCase<
    ReaderCCodeOptions, ReadCodeCInput, ReadCodeCOutput>
{
    private readonly ClangInstaller _clangInstaller;
    private readonly Explorer _explorer;
    private readonly CJsonSerializer _serializer;

    public UseCaseReadCodeC(
        ILogger<UseCaseReadCodeC> logger,
        ReadCodeCValidator validator,
        ClangInstaller clangInstaller,
        Explorer explorer,
        CJsonSerializer serializer)
        : base(logger, validator)
    {
        _clangInstaller = clangInstaller;
        _explorer = explorer;
        _serializer = serializer;
    }

    protected override void Execute(ReadCodeCInput input, ReadCodeCOutput output)
    {
        var builder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeInput>();

        if (!InstallClang())
        {
            return;
        }

        foreach (var options in input.AbstractSyntaxTreesOptionsList)
        {
            var abstractSyntaxTreeC = Explore(
                input.InputFilePath,
                Diagnostics,
                options.TargetPlatform,
                options.ParseOptions,
                options.ExplorerOptions);

            Write(options.OutputFilePath, abstractSyntaxTreeC, options.TargetPlatform);

            builder.Add(options);
        }

        output.AbstractSyntaxTreesOptions = builder.ToImmutable();
    }

    private bool InstallClang()
    {
        BeginStep("Install Clang");

        var isInstalled = _clangInstaller.Install(Native.OperatingSystem);

        EndStep();
        return isInstalled;
    }

    private CAbstractSyntaxTree Explore(
        string headerFilePath,
        DiagnosticCollection diagnostics,
        TargetPlatform platform,
        ParseOptions parseOptions,
        ExploreOptions exploreOptions)
    {
        BeginStep($"{platform}");

        var result = _explorer.AbstractSyntaxTree(
            headerFilePath,
            diagnostics,
            platform,
            parseOptions,
            exploreOptions);

        EndStep();
        return result;
    }

    private void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree, TargetPlatform platform)
    {
        BeginStep($"Write {platform}");

        _serializer.Write(abstractSyntaxTree, outputFilePath);

        EndStep();
    }
}
