// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class Request : UseCaseRequest
    {
        public FileInfo InputFile { get; }

        public FileInfo OutputFile { get; }

        public bool AutomaticallyFindSoftwareDevelopmentKit { get; }

        public ImmutableArray<string> IncludeDirectories { get; }

        public ImmutableArray<string> IgnoredFiles { get; }

        public ImmutableArray<string> OpaqueTypes { get; }

        public ImmutableArray<string> Defines { get; }

        public int Bitness { get; }

        public ImmutableArray<string> ClangArgs { get; }

        public Request(
            FileInfo inputFile,
            FileInfo outputFile,
            bool? automaticallyFindSoftwareDevelopmentKit,
            IEnumerable<string?>? includeDirectories,
            IEnumerable<string?>? ignoredFiles,
            IEnumerable<string?>? opaqueTypes,
            IEnumerable<string?>? defines,
            int? bitness,
            IEnumerable<string?>? clangArgs)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            AutomaticallyFindSoftwareDevelopmentKit = automaticallyFindSoftwareDevelopmentKit ?? true;
            IncludeDirectories = CreateIncludeDirectories(inputFile, includeDirectories);
            IgnoredFiles = ToImmutableArray(ignoredFiles);
            OpaqueTypes = ToImmutableArray(opaqueTypes);
            Defines = ToImmutableArray(defines);
            Bitness = CreateBitness(bitness);
            ClangArgs = ToImmutableArray(clangArgs);
        }

        private static ImmutableArray<string> CreateIncludeDirectories(
            FileInfo inputFile,
            IEnumerable<string?>? includeDirectories)
        {
            var result = ToImmutableArray(includeDirectories);

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

        private static int CreateBitness(int? bitness)
        {
            if (bitness != null)
            {
                return bitness.Value;
            }

            return RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.X64 ? 64 : 32;
        }

        private static ImmutableArray<string> ToImmutableArray(IEnumerable<string?>? enumerable)
        {
            var nonNull = enumerable?.ToArray() ?? Array.Empty<string>();
            var result =
                nonNull.Where(x => !string.IsNullOrEmpty(x)).Cast<string>().ToImmutableArray();
            return result;
        }
    }
}
