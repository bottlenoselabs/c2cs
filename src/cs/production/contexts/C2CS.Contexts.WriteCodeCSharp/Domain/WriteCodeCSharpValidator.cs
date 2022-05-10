// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Foundation.UseCases;
using C2CS.Foundation.UseCases.Exceptions;

namespace C2CS.Contexts.WriteCodeCSharp.Domain;

public sealed class WriteCodeCSharpValidator : UseCaseValidator<WriteCodeCSharpConfiguration, WriteCodeCSharpInput>
{
    private readonly IFileSystem _fileSystem;

    public WriteCodeCSharpValidator(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override WriteCodeCSharpInput Validate(WriteCodeCSharpConfiguration configuration)
    {
        var inputFilePaths = InputFilePaths(configuration.InputFileDirectory);
        var outputFilePath = OutputFilePath(configuration.OutputFilePath);
        var className = ClassName(configuration.ClassName, outputFilePath);
        var libraryName = LibraryName(configuration.LibraryName, className);
        var namespaceName = Namespace(configuration.NamespaceName, libraryName);
        var typeAliases = TypeAliases(configuration.MappedTypeNames);
        var ignoredNames = IgnoredTypeNames(configuration.IgnoredNames);
        var headerCodeRegion = HeaderCodeRegion(configuration.HeaderCodeRegionFilePath);
        var footerCodeRegion = FooterCodeRegion(configuration.FooterCodeRegionFilePath);

        return new WriteCodeCSharpInput
        {
            InputFilePaths = inputFilePaths,
            OutputFilePath = outputFilePath,
            ClassName = className,
            LibraryName = libraryName,
            NamespaceName = namespaceName,
            TypeAliases = typeAliases,
            IgnoredNames = ignoredNames,
            HeaderCodeRegion = headerCodeRegion,
            FooterCodeRegion = footerCodeRegion
        };
    }

    private ImmutableArray<string> InputFilePaths(string? inputDirectoryPath)
    {
        string directoryPath;
        if (string.IsNullOrWhiteSpace(inputDirectoryPath))
        {
            directoryPath = _fileSystem.Path.Combine(Environment.CurrentDirectory, "ast");
        }
        else
        {
            directoryPath = _fileSystem.Path.GetFullPath(inputDirectoryPath);
        }

        if (!_fileSystem.Directory.Exists(directoryPath))
        {
            var directory = _fileSystem.Directory.CreateDirectory(directoryPath);
            if (!directory.Exists)
            {
                throw new UseCaseException($"The abstract syntax tree input directory '{directoryPath}' does not exist.");
            }
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        var filePaths = _fileSystem.Directory.EnumerateFiles(directoryPath);
        foreach (var filePath in filePaths)
        {
            var fileName = _fileSystem.Path.GetFileName(filePath);
            var platformString = fileName.Replace(".json", string.Empty, StringComparison.InvariantCulture);
            var platform = new TargetPlatform(platformString);
            if (platform == TargetPlatform.Unknown)
            {
                throw new UseCaseException($"Unknown platform '{platform}' for abstract syntax tree.");
            }

            builder.Add(filePath);
        }

        return builder.ToImmutable();
    }

    private string OutputFilePath(string? outputFilePath)
    {
        if (string.IsNullOrEmpty(outputFilePath))
        {
            throw new UseCaseException($"The output file path can not be an empty or null string.");
        }

        var result = _fileSystem.Path.GetFullPath(outputFilePath);
        var directoryPath = _fileSystem.Path.GetDirectoryName(outputFilePath);
        _fileSystem.Directory.CreateDirectory(directoryPath);
        return result;
    }

    private static ImmutableArray<CSharpTypeAlias> TypeAliases(
        ImmutableArray<(string Source, string Target)>? mappedTypeNames)
    {
        if (mappedTypeNames == null || mappedTypeNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpTypeAlias>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpTypeAlias>();
        foreach (var (source, target) in mappedTypeNames)
        {
            var typeAlias = new CSharpTypeAlias()
            {
                Source = source,
                Target = target
            };
            builder.Add(typeAlias);
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> IgnoredTypeNames(ImmutableArray<string?>? ignoredTypeNames)
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

    private static string LibraryName(string? libraryName, string className)
    {
        return !string.IsNullOrEmpty(libraryName) ? libraryName : className;
    }

    private static string Namespace(string? @namespace, string libraryName)
    {
        return !string.IsNullOrEmpty(@namespace) ? @namespace : libraryName;
    }

    private static string ClassName(string? className, string outputFilePath)
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

    private static string HeaderCodeRegion(string? headerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(headerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(headerCodeRegionFilePath);
        return code;
    }

    private static string FooterCodeRegion(string? footerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(footerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(footerCodeRegionFilePath);
        return code;
    }
}
