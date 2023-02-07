// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Data.CSharp.Model;
using C2CS.Foundation.Executors;
using C2CS.Options;
using C2CS.WriteCodeCSharp.Data.Models;
using C2CS.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.WriteCodeCSharp.Domain.Mapper;
using JetBrains.Annotations;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class WriteCodeCSharpInputValidator : WriteCodeCSharpInputValidator<WriterCSharpCodeOptions, WriteCodeCSharpInput>
{
    public WriteCodeCSharpInputValidator(IFileSystem fileSystem)
        : base(fileSystem)
    {
    }

    public override WriteCodeCSharpInput Validate(WriterCSharpCodeOptions options)
    {
        var inputFilePaths = InputFilePaths(options.InputAbstractSyntaxTreesFileDirectory);
        var outputFilePath = OutputFilePath(options.OutputCSharpCodeFilePath);
        var className = ClassName(options.ClassName, outputFilePath);
        var libraryName = LibraryName(options.LibraryName, className);
        var namespaceName = Namespace(options.NamespaceName, libraryName);
        var typeAliases = TypeAliases(options.MappedNames);
        var ignoredNames = IgnoredTypeNames(options.IgnoredNames);
        var headerCodeRegion = HeaderCodeRegion(options.HeaderCodeRegionFilePath);
        var footerCodeRegion = FooterCodeRegion(options.FooterCodeRegionFilePath);
        var isEnabledPreCompile = options.IsEnabledPreCompile ?? true;
        var isEnabledFunctionPointers = options.IsEnabledFunctionPointers ?? true;
        var isEnabledVerifyCSharpCodeCompiles = options.IsEnabledVerifyCSharpCodeCompiles ?? true;

        return new WriteCodeCSharpInput
        {
            InputFilePaths = inputFilePaths,
            OutputFilePath = outputFilePath,
            MapperOptions = new CSharpCodeMapperOptions
            {
                TypeRenames = typeAliases,
                IgnoredNames = ignoredNames
            },
            GeneratorOptions = new CSharpCodeGeneratorOptions
            {
                ClassName = className,
                LibraryName = libraryName,
                NamespaceName = namespaceName,
                HeaderCodeRegion = headerCodeRegion,
                FooterCodeRegion = footerCodeRegion,
                IsEnabledPreCompile = isEnabledPreCompile,
                IsEnabledFunctionPointers = isEnabledFunctionPointers,
                IsEnabledVerifyCSharpCodeCompiles = isEnabledVerifyCSharpCodeCompiles
            }
        };
    }

    private static ImmutableArray<CSharpTypeRename> TypeAliases(
        ImmutableArray<WriterCSharpCodeOptionsMappedName>? mappedNames)
    {
        if (mappedNames == null || mappedNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpTypeRename>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpTypeRename>();
        foreach (var mappedName in mappedNames)
        {
            var typeAlias = new CSharpTypeRename
            {
                Source = mappedName.Source,
                Target = mappedName.Target
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

[PublicAPI]
public abstract class WriteCodeCSharpInputValidator<TOptions, TInput> : ExecutorInputValidator<TOptions, TInput>
    where TOptions : ExecutorOptions
{
    protected WriteCodeCSharpInputValidator(IFileSystem fileSystem)
    {
        FileSystem = fileSystem;
    }

    public IFileSystem FileSystem { get; }

    protected ImmutableArray<string> InputFilePaths(string? inputDirectoryPath)
    {
        string directoryPath;
        if (string.IsNullOrWhiteSpace(inputDirectoryPath))
        {
            directoryPath = FileSystem.Path.Combine(Environment.CurrentDirectory, "ast");
        }
        else
        {
            directoryPath = FileSystem.Path.GetFullPath(inputDirectoryPath);
        }

        if (!FileSystem.Directory.Exists(directoryPath))
        {
            var directory = FileSystem.Directory.CreateDirectory(directoryPath);
            if (!directory.Exists)
            {
                throw new ExecutorException(
                    $"The abstract syntax tree input directory '{directoryPath}' does not exist.");
            }
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        var filePaths = FileSystem.Directory.EnumerateFiles(directoryPath, "*.json");
        foreach (var filePath in filePaths)
        {
            var fileName = FileSystem.Path.GetFileName(filePath);
            var platformString = fileName.Replace(".json", string.Empty, StringComparison.InvariantCulture);
            var platform = new TargetPlatform(platformString);
            if (platform == TargetPlatform.Unknown)
            {
                throw new ExecutorException($"Unknown platform '{platform}' for abstract syntax tree.");
            }

            builder.Add(filePath);
        }

        return builder.ToImmutable();
    }

    protected string OutputFilePath(string? outputFilePath)
    {
        if (string.IsNullOrEmpty(outputFilePath))
        {
            throw new ExecutorException("The output file path can not be an empty or null string.");
        }

        var result = FileSystem.Path.GetFullPath(outputFilePath);
        var directoryPath = FileSystem.Path.GetDirectoryName(outputFilePath);
        FileSystem.Directory.CreateDirectory(directoryPath);
        return result;
    }
}
