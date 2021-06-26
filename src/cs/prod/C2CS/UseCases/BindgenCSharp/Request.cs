// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace C2CS.UseCases.BindgenCSharp
{
    public class Request : UseCaseRequest
    {
        public FileInfo InputFile { get; }

        public FileInfo OutputFile { get; }

        public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

        public string LibraryName { get; }

        public Request(
            FileInfo inputFile, FileInfo outputFile, IEnumerable<string?>? typeAliases, string libraryName)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            TypeAliases = CreateTypeAliases(typeAliases);
            LibraryName = libraryName;
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
    }
}
