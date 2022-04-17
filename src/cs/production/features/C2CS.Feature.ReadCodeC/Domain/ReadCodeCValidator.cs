// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Foundation;
using C2CS.Foundation.UseCases;
using C2CS.Foundation.UseCases.Exceptions;

namespace C2CS.Feature.ReadCodeC.Domain;

public sealed class ReadCodeCValidator : UseCaseValidator<ReadCodeCConfiguration, ReadCodeCInput>
{
    public override ReadCodeCInput Validate(ReadCodeCConfiguration configuration)
    {
        var inputFilePath = VerifyInputFilePath(configuration.InputFilePath);

        var optionsBuilder = ImmutableArray.CreateBuilder<ReadCodeCAbstractSyntaxTreeOptions>();
        if (configuration.ConfigurationPlatforms == null)
        {
            var abstractSyntaxTreeRequests = new Dictionary<string, ReadCodeCConfigurationPlatform?>();
            var targetPlatform = Native.Platform;
            abstractSyntaxTreeRequests.Add(targetPlatform.ToString(), null);
            configuration.ConfigurationPlatforms = abstractSyntaxTreeRequests;
        }

        foreach (var (targetPlatformString, configurationPlatform) in configuration.ConfigurationPlatforms)
        {
            var targetPlatform = VerifyTargetPlatform(targetPlatformString);
            var outputFilePath = VerifyOutputFilePath(
                configurationPlatform?.OutputFileDirectory ?? configuration.OutputFileDirectory, targetPlatform);
            var systemIncludeDirectories =
                VerifyImmutableArray(configurationPlatform?.SystemIncludeDirectories);
            var includeDirectories =
                VerifyIncludeDirectories(configurationPlatform?.IncludeDirectories, inputFilePath);
            var excludedHeaderFiles = VerifyImmutableArray(configurationPlatform?.ExcludedHeaderFiles);
            var opaqueTypeNames = VerifyImmutableArray(configurationPlatform?.OpaqueTypeNames);
            var functionNamesWhitelist = VerifyImmutableArray(configurationPlatform?.FunctionNamesWhiteList);
            var clangDefines = VerifyImmutableArray(configurationPlatform?.Defines);
            var clangArguments = VerifyImmutableArray(configurationPlatform?.ClangArguments);

            var inputAbstractSyntaxTree = new ReadCodeCAbstractSyntaxTreeOptions
            {
                TargetPlatform = targetPlatform,
                OutputFilePath = outputFilePath,
                SystemIncludeDirectories = systemIncludeDirectories,
                IncludeDirectories = includeDirectories,
                ExcludedHeaderFiles = excludedHeaderFiles,
                OpaqueTypeNames = opaqueTypeNames,
                FunctionNamesWhitelist = functionNamesWhitelist,
                ClangDefines = clangDefines,
                ClangArguments = clangArguments
            };

            optionsBuilder.Add(inputAbstractSyntaxTree);
        }

        return new ReadCodeCInput
        {
            InputFilePath = inputFilePath,
            AbstractSyntaxTreesOptions = optionsBuilder.ToImmutable()
        };
    }

    private static string VerifyInputFilePath(string? inputFilePath)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            throw new ConfigurationException("The input file can not be null, empty, or whitespace.");
        }

        var filePath = Path.GetFullPath(inputFilePath);

        if (!File.Exists(filePath))
        {
            throw new UseCaseException($"The input file does not exist: `{filePath}`.");
        }

        return filePath;
    }

    private static string VerifyOutputFilePath(string? outputFileDirectory, TargetPlatform targetPlatform)
    {
        string directoryPath;
        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (string.IsNullOrEmpty(outputFileDirectory))
        {
            directoryPath = Path.Combine(Environment.CurrentDirectory, "ast");
        }
        else
        {
            directoryPath = Path.GetFullPath(outputFileDirectory);
        }

        var defaultFilePath = Path.Combine(directoryPath, targetPlatform + ".json");
        return defaultFilePath;
    }

    private static TargetPlatform VerifyTargetPlatform(string? targetPlatformString)
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

    private static ImmutableArray<string> VerifyIncludeDirectories(
        ImmutableArray<string?>? includeDirectories,
        string inputFilePath)
    {
        var result = VerifyImmutableArray(includeDirectories);

        if (result.IsDefaultOrEmpty)
        {
            var directoryPath = Path.GetDirectoryName(inputFilePath)!;
            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = Environment.CurrentDirectory;
            }

            result = new[]
            {
                Path.GetFullPath(directoryPath)
            }.ToImmutableArray();
        }
        else
        {
            result = result.Select(Path.GetFullPath).ToImmutableArray();
        }

        foreach (var includeDirectory in result)
        {
            if (!Directory.Exists(includeDirectory))
            {
                throw new UseCaseException($"The include directory does not exist: `{includeDirectory}`.");
            }
        }

        return result;
    }

    private static ImmutableArray<string> VerifyImmutableArray(ImmutableArray<string?>? array)
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
