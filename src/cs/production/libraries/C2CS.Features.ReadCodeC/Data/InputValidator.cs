// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Foundation.Executors;
using C2CS.Options;
using C2CS.ReadCodeC.Domain.Explore;
using C2CS.ReadCodeC.Domain.Parse;

namespace C2CS.ReadCodeC.Data;

public sealed class InputValidator : ExecutorInputValidator<ReaderCCodeOptions, Input>
{
    private readonly IFileSystem _fileSystem;

    public InputValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override Input Validate(ReaderCCodeOptions options)
    {
        var inputFilePath = VerifyInputFilePath(options.InputHeaderFilePath);
        var optionsList = OptionsList(options, inputFilePath);

        return new Input
        {
            InputFilePath = inputFilePath,
            AbstractSyntaxTreesOptionsList = optionsList
        };
    }

    private ImmutableArray<InputAbstractSyntaxTree> OptionsList(
        ReaderCCodeOptions configuration,
        string inputFilePath)
    {
        var optionsBuilder = ImmutableArray.CreateBuilder<InputAbstractSyntaxTree>();
        if (configuration.Platforms == null)
        {
            var abstractSyntaxTreeRequests = new Dictionary<TargetPlatform, ReaderCCodeOptionsPlatform>();
            var targetPlatform = Native.Platform;
            abstractSyntaxTreeRequests.Add(targetPlatform, new ReaderCCodeOptionsPlatform());
            configuration.Platforms = abstractSyntaxTreeRequests.ToImmutableDictionary();
        }

        foreach (var (targetPlatformString, configurationPlatform) in configuration.Platforms)
        {
            var options = Options(configuration, targetPlatformString, configurationPlatform, inputFilePath);
            optionsBuilder.Add(options);
        }

        return optionsBuilder.ToImmutable();
    }

    private InputAbstractSyntaxTree Options(
        ReaderCCodeOptions configuration,
        TargetPlatform targetPlatform,
        ReaderCCodeOptionsPlatform configurationPlatform,
        string inputFilePath)
    {
        var systemIncludeDirectories = VerifyImmutableArray(configuration.SystemIncludeDirectories);
        var systemIncludeDirectoriesPlatform =
            VerifySystemDirectoriesPlatform(configurationPlatform?.SystemIncludeFileDirectories, systemIncludeDirectories);

        var userIncludeDirectories = VerifyIncludeDirectories(configuration.UserIncludeDirectories, inputFilePath);
        var userIncludeDirectoriesPlatform =
            VerifyIncludeDirectoriesPlatform(
                configurationPlatform?.UserIncludeFileDirectories,
                inputFilePath,
                userIncludeDirectories);

        var frameworks = VerifyImmutableArray(configuration.Frameworks);
        var frameworksPlatform = VerifyFrameworks(configurationPlatform?.Frameworks, frameworks);

        var outputFilePath = VerifyOutputFilePath(configuration.OutputAbstractSyntaxTreesFileDirectory, targetPlatform);

        var headerFilesBlocked = VerifyBlockedHeaderFiles(configuration, configurationPlatform);
        var macroObjectNamesAllowed = VerifyImmutableArray(configuration.MacroObjectNamesAllowed).ToImmutableHashSet();
        var enumConstantNamesAllowed =
            VerifyImmutableArray(configuration.EnumConstantNamesAllowed).ToImmutableHashSet();
        var clangDefines = VerifyImmutableArray(configurationPlatform?.Defines);
        var clangArguments = VerifyImmutableArray(configurationPlatform?.ClangArguments);

        var passThroughTypedNames = VerifyImmutableArray(configuration.PassThroughTypeNames).ToImmutableHashSet();

        var inputAbstractSyntaxTree = new InputAbstractSyntaxTree
        {
            TargetPlatform = targetPlatform,
            OutputFilePath = outputFilePath,
            ExplorerOptions = new ExploreOptions
            {
                HeaderFilesBlocked = headerFilesBlocked,
                EnumConstantNamesAllowed = enumConstantNamesAllowed,
                IsEnabledLocationFullPaths = configuration.IsEnabledLocationFullPaths ?? false,
                IsEnabledEnumConstants = configuration.IsEnabledEnumConstants ?? true,
                IsEnabledEnumsDangling = configuration.IsEnabledEnumsDangling ?? false,
                IsEnabledAllowNamesWithPrefixedUnderscore =
                    configuration.IsEnabledAllowNamesWithPrefixedUnderscore ?? false,
                IsEnabledSystemDeclarations = configuration.IsEnabledSystemDeclarations ?? false,
                PassThroughTypeNames = passThroughTypedNames
            },
            ParseOptions = new ParseOptions
            {
                HeaderFilesBlocked = headerFilesBlocked,
                UserIncludeDirectories = userIncludeDirectoriesPlatform,
                SystemIncludeDirectories = systemIncludeDirectoriesPlatform,
                MacroObjectsDefines = clangDefines,
                AdditionalArguments = clangArguments,
                IsEnabledFindSystemHeaders = configuration.IsEnabledFindSystemHeaders ?? true,
                Frameworks = frameworksPlatform,
                IsEnabledSystemDeclarations = configuration.IsEnabledSystemDeclarations ?? false,
                IsEnabledMacroObjects = configuration.IsEnabledMacroObjects ?? true,
                IsEnabledSingleHeader = configuration.IsEnabledSingleHeader ?? true,
                MacroObjectNamesAllowed = macroObjectNamesAllowed
            }
        };

        return inputAbstractSyntaxTree;
    }

