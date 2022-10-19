// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Contexts.WriteCodeCSharp.CodeGenerator;
using C2CS.Contexts.WriteCodeCSharp.Mapper;
using C2CS.Data.CSharp.Model;
using C2CS.Options;

namespace C2CS.Contexts.WriteCodeCSharp;

public sealed class WriteCodeCSharpValidator : WriteCodeValidator<WriterOptionsCSharpCode, WriteCodeCSharpInput>
{
    public WriteCodeCSharpValidator(IFileSystem fileSystem)
        : base(fileSystem)
    {
    }

    public override WriteCodeCSharpInput Validate(WriterOptionsCSharpCode options)
    {
        var inputFilePaths = InputFilePaths(options.InputFileDirectory);
        var outputFilePath = OutputFilePath(options.OutputFilePath);
        var className = ClassName(options.ClassName, outputFilePath);
        var libraryName = LibraryName(options.LibraryName, className);
        var namespaceName = Namespace(options.NamespaceName, libraryName);
        var typeAliases = TypeAliases(options.MappedNames);
        var ignoredNames = IgnoredTypeNames(options.IgnoredNames);
        var headerCodeRegion = HeaderCodeRegion(options.HeaderCodeRegionFilePath);
        var footerCodeRegion = FooterCodeRegion(options.FooterCodeRegionFilePath);
        var isEnabledPreCompile = options.IsEnabledPreCompile ?? true;
        var isEnabledFunctionPointers = options.IsEnabledFunctionPointers ?? true;

        return new WriteCodeCSharpInput
        {
            InputFilePaths = inputFilePaths,
            OutputFilePath = outputFilePath,
            MapperOptions = new CSharpCodeMapperOptions
            {
                TypeAliases = typeAliases,
                IgnoredNames = ignoredNames
            },
            GeneratorOptions = new CSharpCodeGeneratorOptions
            {
                ClassName = className,
                LibraryName = libraryName,
                NamespaceName = namespaceName,
                HeaderCodeRegion = headerCodeRegion,
                FooterCodeRegion = footerCodeRegion,
                IsEnabledPreCompile = isEnabledPreCompile,
                IsEnabledFunctionPointers = isEnabledFunctionPointers
            }
        };
    }

    private static ImmutableArray<CSharpTypeAlias> TypeAliases(
        ImmutableArray<WriterOptionsCSharpCodeMappedName>? mappedNames)
    {
        if (mappedNames == null || mappedNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpTypeAlias>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpTypeAlias>();
        foreach (var mappedName in mappedNames)
        {
            var typeAlias = new CSharpTypeAlias
            {
                Source = mappedName.Source,
                Target = mappedName.Target
            };
            builder.Add(typeAlias);
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<string> IgnoredTypeNames(ImmutableArray<string?>? ignoredTypeNames)
    {
        if (ignoredTypeNames == null || ignoredTypeNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<string>.Empty;
        }

        var array = ignoredTypeNames.Value
            .Where(x => !string.IsNullOrEmpty(x))
            .Cast<string>();
        return array.ToImmutableArray();
    }

    private static string LibraryName(string? libraryName, string className)
    {
        return !string.IsNullOrEmpty(libraryName) ? libraryName : className;
    }

    private static string Namespace(string? @namespace, string libraryName)
    {
        return !string.IsNullOrEmpty(@namespace) ? @namespace : libraryName;
    }

    private static string ClassName(string? className, string outputFilePath)
    {
        string result;
        if (string.IsNullOrEmpty(className))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputFilePath);
            var firstIndexOfPeriod = fileNameWithoutExtension.IndexOf('.', StringComparison.InvariantCulture);
            result = firstIndexOfPeriod == -1
                ? fileNameWithoutExtension
                : fileNameWithoutExtension[..firstIndexOfPeriod];
        }
        else
        {
            result = className;
        }

        return result;
    }

    private static string HeaderCodeRegion(string? headerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(headerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(headerCodeRegionFilePath);
        return code;
    }

    private static string FooterCodeRegion(string? footerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(footerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(footerCodeRegionFilePath);
        return code;
    }
}
