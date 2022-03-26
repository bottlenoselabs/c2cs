// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractAbstractSyntaxTreeValidator : UseCaseValidator<ExtractAbstractSyntaxTreeRequest, ExtractAbstractSyntaxTreeInput>
{
    public override ExtractAbstractSyntaxTreeInput Validate(ExtractAbstractSyntaxTreeRequest request)
    {
        var inputFilePath = VerifyInputFilePath(request.InputFilePath);
        var targetPlatform = VerifyTargetPlatform(request.TargetPlatform);
        var outputFilePath = VerifyOutputFilePath(request.OutputFileDirectory, targetPlatform);
        var isEnabledFindSdk = request.IsEnabledFindSdk ?? true;
        var includeDirectories = VerifyIncludeDirectories(request.IncludeDirectories, inputFilePath);
        var excludedHeaderFiles = VerifyImmutableArray(request.ExcludedHeaderFiles);
        var opaqueTypeNames = VerifyImmutableArray(request.OpaqueTypeNames);
        var functionNamesWhitelist = VerifyImmutableArray(request.FunctionNamesWhiteList);
        var clangDefines = VerifyImmutableArray(request.Defines);
        var clangArguments = VerifyImmutableArray(request.ClangArguments);

        return new ExtractAbstractSyntaxTreeInput
        {
            InputFilePath = inputFilePath,
            TargetPlatform = targetPlatform,
            OutputFilePath = outputFilePath,
            IsEnabledFindSdk = isEnabledFindSdk,
            IncludeDirectories = includeDirectories,
            ExcludedHeaderFiles = excludedHeaderFiles,
            OpaqueTypeNames = opaqueTypeNames,
            FunctionNamesWhitelist = functionNamesWhitelist,
            ClangDefines = clangDefines,
            ClangArguments = clangArguments
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
