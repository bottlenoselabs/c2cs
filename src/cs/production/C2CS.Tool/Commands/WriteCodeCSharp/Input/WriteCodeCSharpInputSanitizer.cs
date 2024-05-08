// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using bottlenoselabs.Common.Tools;
using C2CS.Commands.WriteCodeCSharp.Data;
using C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Commands.WriteCodeCSharp.Domain.Mapper;
using C2CS.Commands.WriteCodeCSharp.Input.Sanitized;
using C2CS.Commands.WriteCodeCSharp.Input.Unsanitized;
using NuGet.Frameworks;

namespace C2CS.Commands.WriteCodeCSharp.Input;

public sealed class WriteCodeCSharpInputSanitizer : ToolInputSanitizer<WriteCSharpCodeInput, WriteCodeCSharpInput>
{
    private readonly IFileSystem _fileSystem;

    public WriteCodeCSharpInputSanitizer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public override WriteCodeCSharpInput Sanitize(WriteCSharpCodeInput unsanitizedInput)
    {
        var inputFilePath = InputFilePath(unsanitizedInput.InputCrossPlatformFfiFilePath);
        var outputFileDirectory = OutputFileDirectory(unsanitizedInput.OutputCSharpCodeFileDirectory);

        return new WriteCodeCSharpInput
        {
            InputFilePath = inputFilePath,
            OutputFileDirectory = outputFileDirectory,
            MapperOptions = MapperOptions(unsanitizedInput),
            GeneratorOptions = GeneratorOptions(unsanitizedInput)
        };
    }

    private static CSharpCodeMapperOptions MapperOptions(WriteCSharpCodeInput unsanitizedInput)
    {
        return new CSharpCodeMapperOptions
        {
            MappedTypeNames = MappedNames(unsanitizedInput.MappedNames),
            IgnoredNames = IgnoredTypeNames(unsanitizedInput.IgnoredNames),
            MappedCNamespaces = MappedNames(unsanitizedInput.MappedCNamespaces),
            IsEnabledIdiomaticCSharp = unsanitizedInput.IsEnabledIdiomaticCSharp ?? false,
            IsEnabledEnumValueNamesUpperCase = unsanitizedInput.IsEnabledEnumValueNamesUpperCase ?? true
        };
    }

    private CSharpCodeGeneratorOptions GeneratorOptions(WriteCSharpCodeInput unsanitizedInput)
    {
        var className = ClassName(unsanitizedInput.ClassName);
        var libraryName = LibraryName(unsanitizedInput.LibraryName, className);
        var targetFramework = TargetFramework(unsanitizedInput.TargetFrameworkMoniker);

        return new CSharpCodeGeneratorOptions
        {
            TargetFramework = targetFramework,
            ClassName = className,
            LibraryName = libraryName,
            NamespaceName = NamespaceName(unsanitizedInput.NamespaceName),
            HeaderCodeRegion = HeaderCodeRegion(unsanitizedInput.HeaderCodeRegionFilePath),
            FooterCodeRegion = FooterCodeRegion(unsanitizedInput.FooterCodeRegionFilePath),
            IsEnabledGenerateCSharpRuntimeCode = unsanitizedInput.IsEnabledGeneratingRuntimeCode ?? true,
            IsEnabledFunctionPointers = IsEnabledFunctionPointers(unsanitizedInput, targetFramework),
            IsEnabledRuntimeMarshalling = IsEnabledRuntimeMarshalling(unsanitizedInput, targetFramework),
            IsEnabledFileScopedNamespace = IsEnabledFileScopedNamespace(unsanitizedInput, targetFramework),
            IsEnabledLibraryImportAttribute = IsEnabledLibraryImport(unsanitizedInput, targetFramework)
        };
    }

    private string InputFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ToolInputSanitizationException(
                $"The FFI input file path can not be an empty or a null string.");
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
        ImmutableArray<WriteCSharpCodeInputMappedName>? mappedNames)
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

    private static string NamespaceName(string? @namespace)
    {
        return !string.IsNullOrEmpty(@namespace) ? @namespace : string.Empty;
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

    private NuGetFramework TargetFramework(string? targetFrameworkMoniker)
    {
        if (string.IsNullOrEmpty(targetFrameworkMoniker))
        {
            targetFrameworkMoniker = "net8.0";
        }

        // NuGet uses the word "folder" for what Microsoft calls a TFM
        var nuGetFramework = NuGetFramework.ParseFolder(targetFrameworkMoniker);
        if (nuGetFramework.HasProfile)
        {
            throw new InvalidOperationException(
                $"The Target Framework Moniker (TFM) '{targetFrameworkMoniker}' is not supported because it has a profile. Remove the profile from the TFM and try again.");
        }

        if (nuGetFramework.HasPlatform)
        {
            throw new InvalidOperationException(
                $"The Target Framework Moniker (TFM) '{targetFrameworkMoniker}' is not supported because it has a platform. Remove the platform parts from the TFM and try again.");
        }

        if (nuGetFramework.IsUnsupported)
        {
            throw new InvalidOperationException(
                $"The Target Framework Moniker (TFM) '{targetFrameworkMoniker}' is not supported.");
        }

        return nuGetFramework;
    }

    private static bool IsEnabledFunctionPointers(WriteCSharpCodeInput unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // Function pointers are supported only in C# 9+ and .NET 5+
        var isAtLeastNetCoreV5 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 5 };
        if (!isAtLeastNetCoreV5)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledFunctionPointers ?? true;
    }

    private bool IsEnabledRuntimeMarshalling(WriteCSharpCodeInput unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // Disabling runtime marshalling is only supported in .NET 7+
        var isAtLeastNetCoreV7 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 7 };
        if (!isAtLeastNetCoreV7)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledRuntimeMarshalling ?? false;
    }

    private static bool IsEnabledFileScopedNamespace(WriteCSharpCodeInput unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // File scoped namespaces are only supported in .NET 6+
        var isAtLeastNetCoreV6 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 6 };
        if (!isAtLeastNetCoreV6)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledFileScopedNamespace ?? true;
    }

    private static bool IsEnabledLibraryImport(WriteCSharpCodeInput unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // LibraryImport is only supported in .NET 7+
        var isAtLeastNetCoreV7 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 7 };
        if (!isAtLeastNetCoreV7)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledLibraryImport ?? true;
    }
}
