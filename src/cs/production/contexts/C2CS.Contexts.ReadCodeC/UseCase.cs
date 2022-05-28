// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Data.Serialization;
using C2CS.Contexts.ReadCodeC.Domain;
using C2CS.Contexts.ReadCodeC.Domain.Explore;
using C2CS.Contexts.ReadCodeC.Domain.Parse;
using C2CS.Foundation.UseCases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC;

public sealed class UseCase : UseCase<
    ReadCodeCConfiguration, ReadCodeCInput, ReadCodeCOutput>
{
    private readonly IServiceProvider _services;

    public UseCase(
        ILogger<UseCase> logger, IServiceProvider services, ReadCodeCValidator validator)
        : base(logger, services, validator)
    {
        _services = services;
    }

    protected override void Execute(ReadCodeCInput input, ReadCodeCOutput output)
    {
        var builder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeInput>();

        foreach (var options in input.AbstractSyntaxTreesOptionsList)
        {
            var parseResult = Parse(
                input.InputFilePath,
                options.TargetPlatform,
                options.ParseOptions);

            if (parseResult == null)
            {
                continue;
            }

            var abstractSyntaxTreeC = Explore(
                parseResult.TranslationUnit,
                parseResult.LinkedPaths,
                parseResult.MacroObjects,
                options.TargetPlatform,
                options.ExplorerOptions,
                options.ParseOptions.UserIncludeDirectories);

            clang_disposeTranslationUnit(parseResult.TranslationUnit);

            Write(options.OutputFilePath, abstractSyntaxTreeC, options.TargetPlatform);

            builder.Add(options);
        }

        output.AbstractSyntaxTreesOptions = builder.ToImmutable();
    }

    private ParseResult? Parse(
        string inputFilePath,
        TargetPlatform targetPlatform,
        ParseOptions options)
    {
        BeginStep($"Parse {targetPlatform}");

        var installer = _services.GetService<ClangInstaller>()!;
        var isInstalled = installer.Install(Native.OperatingSystem);
        if (!isInstalled)
        {
            return null;
        }

        var parser = _services.GetService<Parser>()!;
        var translationUnit = parser.TranslationUnit(
            inputFilePath, Diagnostics, targetPlatform, options);
        var macroObjects = parser.MacroObjects(translationUnit, Diagnostics, targetPlatform, options);
        var linkedPaths = parser.GetLinkedPaths();

        var result = new ParseResult
        {
            TranslationUnit = translationUnit,
            MacroObjects = macroObjects,
            LinkedPaths = linkedPaths
        };

        parser.Cleanup();

        EndStep();
        return result;
    }

    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        ImmutableDictionary<string, string> linkedPaths,
        ImmutableArray<CMacroObject> macroObjects,
        TargetPlatform platform,
        ExplorerOptions options,
        ImmutableArray<string> userIncludeDirectories)
    {
        BeginStep($"{platform}");

        var explorer = _services.GetService<Explorer>()!;
        var result = explorer.AbstractSyntaxTree(
            platform, options, macroObjects, userIncludeDirectories, translationUnit, linkedPaths);

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
