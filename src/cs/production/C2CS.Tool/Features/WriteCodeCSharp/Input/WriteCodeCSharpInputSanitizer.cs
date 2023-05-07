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
        var outputFilePath = OutputFilePath(unsanitizedInput.OutputCSharpCodeFilePath);
        var className = ClassName(unsanitizedInput.ClassName, outputFilePath);
        var libraryName = LibraryName(unsanitizedInput.LibraryName, className);
        var namespaceName = Namespace(unsanitizedInput.NamespaceName, libraryName);
        var typeAliases = TypeAliases(unsanitizedInput.MappedNames);
        var ignoredNames = IgnoredTypeNames(unsanitizedInput.IgnoredNames);
        var headerCodeRegion = HeaderCodeRegion(unsanitizedInput.HeaderCodeRegionFilePath);
        var footerCodeRegion = FooterCodeRegion(unsanitizedInput.FooterCodeRegionFilePath);
        var isEnabledFunctionPointers = unsanitizedInput.IsEnabledFunctionPointers ?? true;
        var isEnabledVerifyCSharpCodeCompiles = unsanitizedInput.IsEnabledVerifyCSharpCodeCompiles ?? true;
        var isEnabledGenerateCSharpRuntimeCode = unsanitizedInput.IsEnabledGeneratingRuntimeCode ?? true;
        var isEnabledLibraryImportAttribute = unsanitizedInput.IsEnabledLibraryImport ?? true;
        var isEnabledPointersAsReferences = unsanitizedInput.IsEnabledPointersAsReferences ?? true;

        return new WriteCodeCSharpInput
        {
            InputFilePath = inputFilePath,
            OutputFilePath = outputFilePath,
            MapperOptions = new CSharpCodeMapperOptions
            {
                TypeRenames = typeAliases,
                IgnoredNames = ignoredNames
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
                IsEnabledPointersAsReferences = isEnabledPointersAsReferences
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

    private string OutputFilePath(string? outputFilePath)
    {
        if (string.IsNullOrEmpty(outputFilePath))
        {
            throw new ToolInputSanitizationException("The output file path can not be an empty or null string.");
        }

        var result = _fileSystem.Path.GetFullPath(outputFilePath);
        var directoryPath = _fileSystem.Path.GetDirectoryName(outputFilePath);

        if (!string.IsNullOrEmpty(directoryPath))
        {
            _fileSystem.Directory.CreateDirectory(directoryPath!);
        }

        return result;
    }

    private static ImmutableArray<CSharpTypeRename> TypeAliases(
        ImmutableArray<WriteCSharpCodeOptionsMappedName>? mappedNames)
    {
        if (mappedNames == null || mappedNames.Value.IsDefaultOrEmpty)
        {
            return ImmutableArray<CSharpTypeRename>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<CSharpTypeRename>();
        foreach (var mappedName in mappedNames)
        {
            if (string.IsNullOrEmpty(mappedName.Source) || string.IsNullOrEmpty(mappedName.Target))
            {
                continue;
            }

            var typeAlias = new CSharpTypeRename
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
