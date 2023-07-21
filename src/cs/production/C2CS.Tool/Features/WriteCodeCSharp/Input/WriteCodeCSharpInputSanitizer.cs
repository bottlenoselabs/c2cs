// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Features.WriteCodeCSharp.Domain.Mapper;
using C2CS.Features.WriteCodeCSharp.Input.Sanitized;
using C2CS.Features.WriteCodeCSharp.Input.Unsanitized;
using C2CS.Foundation.Tool;

namespace C2CS.Features.WriteCodeCSharp.Input;

public sealed class WriteCodeCSharpInputSanitizer : ToolInputSanitizer<WriteCSharpCodeOptions, WriteCodeCSharpInput>
{
    private readonly IFileSystem _fileSystem;

    public WriteCodeCSharpInputSanitizer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override WriteCodeCSharpInput Sanitize(WriteCSharpCodeOptions unsanitizedInput)
    {
        var inputFilePath = InputFilePath(unsanitizedInput.InputAbstractSyntaxTreeFilePath);
        var outputFileDirectory = OutputFileDirectory(unsanitizedInput.OutputCSharpCodeFileDirectory);
        var className = ClassName(unsanitizedInput.ClassName);
        var libraryName = LibraryName(unsanitizedInput.LibraryName, className);
        var namespaceName = NamespaceName(unsanitizedInput.NamespaceName, libraryName);
        var mappedTypeNames = MappedNames(unsanitizedInput.MappedNames);
        var ignoredNames = IgnoredTypeNames(unsanitizedInput.IgnoredNames);
        var headerCodeRegion = HeaderCodeRegion(unsanitizedInput.HeaderCodeRegionFilePath);
        var footerCodeRegion = FooterCodeRegion(unsanitizedInput.FooterCodeRegionFilePath);
        var isEnabledFunctionPointers = unsanitizedInput.IsEnabledFunctionPointers ?? true;
        var isEnabledVerifyCSharpCodeCompiles = unsanitizedInput.IsEnabledVerifyCSharpCodeCompiles ?? true;
        var isEnabledGenerateCSharpRuntimeCode = unsanitizedInput.IsEnabledGeneratingRuntimeCode ?? true;
        var isEnabledLibraryImportAttribute = unsanitizedInput.IsEnabledLibraryImport ?? false;
        var mappedCNamespaces = MappedNames(unsanitizedInput.MappedCNamespaces);
        var isEnabledGenerateAssemblyAttributes = unsanitizedInput.IsEnabledGenerateAssemblyAttributes ?? true;
        var isEnabledIdiomaticCSharp = unsanitizedInput.IsEnabledIdiomaticCSharp ?? false;
        var isEnabledEnumValueNamesUpperCase = unsanitizedInput.IsEnabledEnumValueNamesUpperCase ?? true;
        var isEnabledFileScopedNamespace = unsanitizedInput.IsEnabledFileScopedNamespace ?? true;

        return new WriteCodeCSharpInput
        {
            InputFilePath = inputFilePath,
            OutputFileDirectory = outputFileDirectory,
            MapperOptions = new CSharpCodeMapperOptions
            {
                MappedTypeNames = mappedTypeNames,
                IgnoredNames = ignoredNames,
                MappedCNamespaces = mappedCNamespaces,
                IsEnabledIdiomaticCSharp = isEnabledIdiomaticCSharp,
                IsEnabledEnumValueNamesUpperCase = isEnabledEnumValueNamesUpperCase
            },
            GeneratorOptions = new CSharpCodeGeneratorOptions
            {
                ClassName = className,
                LibraryName = libraryName,
                NamespaceName = namespaceName,
                HeaderCodeRegion = headerCodeRegion,
                FooterCodeRegion = footerCodeRegion,
                IsEnabledFunctionPointers = isEnabledFunctionPointers,
                IsEnabledVerifyCSharpCodeCompiles = isEnabledVerifyCSharpCodeCompiles,
                IsEnabledGenerateCSharpRuntimeCode = isEnabledGenerateCSharpRuntimeCode,
                IsEnabledLibraryImportAttribute = isEnabledLibraryImportAttribute,
                IsEnabledGenerateAssemblyAttributes = isEnabledGenerateAssemblyAttributes,
                IsEnabledFileScopedNamespace = isEnabledFileScopedNamespace
            }
        };
    }

    private string InputFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ToolInputSanitizationException(
                $"The abstract syntax tree input directory can not be an empty or null string.");
        }

        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        if (!_fileSystem.File.Exists(fullFilePath))
        {
            throw new ToolInputSanitizationException(
                $"The cross-platform abstract syntax tree input file path '{fullFilePath}' does not exist.");
        }

        return fullFilePath;
    }

    private string OutputFileDirectory(string? outputFileDirectory)
    {
        if (string.IsNullOrEmpty(outputFileDirectory))
        {
            outputFileDirectory = Environment.CurrentDirectory;
        }

        var fullDirectoryPath = _fileSystem.Path.GetFullPath(outputFileDirectory);

        if (!string.IsNullOrEmpty(fullDirectoryPath))
        {
            _fileSystem.Directory.CreateDirectory(fullDirectoryPath!);
        }

        return fullDirectoryPath;
    }

    private static ImmutableArray<CSharpMappedName> MappedNames(
        ImmutableArray<WriteCSharpCodeOptionsMappedName>? mappedNames)
    {
        if (mappedNames == null || mappedNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpMappedName>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpMappedName>();
        foreach (var mappedName in mappedNames)
        {
            if (string.IsNullOrEmpty(mappedName.Source) || string.IsNullOrEmpty(mappedName.Target))
            {
                continue;
            }

            var typeAlias = new CSharpMappedName
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

    private static string NamespaceName(string? @namespace, string libraryName)
    {
        return !string.IsNullOrEmpty(@namespace) ? @namespace : libraryName;
    }

    private static string ClassName(string? className)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ToolInputSanitizationException(
                $"The class name can not be an empty or null string.");
        }

        return className;
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
