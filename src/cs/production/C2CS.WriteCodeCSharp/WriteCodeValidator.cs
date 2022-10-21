// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Foundation.UseCases;
using C2CS.Foundation.UseCases.Exceptions;
using C2CS.Options;
using JetBrains.Annotations;

namespace C2CS.WriteCodeCSharp;

[PublicAPI]
public abstract class WriteCodeValidator<TOptions, TInput> : UseCaseValidator<TOptions, TInput>
    where TOptions : UseCaseOptions
{
    protected WriteCodeValidator(IFileSystem fileSystem)
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
                throw new UseCaseException(
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
                throw new UseCaseException($"Unknown platform '{platform}' for abstract syntax tree.");
            }

            builder.Add(filePath);
        }

        return builder.ToImmutable();
    }

    protected string OutputFilePath(string? outputFilePath)
    {
        if (string.IsNullOrEmpty(outputFilePath))
        {
            throw new UseCaseException("The output file path can not be an empty or null string.");
        }

        var result = FileSystem.Path.GetFullPath(outputFilePath);
        var directoryPath = FileSystem.Path.GetDirectoryName(outputFilePath);
        FileSystem.Directory.CreateDirectory(directoryPath);
        return result;
    }
}
