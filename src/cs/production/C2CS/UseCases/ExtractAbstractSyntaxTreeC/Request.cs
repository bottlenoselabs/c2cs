// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.ExtractAbstractSyntaxTreeC;

public class Request : UseCaseRequest
{
    public Request(
        string? inputFilePath,
        string? outputFilePath,
        bool? isEnabledFindSdk,
        int? machineBitWidth,
        ImmutableArray<string?>? includeDirectories,
        ImmutableArray<string?>? excludedHeaderFiles,
        ImmutableArray<string?>? opaqueTypeNames,
        ImmutableArray<string?>? functionNamesWhitelist,
        ImmutableArray<string?>? defines,
        ImmutableArray<string?>? clangArgs)
    {
        InputFilePath = VerifyInputFilePath(inputFilePath);
        OutputFilePath = VerifyOutputFilePath(outputFilePath);
        IsEnabledFindSdk = isEnabledFindSdk ?? true;
        MachineBitWidth = VerifyMachineBitWidth(machineBitWidth);
        IncludeDirectories = VerifyIncludeDirectories(includeDirectories, InputFilePath);
        ExcludedHeaderFiles = VerifyImmutableArray(excludedHeaderFiles);
        OpaqueTypeNames = VerifyImmutableArray(opaqueTypeNames);
        FunctionNamesWhitelist = VerifyImmutableArray(functionNamesWhitelist);
        ClangDefines = VerifyImmutableArray(defines);
        ClangArguments = VerifyImmutableArray(clangArgs);
    }

    public string InputFilePath { get; }

    public string OutputFilePath { get; }

    public bool IsEnabledFindSdk { get; }

    public int MachineBitWidth { get; }

    public ImmutableArray<string> IncludeDirectories { get; }

    public ImmutableArray<string> ExcludedHeaderFiles { get; }

    public ImmutableArray<string> OpaqueTypeNames { get; }

    public ImmutableArray<string> FunctionNamesWhitelist { get; }

    public ImmutableArray<string> ClangDefines { get; }

    public ImmutableArray<string> ClangArguments { get; }

    private static string VerifyInputFilePath(string? inputFilePath)
    {
        if (string.IsNullOrEmpty(inputFilePath))
        {
            throw new ProgramConfigurationException("The input file can not be null, empty, or whitespace.");
        }

        return Path.GetFullPath(inputFilePath);
    }

    private static string VerifyOutputFilePath(string? outputFilePath)
    {
        if (!string.IsNullOrEmpty(outputFilePath))
        {
            return Path.GetFullPath(outputFilePath);
        }

        var defaultFilePath = Path.GetTempFileName();
        return defaultFilePath;
    }

    private static int VerifyMachineBitWidth(int? machineBitWidth)
    {
        if (machineBitWidth == null)
        {
            return Platform.Architecture switch
            {
                RuntimeArchitecture.ARM32 or RuntimeArchitecture.X86 => 32,
                RuntimeArchitecture.ARM64 or RuntimeArchitecture.X64 => 64,
                _ => throw new UseCaseException("Unknown runtime architecture.")
            };
        }

        return machineBitWidth.Value;
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
