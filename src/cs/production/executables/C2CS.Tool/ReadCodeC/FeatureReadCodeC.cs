// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using C2CS.Data.C.Serialization;
using C2CS.Foundation.Executors;
using C2CS.Options;
using C2CS.ReadCodeC.Data;
using C2CS.ReadCodeC.Data.Models;
using C2CS.ReadCodeC.Domain.Explore;
using C2CS.ReadCodeC.Domain.Parse;
using Microsoft.Extensions.Logging;
using Explorer = C2CS.ReadCodeC.Domain.Explore.Explorer;

namespace C2CS.ReadCodeC;

public sealed class FeatureReadCodeC : Executor<ReaderCCodeOptions, ReadCodeCInput, ReadCodeCOutput>
{
    private readonly ClangInstaller _clangInstaller;
    private readonly Explorer _explorer;
    private readonly CJsonSerializer _serializer;

    public FeatureReadCodeC(
        ILogger<FeatureReadCodeC> logger,
        ReadCodeCInputValidator inputValidator,
        ClangInstaller clangInstaller,
        Explorer explorer,
        CJsonSerializer serializer)
        : base(logger, inputValidator)
    {
        _clangInstaller = clangInstaller;
        _explorer = explorer;
        _serializer = serializer;
    }

    protected override void Execute(ReadCodeCInput input, ReadCodeCOutput output)
    {
        var builder = ImmutableArray.CreateBuilder<ReadCodeCInputAbstractSyntaxTree>();

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

        output.AbstractSyntaxTrees = builder.ToImmutable();
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
