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

        public ImmutableArray<string> IgnoredFiles { get; }

        public ImmutableArray<string> OpaqueTypes { get; }

        public FileInfo? ConfigurationFile { get; }

        public Request(
            FileInfo inputFile,
            FileInfo outputFile,
            IEnumerable<string?>? includeDirectories,
            IEnumerable<string?>? ignoredFiles,
            IEnumerable<string?>? opaqueTypes,
            FileInfo configurationFile)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            ConfigurationFile = configurationFile;
            IncludeDirectories = CreateIncludeDirectories(inputFile, includeDirectories);
            IgnoredFiles = CreateIgnoredFiles(ignoredFiles);
            OpaqueTypes = CreateOpaqueTypes(opaqueTypes);
        }

        private static ImmutableArray<string> CreateIncludeDirectories(
            FileInfo inputFile,
            IEnumerable<string?>? includeDirectories)
        {
            var nonNull = includeDirectories?.ToArray() ?? Array.Empty<string>();
            ImmutableArray<string> result;
            if (nonNull.Length == 0)
            {
                result = ImmutableArray<string>.Empty;
            }
            else
            {
                result = nonNull.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
            }

            if (result.IsDefaultOrEmpty)
            {
                var directoryPath = Path.GetDirectoryName(inputFile.FullName)!;
                result = new[] {directoryPath}.ToImmutableArray();
            }
            else
            {
                result = result.Select(Path.GetFullPath).ToImmutableArray();
            }

            return result;
        }

        private ImmutableArray<string> CreateIgnoredFiles(IEnumerable<string?>? ignoredFiles)
        {
            var nonNull = ignoredFiles?.ToArray() ?? Array.Empty<string>();
            var result =
                nonNull.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
            return result;
        }

        private ImmutableArray<string> CreateOpaqueTypes(IEnumerable<string?>? opaqueTypes)
        {
            var nonNull = opaqueTypes?.ToArray() ?? Array.Empty<string>();
            var result =
                nonNull.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
            return result;
        }
    }
}
