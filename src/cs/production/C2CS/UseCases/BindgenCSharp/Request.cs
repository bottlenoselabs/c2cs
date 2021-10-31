// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace C2CS.UseCases.BindgenCSharp;

public class Request : UseCaseRequest
{
    public string InputFilePath { get; }

    public string OutputFilePath { get; }

    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; }

    public ImmutableArray<string> IgnoredNames { get; }

    public string LibraryName { get; }

    public string ClassName { get; }

    public ImmutableArray<string> UsingNamespaces { get; }

    public Request(
        string inputFilePath,
        string outputFilePath,
        IEnumerable<string?>? typeAliases,
        string ignoredNamesFilePath,
        string libraryName,
        string className,
        IEnumerable<string?>? usingNamespaces)
    {
        InputFilePath = inputFilePath;
        OutputFilePath = outputFilePath;
        TypeAliases = CreateTypeAliases(typeAliases);
        IgnoredNames = CreateIgnoredNames(ignoredNamesFilePath);
        LibraryName = libraryName;
        ClassName = className;
        UsingNamespaces = CreateUsingNamespaces(usingNamespaces);
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

    private static ImmutableArray<string> CreateIgnoredNames(string ignoredNamesFilePath)
    {
        if (string.IsNullOrEmpty(ignoredNamesFilePath))
        {
            return ImmutableArray<string>.Empty;
        }

        var text = File.ReadAllText(ignoredNamesFilePath);
        var ignoredNamesSplit = text.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        var ignoredNames = ignoredNamesSplit
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x));
        return ignoredNames.ToImmutableArray();
    }

    private static ImmutableArray<string> CreateUsingNamespaces(IEnumerable<string?>? usingNamespaces)
    {
        if (usingNamespaces == null)
        {
            return ImmutableArray<string>.Empty;
        }

        var nonNull = usingNamespaces!
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>();
        return nonNull.ToImmutableArray();
    }
}
