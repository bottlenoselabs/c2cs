// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Contexts.ReadCodeC.Domain.Explore;
using C2CS.Contexts.ReadCodeC.Domain.Parse;
using C2CS.Foundation;
using C2CS.Foundation.UseCases;
using C2CS.Foundation.UseCases.Exceptions;

namespace C2CS.Contexts.ReadCodeC.Domain;

public sealed class ReadCodeCValidator : UseCaseValidator<ReadCodeCConfiguration, ReadCodeCInput>
{
    private readonly IFileSystem _fileSystem;

    public ReadCodeCValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override ReadCodeCInput Validate(ReadCodeCConfiguration configuration)
    {
        var inputFilePath = VerifyInputFilePath(configuration.InputFilePath);
        var optionsList = OptionsList(configuration, inputFilePath);

        return new ReadCodeCInput
        {
            InputFilePath = inputFilePath,
            AbstractSyntaxTreesOptionsList = optionsList
        };
    }

    private ImmutableArray<ReadCodeCAbstractSyntaxTreeOptions> OptionsList(
        ReadCodeCConfiguration configuration,
        string inputFilePath)
    {
        var optionsBuilder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeOptions>();
        if (configuration.ConfigurationPlatforms == null)
        {
            var abstractSyntaxTreeRequests = new Dictionary<string, ReadCodeCConfigurationPlatform?>();
            var targetPlatform = Native.Platform;
            abstractSyntaxTreeRequests.Add(targetPlatform.ToString(), null);
            configuration.ConfigurationPlatforms = abstractSyntaxTreeRequests;
        }

        foreach (var keyValurPair in configuration.ConfigurationPlatforms)
        {
            var targetPlatformString = keyValurPair.Key;
            var configurationPlatform = keyValurPair.Value;
            var options = Options(configuration, targetPlatformString, configurationPlatform, inputFilePath);
            optionsBuilder.Add(options);
        }

        return optionsBuilder.ToImmutable();
    }

    private ReadCodeCAbstractSyntaxTreeOptions Options(
        ReadCodeCConfiguration configuration,
        string targetPlatformString,
        ReadCodeCConfigurationPlatform? configurationPlatform,
        string inputFilePath)
    {
        var systemIncludeDirectories = VerifyImmutableArray(configuration.SystemIncludeDirectories);
        var systemIncludeDirectoriesPlatform =
            VerifySystemDirectoriesPlatform(configurationPlatform?.SystemIncludeDirectories, systemIncludeDirectories);

        var userIncludeDirectories = VerifyIncludeDirectories(configuration.UserIncludeDirectories, inputFilePath);
        var userIncludeDirectoriesPlatform =
            VerifyIncludeDirectoriesPlatform(configurationPlatform?.UserIncludeDirectories, inputFilePath, userIncludeDirectories);

        var frameworks = VerifyImmutableArray(configuration.Frameworks);
        var frameworksPlatform = VerifyFrameworks(configurationPlatform?.Frameworks, frameworks);

        var targetPlatform = VerifyTargetPlatform(targetPlatformString);
        var outputFilePath = VerifyOutputFilePath(configuration.OutputFileDirectory, targetPlatform);

        var excludedHeaderFiles = VerifyImmutableArray(configurationPlatform?.HeaderFilesBlocked);
        var opaqueTypeNames = VerifyImmutableArray(configuration.OpaqueTypeNames);
        var functionNamesAllowed = VerifyImmutableArray(configuration.FunctionNamesAllowed);
        var clangDefines = VerifyImmutableArray(configurationPlatform?.Defines);
        var clangArguments = VerifyImmutableArray(configurationPlatform?.ClangArguments);

        var inputAbstractSyntaxTree = new ReadCodeCAbstractSyntaxTreeOptions
        {
            TargetPlatform = targetPlatform,
            OutputFilePath = outputFilePath,
            ExplorerOptions = new ExplorerOptions
            {
                HeaderFilesBlocked = excludedHeaderFiles,
                OpaqueTypesNames = opaqueTypeNames,
                FunctionNamesAllowed = functionNamesAllowed,
                IsEnabledLocationFullPaths = configuration.IsEnabledLocationFullPaths ?? false,
                IsEnabledMacroObjects = configuration.IsEnabledMacroObjects ?? false,
                IsEnabledFunctions = configuration.IsEnabledFunctions ?? true,
                IsEnabledVariables = false, // TODO: transpile variables
                IsEnabledEnumsDangling = configuration.IsEnabledEnumsDangling ?? false,
                IsEnabledAllowNamesWithPrefixedUnderscore = configuration.IsEnabledAllowNamesWithPrefixedUnderscore ?? false,
                IsEnabledSystemDeclarations = configuration.IsEnabledSystemDeclarations ?? false
            },
            ParseOptions = new ParseOptions
            {
                UserIncludeDirectories = userIncludeDirectoriesPlatform,
                SystemIncludeDirectories = systemIncludeDirectoriesPlatform,
                MacroObjectsDefines = clangDefines,
                AdditionalArguments = clangArguments,
                IsEnabledFindSystemHeaders = configuration.IsEnabledFindSystemHeaders ?? true,
                Frameworks = frameworksPlatform
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

    private string VerifyOutputFilePath(string? outputFileDirectory, TargetPlatform targetPlatform)
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

    private TargetPlatform VerifyTargetPlatform(string? targetPlatformString)
    {
        if (string.IsNullOrEmpty(targetPlatformString))
        {
            throw new UseCaseException("Platform can not be null.");
        }

        var platform = new TargetPlatform(targetPlatformString);

        if (platform.Architecture == NativeArchitecture.Unknown && platform.OperatingSystem == NativeOperatingSystem.Unknown)
        {
            throw new UseCaseException($"Unknown platform `{platform}`.");
        }

        if (platform.OperatingSystem == NativeOperatingSystem.Unknown)
        {
            throw new UseCaseException($"Unknown operating system for platform '{platform}'.");
        }

        if (platform.Architecture == NativeArchitecture.Unknown)
        {
            throw new UseCaseException($"Unknown architecture for platform '{platform}'.");
        }

        return platform;
    }

    private ImmutableArray<string> VerifyIncludeDirectories(
        ImmutableArray<string?>? includeDirectories,
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
        ImmutableArray<string?>? includeDirectoriesPlatform,
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
        ImmutableArray<string?>? includeDirectoriesPlatform,
        ImmutableArray<string> includeDirectoriesNonPlatform)
    {
        var directoriesPlatform = VerifyImmutableArray(includeDirectoriesPlatform);
        var result = directoriesPlatform.AddRange(includeDirectoriesNonPlatform);
        return result;
    }

    private ImmutableArray<string> VerifyFrameworks(
        ImmutableArray<string?>? platformFrameworks, ImmutableArray<string> frameworksNonPlatform)
    {
        var directoriesPlatform = VerifyImmutableArray(platformFrameworks);
        var result = directoriesPlatform.AddRange(frameworksNonPlatform);
        return result;
    }

    private ImmutableArray<string> VerifyImmutableArray(ImmutableArray<string?>? array)
    {
        if (array == null || array.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var result = array.Value
            .Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
        return result;
    }
}