    private string VerifyInputFilePath(string? inputFilePath)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            throw new ExecutorException("The input file can not be null, empty, or whitespace.");
        }

        var filePath = _fileSystem.Path.GetFullPath(inputFilePath);

        if (!_fileSystem.File.Exists(filePath))
        {
            throw new ExecutorException($"The input file does not exist: `{filePath}`.");
        }

        return filePath;
    }

    private string VerifyOutputFilePath(
        string? outputFileDirectory,
        TargetPlatform targetPlatform)
    {
        string directoryPath;
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (string.IsNullOrEmpty(outputFileDirectory))
        {
            directoryPath = _fileSystem.Path.Combine(Environment.CurrentDirectory, "ast");
        }
        else
        {
            directoryPath = _fileSystem.Path.GetFullPath(outputFileDirectory);
        }

        var defaultFilePath = _fileSystem.Path.Combine(directoryPath, targetPlatform + ".json");
        return defaultFilePath;
    }

    private ImmutableArray<string> VerifyIncludeDirectories(
        ImmutableArray<string>? includeDirectories,
        string inputFilePath)
    {
        var result = VerifyImmutableArray(includeDirectories);

        if (result.IsDefaultOrEmpty)
        {
            var directoryPath = _fileSystem.Path.GetDirectoryName(inputFilePath)!;
            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = Environment.CurrentDirectory;
            }

            result = new[]
            {
                _fileSystem.Path.GetFullPath(directoryPath)
            }.ToImmutableArray();
        }
        else
        {
            result = result.Select(_fileSystem.Path.GetFullPath).ToImmutableArray();
        }

        foreach (var includeDirectory in result)
        {
            if (!_fileSystem.Directory.Exists(includeDirectory))
            {
                throw new ExecutorException($"The include directory does not exist: `{includeDirectory}`.");
            }
        }

        return result;
    }

    private ImmutableArray<string> VerifyIncludeDirectoriesPlatform(
        ImmutableArray<string>? includeDirectoriesPlatform,
        string inputFilePath,
        ImmutableArray<string> includeDirectoriesNonPlatform)
    {
        if (includeDirectoriesPlatform == null || includeDirectoriesPlatform.Value.IsDefaultOrEmpty)
        {
            return includeDirectoriesNonPlatform;
        }

        var directoriesPlatform = VerifyIncludeDirectories(includeDirectoriesPlatform, inputFilePath);
        var result = directoriesPlatform.AddRange(includeDirectoriesNonPlatform);
        return result;
    }

    private ImmutableArray<string> VerifySystemDirectoriesPlatform(
        ImmutableArray<string>? includeDirectoriesPlatform,
        ImmutableArray<string> includeDirectoriesNonPlatform)
    {
        var directoriesPlatform = VerifyImmutableArray(includeDirectoriesPlatform);
        var result = directoriesPlatform.AddRange(includeDirectoriesNonPlatform);
        return result;
    }

    private ImmutableArray<string> VerifyFrameworks(
        ImmutableArray<string>? platformFrameworks,
        ImmutableArray<string> frameworksNonPlatform)
    {
        var directoriesPlatform = VerifyImmutableArray(platformFrameworks);
        var result = directoriesPlatform.AddRange(frameworksNonPlatform);
        return result;
    }

    private ImmutableHashSet<string> VerifyBlockedHeaderFiles(
        ReaderCCodeOptions options,
        ReaderCCodeOptionsPlatform? optionsPlatform)
    {
        var headerFilesBlocked = ImmutableHashSet.CreateBuilder<string>();

        if (options.HeaderFilesBlocked != null)
        {
            foreach (var headerFileBlocked in options.HeaderFilesBlocked)
            {
                if (!string.IsNullOrEmpty(headerFileBlocked))
                {
                    headerFilesBlocked.Add(headerFileBlocked);
                }
            }
        }

        if (optionsPlatform != null && optionsPlatform.HeaderFilesBlocked != null)
        {
            foreach (var headerFileBlocked in optionsPlatform.HeaderFilesBlocked)
            {
                if (!string.IsNullOrEmpty(headerFileBlocked))
                {
                    headerFilesBlocked.Add(headerFileBlocked);
                }
            }
        }

        return headerFilesBlocked.ToImmutable();
    }

    private static ImmutableArray<string> VerifyImmutableArray(ImmutableArray<string>? array)
    {
        if (array == null || array.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var result = array.Value
            .Where(x => !string.IsNullOrEmpty(x)).ToImmutableArray();
        return result;
    }
}
