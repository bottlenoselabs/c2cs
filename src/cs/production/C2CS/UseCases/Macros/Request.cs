// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.Macros
{
    public class Request : UseCaseRequest
    {
        public string InputFilePath { get; }

        public bool AutomaticallyFindSoftwareDevelopmentKit { get; }

        public ImmutableArray<string> IncludeDirectories { get; }

        public Request(
            string inputFilePath,
            bool? automaticallyFindSoftwareDevelopmentKit,
            IEnumerable<string?>? includeDirectories)
        {
            InputFilePath = inputFilePath;
            AutomaticallyFindSoftwareDevelopmentKit = automaticallyFindSoftwareDevelopmentKit ?? true;
            IncludeDirectories = CreateIncludeDirectories(inputFilePath, includeDirectories);
        }

        private static ImmutableArray<string> CreateIncludeDirectories(
            string inputFilePath,
            IEnumerable<string?>? includeDirectories)
        {
            var result = ToImmutableArray(includeDirectories);

            if (result.IsDefaultOrEmpty)
            {
                var directoryPath = Path.GetDirectoryName(inputFilePath)!;
                result = new[] {directoryPath}.ToImmutableArray();
            }
            else
            {
                result = result.Select(Path.GetFullPath).ToImmutableArray();
            }

            return result;
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
