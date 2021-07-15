// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.BindgenCSharp
{
    public class Request : UseCaseRequest
    {
        public FileInfo InputFile { get; }

        public FileInfo OutputFile { get; }

        public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

        public ImmutableArray<string> IgnoredTypeNames { get; }

        public string LibraryName { get; }

        public string ClassName { get; }

        public Request(
            FileInfo inputFile,
            FileInfo outputFile,
            IEnumerable<string?>? typeAliases,
            IEnumerable<string?>? ignoredTypeNames,
            string libraryName,
            string className)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            TypeAliases = CreateTypeAliases(typeAliases);
            IgnoredTypeNames = CreateIgnoredTypeNames(ignoredTypeNames);
            LibraryName = libraryName;
            ClassName = className;
        }

        private static ImmutableArray<CSharpTypeAlias> CreateTypeAliases(IEnumerable<string?>? typeAliases)
        {
            if (typeAliases == null)
            {
                return ImmutableArray<CSharpTypeAlias>.Empty;
            }

            var builder = ImmutableArray.CreateBuilder<CSharpTypeAlias>();
            foreach (var typeAliasString in typeAliases!)
            {
                if (string.IsNullOrEmpty(typeAliasString))
                {
                    continue;
                }

                var parse = typeAliasString.Split("->", StringSplitOptions.RemoveEmptyEntries);
                if (parse.Length != 2)
                {
                    continue;
                }

                var typeFrom = parse[0].Trim();
                var typeTo = parse[1].Trim();

                var typeAlias = new CSharpTypeAlias
                {
                    From = typeFrom,
                    To = typeTo
                };

                builder.Add(typeAlias);
            }

            return builder.ToImmutable();
        }

        private static ImmutableArray<string> CreateIgnoredTypeNames(IEnumerable<string?>? ignoredTypeNames)
        {
            if (ignoredTypeNames == null)
            {
                return ImmutableArray<string>.Empty;
            }

            var nonNull = ignoredTypeNames!
                .Select(x => x?.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .Cast<string>();
            return nonNull.ToImmutableArray();
        }
    }
}
