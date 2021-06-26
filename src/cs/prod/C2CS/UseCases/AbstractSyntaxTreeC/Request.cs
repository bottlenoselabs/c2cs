// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class Request : UseCaseRequest
    {
        public FileInfo InputFile { get; }

        public FileInfo OutputFile { get; }

        public ImmutableArray<string> IncludeDirectories { get; }

        public FileInfo? ConfigurationFile { get; }

        public Request(FileInfo inputFile, FileInfo outputFile, IEnumerable<string?> includeDirectories, FileInfo configurationFile)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            ConfigurationFile = configurationFile;

            var includeDirectories2 = includeDirectories?.ToArray() ?? Array.Empty<string>();
            ImmutableArray<string> includeDirectoriesArray;
            if (includeDirectories2.Length == 0)
            {
                includeDirectoriesArray = ImmutableArray<string>.Empty;
            }
            else
            {
                var includeDirectoriesNonEmpty = includeDirectories2.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToArray();
                includeDirectoriesArray = includeDirectoriesNonEmpty.Any() ? includeDirectoriesNonEmpty.ToImmutableArray() : ImmutableArray<string>.Empty;
            }

            if (includeDirectoriesArray.IsDefaultOrEmpty)
            {
                var directoryPath = Path.GetDirectoryName(inputFile.FullName)!;
                includeDirectoriesArray = new[] { directoryPath }.ToImmutableArray();
            }
            else
            {
                includeDirectoriesArray = includeDirectoriesArray.Select(Path.GetFullPath).ToImmutableArray();
            }

            IncludeDirectories = includeDirectoriesArray;
        }
    }
}
