// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using bottlenoselabs.Common.Tools;
using NuGet.Frameworks;

namespace C2CS.GenerateCSharpCode;

public sealed class InputSanitizer(IFileSystem fileSystem) : ToolInputSanitizer<InputUnsanitized, InputSanitized>
{
    public override InputSanitized Sanitize(InputUnsanitized unsanitizedInput)
    {
        var inputFilePath = InputFilePath(unsanitizedInput.InputCrossPlatformFfiFilePath);
        var outputFileDirectory = OutputFileDirectory(unsanitizedInput.OutputCSharpCodeFileDirectory);
        var className = ClassName(unsanitizedInput.ClassName);
        var libraryName = LibraryName(unsanitizedInput.LibraryName, className);
        var targetFramework = TargetFramework(unsanitizedInput.TargetFrameworkMoniker);

        return new InputSanitized
        {
            InputFilePath = inputFilePath,
            OutputFileDirectory = outputFileDirectory,
            TargetFramework = targetFramework,
            ClassName = className,
            LibraryName = libraryName,
            NamespaceName = NamespaceName(unsanitizedInput.NamespaceName),
            CodeRegionHeader = HeaderCodeRegion(unsanitizedInput.HeaderCodeRegionFilePath),
            CodeRegionFooter = FooterCodeRegion(unsanitizedInput.FooterCodeRegionFilePath),
            MappedNames = MappedNames(unsanitizedInput.MappedNames),
            IsEnabledGenerateCSharpRuntimeCode = unsanitizedInput.IsEnabledGeneratingRuntimeCode ?? true,
            IsEnabledFunctionPointers = IsEnabledFunctionPointers(unsanitizedInput, targetFramework),
            IsEnabledRuntimeMarshalling = IsEnabledRuntimeMarshalling(unsanitizedInput, targetFramework),
            IsEnabledFileScopedNamespace = IsEnabledFileScopedNamespace(unsanitizedInput, targetFramework),
            IsEnabledLibraryImportAttribute = IsEnabledLibraryImport(unsanitizedInput, targetFramework),
            IsEnabledSpans = IsEnabledSpans(targetFramework),
            IsEnabledRefStructs = IsEnabledRefStructs(unsanitizedInput, targetFramework)
        };
    }

    private string InputFilePath(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ToolInputSanitizationException(
                "The FFI input file path can not be an empty or a null string.");
        }

        var fullFilePath = fileSystem.Path.GetFullPath(filePath);

        if (!fileSystem.File.Exists(fullFilePath))
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

        var fullDirectoryPath = fileSystem.Path.GetFullPath(outputFileDirectory);

        if (!string.IsNullOrEmpty(fullDirectoryPath))
        {
            _ = fileSystem.Directory.CreateDirectory(fullDirectoryPath);
        }

        return fullDirectoryPath;
    }

    private static string LibraryName(string? libraryName, string className)
    {
        return !string.IsNullOrEmpty(libraryName) ? libraryName : className;
    }

    private static string NamespaceName(string? @namespace)
    {
        if (string.IsNullOrEmpty(@namespace))
        {
            throw new ToolInputSanitizationException(
                "The namespace name can not be an empty or null string.");
        }

        return @namespace;
    }

    private static string ClassName(string? className)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ToolInputSanitizationException(
                "The class name can not be an empty or null string.");
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
        if (!string.IsNullOrEmpty(code))
        {
            code = $"\n{code}\n";
        }

        return code;
    }

    private static string FooterCodeRegion(string? footerCodeRegionFilePath)
    {
        if (string.IsNullOrEmpty(footerCodeRegionFilePath))
        {
            return string.Empty;
        }

        var code = File.ReadAllText(footerCodeRegionFilePath);
        if (!string.IsNullOrEmpty(code))
        {
            code = $"\n{code}\n";
        }

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

    private ImmutableDictionary<string, string> MappedNames(ImmutableArray<InputUnsanitizedMappedName>? mappedNames)
    {
        if (mappedNames == null)
        {
            return ImmutableDictionary<string, string>.Empty;
        }

        var dictionary = new Dictionary<string, string>();

        foreach (var mappedName in mappedNames)
        {
            if (string.IsNullOrEmpty(mappedName.Source) || string.IsNullOrEmpty(mappedName.Target))
            {
                continue;
            }

            dictionary.Add(mappedName.Source, mappedName.Target);
        }

        return dictionary.ToImmutableDictionary();
    }

    private static bool IsEnabledFunctionPointers(InputUnsanitized unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // Function pointers are supported only in C# 9+ and .NET 5+
        var isAtLeastNetCoreV5 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 5 };
        if (!isAtLeastNetCoreV5)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledFunctionPointers ?? true;
    }

    private bool IsEnabledRuntimeMarshalling(InputUnsanitized unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // Disabling runtime marshalling is only supported in .NET 7+
        var isAtLeastNetCoreV7 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 7 };
        if (!isAtLeastNetCoreV7)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledRuntimeMarshalling ?? false;
    }

    private static bool IsEnabledFileScopedNamespace(InputUnsanitized unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // File scoped namespaces are only supported in .NET 6+
        var isAtLeastNetCoreV6 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 6 };
        if (!isAtLeastNetCoreV6)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledFileScopedNamespace ?? true;
    }

    private static bool IsEnabledLibraryImport(InputUnsanitized unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // LibraryImport is only supported in .NET 7+
        var isAtLeastNetCoreV7 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 7 };
        if (!isAtLeastNetCoreV7)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledLibraryImport ?? true;
    }

    private bool IsEnabledSpans(NuGetFramework nuGetFramework)
    {
        // Spans are only supported in .NET Core 2.1+
        var isAtLeastNetCore = nuGetFramework is { Framework: ".NETCoreApp" };
        if (!isAtLeastNetCore)
        {
            return false;
        }

        return nuGetFramework.Version.Major > 2 || nuGetFramework.Version is { Major: 2, Minor: >= 1 };
    }

    private bool IsEnabledRefStructs(InputUnsanitized unsanitizedInput, NuGetFramework nuGetFramework)
    {
        // Ref structs
        var isAtLeastNetCoreV7 = nuGetFramework is { Framework: ".NETCoreApp", Version.Major: >= 7 };
        if (!isAtLeastNetCoreV7)
        {
            return false;
        }

        return unsanitizedInput.IsEnabledRefStructs ?? true;
    }
}
