// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using SearchOption = System.IO.SearchOption;

namespace C2CS.Tests.Helpers;

[ExcludeFromCodeCoverage]
public class FileSystemHelper
{
    private readonly IFileSystem _fileSystem;
    private string? _gitRepositoryRootDirectoryPath;

    public string GitRepositoryRootDirectoryPath => _gitRepositoryRootDirectoryPath ??= FindGitRepositoryRootDirectoryPath();

    public FileSystemHelper(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string GetFullFilePath(string relativeFilePath)
    {
        var rootDirectoryPath = GitRepositoryRootDirectoryPath;
        var filePath = _fileSystem.Path.Combine(rootDirectoryPath, relativeFilePath);

        if (!_fileSystem.File.Exists(filePath))
        {
            throw new InvalidOperationException($"Could not find file path: {filePath}");
        }

        return filePath;
    }

    public string GetFullDirectoryPath(string relativeDirectoryPath)
    {
        var rootDirectoryPath = GitRepositoryRootDirectoryPath;
        var directoryPath = _fileSystem.Path.Combine(rootDirectoryPath, relativeDirectoryPath);
        if (!_fileSystem.Directory.Exists(directoryPath))
        {
            throw new InvalidOperationException($"Could not find directory path: {relativeDirectoryPath}");
        }

        return directoryPath;
    }

    private string FindGitRepositoryRootDirectoryPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var directoryInfo = _fileSystem.DirectoryInfo.New(baseDirectory);
        while (true)
        {
            var files = directoryInfo.GetFiles(".gitignore", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                return directoryInfo.FullName;
            }

            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null)
            {
                return string.Empty;
            }
        }
    }
}
