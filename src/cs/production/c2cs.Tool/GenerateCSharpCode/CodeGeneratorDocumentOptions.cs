// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Globalization;
using System.Reflection;

namespace C2CS.GenerateCSharpCode;

public class CodeGeneratorDocumentOptions
{
    public string ClassName { get; }

    public string CodeRegionHeader { get; }

    public string CodeRegionFooter { get; }

    public bool IsEnabledFileScopedNamespace { get; }

    public bool AreTypeAccessModifiersPublic { get; }

    public bool IsEnabledRuntimeMarshalling { get; }

    public bool IsEnabledNullables { get; }

    public string LibraryName { get; }

    public string NamespaceName { get; }

    public readonly string DateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz", CultureInfo.InvariantCulture);
    public readonly string VersionStamp = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

    public CodeGeneratorDocumentOptions(InputSanitized input)
    {
        ClassName = input.ClassName;
        CodeRegionHeader = input.CodeRegionHeader;
        CodeRegionFooter = input.CodeRegionFooter;
        IsEnabledFileScopedNamespace = input.IsEnabledFileScopedNamespace;
        IsEnabledRuntimeMarshalling = input.IsEnabledRuntimeMarshalling;
        LibraryName = input.LibraryName;
        NamespaceName = input.NamespaceName;
        AreTypeAccessModifiersPublic = input.AreTypeAccessModifiersPublic;

        IsEnabledNullables = input.TargetFramework is { Framework: ".NETCoreApp", Version.Major: >= 3 };
    }
}
