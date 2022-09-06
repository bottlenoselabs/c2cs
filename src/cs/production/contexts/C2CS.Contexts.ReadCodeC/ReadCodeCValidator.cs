// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Configuration;
using C2CS.Contexts.ReadCodeC.Explore;
using C2CS.Contexts.ReadCodeC.Parse;
using C2CS.Foundation;
using C2CS.Foundation.UseCases;
using C2CS.Foundation.UseCases.Exceptions;

namespace C2CS.Contexts.ReadCodeC;

public sealed class ReadCodeCValidator : UseCaseValidator<ConfigurationReadCodeC, ReadCodeCInput>
{
    private readonly IFileSystem _fileSystem;

    public ReadCodeCValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override ReadCodeCInput Validate(ConfigurationReadCodeC configuration)
    {
        var inputFilePath = VerifyInputFilePath(configuration.InputFilePath);
        var optionsList = OptionsList(configuration, inputFilePath);

        return new ReadCodeCInput
        {
            InputFilePath = inputFilePath,
            AbstractSyntaxTreesOptionsList = optionsList
        };
    }

    private ImmutableArray<ReadCodeCAbstractSyntaxTreeInput> OptionsList(
        ConfigurationReadCodeC configuration,
        string inputFilePath)
    {
        var optionsBuilder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeInput>();
        if (configuration.Platforms == null)
        {
            var abstractSyntaxTreeRequests = new Dictionary<TargetPlatform, ConfigurationReadCodeCPlatform>();
            var targetPlatform = Native.Platform;
            abstractSyntaxTreeRequests.Add(targetPlatform, new ConfigurationReadCodeCPlatform());
            configuration.Platforms = abstractSyntaxTreeRequests.ToImmutableDictionary();
        }

        foreach (var (targetPlatformString, configurationPlatform) in configuration.Platforms)
        {
            var options = Options(configuration, targetPlatformString, configurationPlatform, inputFilePath);
            optionsBuilder.Add(options);
        }

        return optionsBuilder.ToImmutable();
    }

    private ReadCodeCAbstractSyntaxTreeInput Options(
        ConfigurationReadCodeC configuration,
        TargetPlatform targetPlatform,
        ConfigurationReadCodeCPlatform configurationPlatform,
        string inputFilePath)
    {
        var systemIncludeDirectories = VerifyImmutableArray(configuration.SystemIncludeDirectories);
        var systemIncludeDirectoriesPlatform =
            VerifySystemDirectoriesPlatform(configurationPlatform?.SystemIncludeDirectories, systemIncludeDirectories);

        var userIncludeDirectories = VerifyIncludeDirectories(configuration.UserIncludeDirectories, inputFilePath);
        var userIncludeDirectoriesPlatform =
            VerifyIncludeDirectoriesPlatform(
                configurationPlatform?.UserIncludeDirectories,
                inputFilePath,
                userIncludeDirectories);

        var frameworks = VerifyImmutableArray(configuration.Frameworks);
        var frameworksPlatform = VerifyFrameworks(configurationPlatform?.Frameworks, frameworks);

        var outputFilePath = VerifyOutputFilePath(configuration.OutputFileDirectory, targetPlatform);

        var excludedHeaderFiles = VerifyImmutableArray(configurationPlatform?.HeaderFilesBlocked).ToImmutableHashSet();
        var opaqueTypeNames = VerifyImmutableArray(configuration.OpaqueTypeNames).ToImmutableHashSet();
        var functionNamesAllowed = VerifyImmutableArray(configuration.FunctionNamesAllowed).ToImmutableHashSet();
        var functionNamesBlocked = VerifyImmutableArray(configuration.FunctionNamesBlocked).ToImmutableHashSet();
        var macroObjectNamesAllowed = VerifyImmutableArray(configuration.MacroObjectNamesAllowed).ToImmutableHashSet();
        var enumConstantNamesAllowed =
            VerifyImmutableArray(configuration.EnumConstantNamesAllowed).ToImmutableHashSet();
        var clangDefines = VerifyImmutableArray(configurationPlatform?.Defines);
        var clangArguments = VerifyImmutableArray(configurationPlatform?.ClangArguments);

        var passThroughTypedNames = VerifyImmutableArray(configuration.PassThroughTypeNames).ToImmutableHashSet();

        var inputAbstractSyntaxTree = new ReadCodeCAbstractSyntaxTreeInput
        {
            TargetPlatform = targetPlatform,
            OutputFilePath = outputFilePath,
            ExplorerOptions = new ExploreOptions
            {
                HeaderFilesBlocked = excludedHeaderFiles,
                OpaqueTypesNames = opaqueTypeNames,
                FunctionNamesAllowed = functionNamesAllowed,
                FunctionNamesBlocked = functionNamesBlocked,
                EnumConstantNamesAllowed = enumConstantNamesAllowed,
                IsEnabledLocationFullPaths = configuration.IsEnabledLocationFullPaths ?? false,
                IsEnabledFunctions = configuration.IsEnabledFunctions ?? true,
                IsEnabledVariables = configuration.IsEnabledVariables ?? true,
                IsEnabledEnumConstants = configuration.IsEnabledEnumConstants ?? true,
                IsEnabledEnumsDangling = configuration.IsEnabledEnumsDangling ?? false,
                IsEnabledAllowNamesWithPrefixedUnderscore =
                    configuration.IsEnabledAllowNamesWithPrefixedUnderscore ?? false,
                IsEnabledSystemDeclarations = configuration.IsEnabledSystemDeclarations ?? false,
                PassThroughTypeNames = passThroughTypedNames
            },
            ParseOptions = new ParseOptions
            {
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
            throw new ConfigurationException("The input file can not be null, empty, or whitespace.");
        }

        var filePath = _fileSystem.Path.GetFullPath(inputFilePath);

        if (!_fileSystem.File.Exists(filePath))
        {
            throw new UseCaseException($"The input file does not exist: `{filePath}`.");
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
                throw new UseCaseException($"The include directory does not exist: `{includeDirectory}`.");
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
