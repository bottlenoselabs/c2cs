// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public class Input : UseCaseInput<Output>
{
    public string InputFilePath { get; }

    public string OutputFilePath { get; }

    public bool IsEnabledFindSdk { get; }

    public RuntimePlatform TargetPlatform { get; }

    public ImmutableArray<string> IncludeDirectories { get; }

    public ImmutableArray<string> ExcludedHeaderFiles { get; }

    public ImmutableArray<string> OpaqueTypeNames { get; }

    public ImmutableArray<string> FunctionNamesWhitelist { get; }

    public ImmutableArray<string> ClangDefines { get; }

    public ImmutableArray<string> ClangArguments { get; }

    public Input(ConfigurationExtractAbstractSyntaxTreeC configuration)
    {
        InputFilePath = VerifyInputFilePath(configuration.InputFilePath);
        TargetPlatform = VerifyTargetPlatform(configuration.TargetPlatform);
        OutputFilePath = VerifyOutputFilePath(configuration.OutputFileDirectory, TargetPlatform);
        IsEnabledFindSdk = configuration.IsEnabledFindSdk ?? true;
        IncludeDirectories = VerifyIncludeDirectories(configuration.IncludeDirectories, InputFilePath);
        ExcludedHeaderFiles = VerifyImmutableArray(configuration.ExcludedHeaderFiles);
        OpaqueTypeNames = VerifyImmutableArray(configuration.OpaqueTypeNames);
        FunctionNamesWhitelist = VerifyImmutableArray(configuration.FunctionNamesWhiteList);
        ClangDefines = VerifyImmutableArray(configuration.Defines);
        ClangArguments = VerifyImmutableArray(configuration.ClangArguments);
    }

    private static string VerifyInputFilePath(string? inputFilePath)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            throw new ConfigurationException("The input file can not be null, empty, or whitespace.");
        }

        return Path.GetFullPath(inputFilePath);
    }

    private static string VerifyOutputFilePath(string? outputFileDirectory, RuntimePlatform targetPlatform)
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

    private static RuntimePlatform VerifyTargetPlatform(string? targetPlatform)
    {
        if (targetPlatform == null)
        {
            return RuntimePlatform.Host;
        }

        var runtimePlatform = RuntimePlatform.FromString(targetPlatform);
        if (runtimePlatform == RuntimePlatform.Unknown)
        {
            throw new UseCaseException($"Unknown target platform '{runtimePlatform}'.");
        }

        return runtimePlatform;
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
