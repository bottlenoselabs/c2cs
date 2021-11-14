// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

public class CExtractAbstractSyntaxTreeRequest : UseCaseRequest
{
    public string InputFilePath { get; }

    public string OutputFilePath { get; }

    public bool AutomaticallyFindSoftwareDevelopmentKit { get; }

    public ImmutableArray<string> IncludeDirectories { get; }

    public ImmutableArray<string> IgnoredFiles { get; }

    public ImmutableArray<string> OpaqueTypes { get; }

    public ImmutableArray<string> Defines { get; }

    public int? Bitness { get; }

    public ImmutableArray<string> ClangArgs { get; }

    public ImmutableArray<string> WhitelistFunctionNames { get;  }

    public CExtractAbstractSyntaxTreeRequest(
        string inputFilePath,
        string outputFilePath,
        bool? automaticallyFindSoftwareDevelopmentKit,
        IEnumerable<string?>? includeDirectories,
        IEnumerable<string?>? ignoredFiles,
        IEnumerable<string?>? opaqueTypes,
        IEnumerable<string?>? defines,
        int? bitness,
        IEnumerable<string?>? clangArgs,
        string? whitelistFunctionsFilePath)
    {
        InputFilePath = inputFilePath;
        OutputFilePath = outputFilePath;
        AutomaticallyFindSoftwareDevelopmentKit = automaticallyFindSoftwareDevelopmentKit ?? true;
        IncludeDirectories = CreateIncludeDirectories(inputFilePath, includeDirectories);
        IgnoredFiles = ToImmutableArray(ignoredFiles);
        OpaqueTypes = ToImmutableArray(opaqueTypes);
        Defines = ToImmutableArray(defines);
        Bitness = bitness;
        ClangArgs = ToImmutableArray(clangArgs);
        WhitelistFunctionNames = CreateWhitelistFunctionNames(whitelistFunctionsFilePath);
    }

    private static ImmutableArray<string> CreateIncludeDirectories(
        string inputFilePath,
        IEnumerable<string?>? includeDirectories)
    {
        var result = ToImmutableArray(includeDirectories);

        if (result.IsDefaultOrEmpty)
        {
            var directoryPath = Path.GetDirectoryName(inputFilePath)!;
            if (string.IsNullOrEmpty(directoryPath))
            {
                directoryPath = Environment.CurrentDirectory;
            }

            result = new[]
            {
                directoryPath
            }.ToImmutableArray();
        }
        else
        {
            result = result.Select(Path.GetFullPath).ToImmutableArray();
        }

        return result;
    }

    private static ImmutableArray<string> ToImmutableArray(IEnumerable<string?>? enumerable)
    {
        var nonNull = enumerable?.ToArray() ?? Array.Empty<string>();
        var result =
            nonNull.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
        return result;
    }

    private static ImmutableArray<string> CreateWhitelistFunctionNames(string? whitelistFunctionsFilePath)
    {
        if (string.IsNullOrEmpty(whitelistFunctionsFilePath))
        {
            return ImmutableArray<string>.Empty;
        }

        if (!File.Exists(whitelistFunctionsFilePath))
        {
            return ImmutableArray<string>.Empty;
        }

        var fileContent = File.ReadAllText(whitelistFunctionsFilePath);
        var fileContentLines = fileContent.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

        var functionNames = ImmutableArray.CreateBuilder<string>();
        foreach (var line in fileContentLines)
        {
            string functionName = line.Contains("!") ? line.Split(new[] { "!" }, StringSplitOptions.RemoveEmptyEntries)[1] : line;
            functionNames.Add(functionName);
        }

        return functionNames.ToImmutable();
    }
}
