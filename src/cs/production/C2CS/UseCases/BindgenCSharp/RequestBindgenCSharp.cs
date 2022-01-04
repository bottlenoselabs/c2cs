// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.BindgenCSharp;

public class RequestBindgenCSharp : UseCaseRequest
{
    public RequestBindgenCSharp(
        string? inputFilePath,
        string? outputFilePath,
        string? libraryName,
        string? @namespace,
        string? className,
        ImmutableArray<(string? SourceName, string? TargetName)>? mappedTypeNames,
        ImmutableArray<string?>? ignoredTypeNames,
        string? headerCodeRegionFilePath,
        string? footerCodeRegionFilePath)
    {
        InputFilePath = VerifyInputFilePath(inputFilePath);
        OutputFilePath = VerifyOutputFilePath(outputFilePath, InputFilePath);
        ClassName = VerifyClassName(className, OutputFilePath);
        LibraryName = VerifyLibraryName(libraryName, ClassName);
        NamespaceName = VerifyNamespace(@namespace, LibraryName);
        TypeAliases = VerifyTypeAliases(mappedTypeNames);
        IgnoredTypeNames = VerifyIgnoredTypeNames(ignoredTypeNames);
        HeaderCodeRegion = VerifyHeaderCodeRegion(headerCodeRegionFilePath);
        FooterCodeRegion = VerifyFooterCodeRegion(footerCodeRegionFilePath);
    }

    public string InputFilePath { get; }

    public string OutputFilePath { get; }

    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

    public ImmutableArray<string> IgnoredTypeNames { get; }

    public string LibraryName { get; }

    public string ClassName { get; }

    public string NamespaceName { get; }

    public string HeaderCodeRegion { get; }

    public string FooterCodeRegion { get; }

    private static string VerifyInputFilePath(string? inputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath))
        {
            throw new UseCaseException("The input file can not be null, empty, or whitespace.");
        }

        if (!File.Exists(inputFilePath))
        {
            throw new UseCaseException($"The input file does not exist: {inputFilePath}");
        }

        return inputFilePath;
    }

    private static string VerifyOutputFilePath(string? outputFilePath, string inputFilePath)
    {
        if (!string.IsNullOrEmpty(outputFilePath))
        {
            return Path.GetFullPath(outputFilePath);
        }

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(inputFilePath);
        var defaultFilePath = Path.Combine(Environment.CurrentDirectory, $"{fileNameWithoutExtension}.cs");
        return defaultFilePath;
    }

    private static ImmutableArray<CSharpTypeAlias> VerifyTypeAliases(
        ImmutableArray<(string? SourceName, string? TargetName)>? mappedTypeNames)
    {
        if (mappedTypeNames == null || mappedTypeNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpTypeAlias>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpTypeAlias>();
        foreach (var (sourceName, targetName) in mappedTypeNames)
        {
            if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName))
            {
                continue;
            }

            var typeAlias = new CSharpTypeAlias
            {
                From = sourceName,
                To = targetName
            };

            builder.Add(typeAlias);
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> VerifyIgnoredTypeNames(ImmutableArray<string?>? ignoredTypeNames)
    {
        if (ignoredTypeNames == null || ignoredTypeNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var array = ignoredTypeNames.Value
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>();
        return array.ToImmutableArray();
    }

    private static string VerifyLibraryName(string? libraryName, string className)
    {
        return !string.IsNullOrEmpty(libraryName) ? libraryName : className;
    }

    private static string VerifyNamespace(string? @namespace, string libraryName)
    {
        return !string.IsNullOrEmpty(@namespace) ? @namespace : libraryName;
    }

    private static string VerifyClassName(string? className, string outputFilePath)
    {
        string result;
        if (string.IsNullOrEmpty(className))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilePath);
            var firstIndexOfPeriod = fileNameWithoutExtension.IndexOf('.', StringComparison.InvariantCulture);
            result = firstIndexOfPeriod == -1
                ? fileNameWithoutExtension
                : fileNameWithoutExtension[..firstIndexOfPeriod];
        }
        else
        {
            result = className;
        }

        return result;
    }

    private static string VerifyHeaderCodeRegion(string? headerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(headerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(headerCodeRegionFilePath);
        return code;
    }

    private static string VerifyFooterCodeRegion(string? footerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(footerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(footerCodeRegionFilePath);
        return code;
    }
}
