// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace C2CS.Tests.Verify.Helpers;

[ExcludeFromCodeCoverage]
public class FileSystemHelper(IFileSystem fileSystem)
{
    private string? _gitRepositoryRootDirectoryPath;

    public IFileSystem FileSystem { get; } = fileSystem;

    public string GitRepositoryRootDirectoryPath => _gitRepositoryRootDirectoryPath ??= FindGitRepositoryRootDirectoryPath();

    public string GetFullDirectoryPath(string relativeDirectoryPath)
    {
        var rootDirectoryPath = GitRepositoryRootDirectoryPath;
        var directoryPath = FileSystem.Path.Combine(rootDirectoryPath, relativeDirectoryPath);
        if (!FileSystem.Directory.Exists(directoryPath))
        {
            throw new InvalidOperationException($"Could not find directory path: {relativeDirectoryPath}");
        }

        return directoryPath;
    }

    private string FindGitRepositoryRootDirectoryPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var directoryInfo = FileSystem.DirectoryInfo.New(baseDirectory);
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
