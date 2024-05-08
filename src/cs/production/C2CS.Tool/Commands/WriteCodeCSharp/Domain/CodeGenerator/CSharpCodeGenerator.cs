// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using bottlenoselabs.Common.Diagnostics;
using C2CS.Commands.WriteCodeCSharp.Data;
using C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class CSharpCodeGenerator
{
    private readonly IServiceProvider _services;
    private readonly CSharpCodeGeneratorOptions _options;

    private readonly string _dateTimeStamp;
    private readonly string _versionStamp;

    public CSharpCodeGenerator(
        IServiceProvider services,
        CSharpCodeGeneratorOptions options)
    {
        _services = services;
        _options = options;

        _dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz", CultureInfo.InvariantCulture);
        _versionStamp = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();
    }

    public CSharpProject? Generate(CSharpAbstractSyntaxTree abstractSyntaxTree, DiagnosticsSink diagnostics)
    {
        try
        {
            var generateCodeHandlers = CreateHandlers(_services);
            var context = new CSharpCodeGeneratorContext(generateCodeHandlers, _options, abstractSyntaxTree.Functions);
            var documentsBuilder = ImmutableArray.CreateBuilder<CSharpProjectDocument>();

            var codeDocument = EmitCodeDocument(abstractSyntaxTree, context);
            documentsBuilder.Add(codeDocument);

            if (_options.IsEnabledGenerateCSharpRuntimeCode)
            {
                var runtimeCodeDocument = EmitRuntimeCodeDocument(_options);
                documentsBuilder.Add(runtimeCodeDocument);
            }

            var assemblyAttributesCodeDocument = EmitAssemblyAttributesCodeDocument();
            documentsBuilder.Add(assemblyAttributesCodeDocument);

            var project = new CSharpProject
            {
                Documents = documentsBuilder.ToImmutable()
            };

            return project;
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            diagnostics.Add(new DiagnosticPanic(e));
            return null;
        }
    }

    private CSharpProjectDocument EmitAssemblyAttributesCodeDocument()
    {
        var code = CodeDocumentTemplate();

        if (!_options.IsEnabledRuntimeMarshalling)
        {
            code += """

                    #if NET7_0_OR_GREATER
                    // NOTE: Disabling runtime marshalling is preferred for performance improvements. You can learn more here: https://learn.microsoft.com/en-us/dotnet/standard/native-interop/disabled-marshalling
                    [assembly: DisableRuntimeMarshalling]
                    #endif

                    """;
        }

        code += """

                #if (NETCOREAPP1_0_OR_GREATER) || (NET45_OR_GREATER || NETFRAMEWORK && (NET45 || NET451 || NET452 || NET46 || NET461 || NET462 || NET47 || NET471 || NET472 || NET48)) || (NETSTANDARD1_1_OR_GREATER || NETSTANDARD && !NETSTANDARD1_0)
                // NOTE: Only takes effect on Windows. Specifies the recommended maximum number of directories (the application directory, the %WinDir%\System32 directory, and user directories in the DLL search path) to search for native libraries. You can learn more here at (1) https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.defaultdllimportsearchpathsattribute and (2) https://learn.microsoft.com/en-ca/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibraryexa#parameters
                [assembly: DefaultDllImportSearchPathsAttribute(DllImportSearchPath.SafeDirectories)]
                #endif

                """;

        var document = new CSharpProjectDocument
        {
            FileName = "AssemblyAttributes.g.cs",
            Contents = code
        };

        return document;
    }

    private CSharpProjectDocument EmitCodeDocument(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        CSharpCodeGeneratorContext context)
    {
        var membersBuilder = new Dictionary<string, List<MemberDeclarationSyntax>>();
        AddSyntaxNodes(context, abstractSyntaxTree, membersBuilder);

        var members = membersBuilder.ToImmutableSortedDictionary();
        var code = CompilationUnitCode(_options, members);

        var codeDocument = new CSharpProjectDocument
        {
            FileName = $"{_options.ClassName}.g.cs",
            Contents = code
        };

        return codeDocument;
    }

    private ImmutableDictionary<Type, GenerateCodeHandler> CreateHandlers(IServiceProvider services)
    {
        var result = new Dictionary<Type, GenerateCodeHandler>
        {
            { typeof(CSharpFunctionPointer), services.GetService<FunctionPointerCodeGenerator>()! },
            { typeof(CSharpFunction), services.GetService<FunctionCodeGenerator>()! },
            { typeof(CSharpStruct), services.GetService<StructCodeGenerator>()! },
            { typeof(CSharpOpaqueType), services.GetService<OpaqueTypeCodeGenerator>()! },
            { typeof(CSharpAliasType), services.GetService<AliasTypeCodeGenerator>()! },
            { typeof(CSharpEnum), services.GetService<EnumCodeGenerator>()! },
            { typeof(CSharpMacroObject), services.GetService<MacroCodeGenerator>()! },
            { typeof(CSharpConstant), services.GetService<ConstantCodeGenerator>()! }
        };

        return result.ToImmutableDictionary();
    }

    private void AddSyntaxNodes(
        CSharpCodeGeneratorContext context,
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        Dictionary<string, List<MemberDeclarationSyntax>> builder)
    {
        var builderMembersApiByClassName = new Dictionary<string, List<MemberDeclarationSyntax>>();
        var builderMembersTypesByClassName = new Dictionary<string, List<MemberDeclarationSyntax>>();

        AddApi(abstractSyntaxTree, builderMembersApiByClassName, context);
        AddTypes(abstractSyntaxTree, builderMembersTypesByClassName, context);

        foreach (var (className, membersApi) in builderMembersApiByClassName)
        {
            if (!builder.TryGetValue(className, out var members))
            {
                members = new List<MemberDeclarationSyntax>();
                builder.Add(className, members);
            }

            members.AddRange(membersApi);
        }

        foreach (var (className, membersTypes) in builderMembersTypesByClassName)
        {
            if (!builder.TryGetValue(className, out var members))
            {
                members = new List<MemberDeclarationSyntax>();
                builder.Add(className, members);
            }

            members.AddRange(membersTypes);
        }
    }

    private void AddApi(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        Dictionary<string, List<MemberDeclarationSyntax>> membersByClassName,
        CSharpCodeGeneratorContext context)
    {
        Add(abstractSyntaxTree.Functions, membersByClassName, context);

        foreach (var (_, members) in membersByClassName)
        {
            members[0] = members[0].AddRegionStart("API", false);
            members[^1] = members[^1].AddRegionEnd();
        }
    }

    private void AddTypes(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        Dictionary<string, List<MemberDeclarationSyntax>> membersByClassName,
        CSharpCodeGeneratorContext context)
    {
        Add(abstractSyntaxTree.FunctionPointers, membersByClassName, context);
        Add(abstractSyntaxTree.Structs, membersByClassName, context);
        Add(abstractSyntaxTree.Enums, membersByClassName, context);
        Add(abstractSyntaxTree.OpaqueStructs, membersByClassName, context);
        Add(abstractSyntaxTree.AliasStructs, membersByClassName, context);
        Add(abstractSyntaxTree.MacroObjects, membersByClassName, context);
        Add(abstractSyntaxTree.Constants, membersByClassName, context);

        foreach (var (_, members) in membersByClassName)
        {
            members[0] = members[0].AddRegionStart("Types", false);
            members[^1] = members[^1].AddRegionEnd();
        }
    }

    private string CompilationUnitCode(
        CSharpCodeGeneratorOptions options,
        ImmutableSortedDictionary<string, List<MemberDeclarationSyntax>> membersByClassName)
    {
        var code = CodeDocumentTemplate();

        if (!string.IsNullOrEmpty(options.HeaderCodeRegion))
        {
            code += $"""

                     {options.HeaderCodeRegion}

                     """;
        }

        if (!string.IsNullOrEmpty(options.NamespaceName))
        {
            if (options.IsEnabledFileScopedNamespace)
            {
                code += $"""

                         namespace {options.NamespaceName};

                         """;
            }
            else
            {
                code += $$"""

                          namespace {{options.NamespaceName}} {

                          """;
            }
        }

        code += $$"""

                  public static unsafe partial class {{options.ClassName}}
                  {
                      private const string LibraryName = "{{options.LibraryName}}";
                  }

                  """;

        if (!string.IsNullOrEmpty(options.FooterCodeRegion))
        {
            code += $"""

                     {options.FooterCodeRegion}

                     """;
        }

        if (!string.IsNullOrEmpty(options.NamespaceName) && !options.IsEnabledFileScopedNamespace)
        {
            code += """

                    }

                    """;
        }

        var syntaxTree = ParseSyntaxTree(code);
        var compilationUnit = syntaxTree.GetCompilationUnitRoot();

        var rootClassDeclaration = compilationUnit.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (rootClassDeclaration == null)
        {
            throw new InvalidOperationException("Unable to find class declaration.");
        }

        var rootClassDeclarationWithMembers = rootClassDeclaration;

        foreach (var (className, classMembers) in membersByClassName)
        {
            if (string.IsNullOrEmpty(className))
            {
                rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.AddMembers(classMembers.ToArray());
            }
            else
            {
                var classCode = $$"""

                                  public static unsafe partial class {{className}}
                                  {
                                  }

                                  """;
                var classDeclaration = (ClassDeclarationSyntax)ParseMemberDeclaration(classCode)!;
                var classDeclarationWithMembers = classDeclaration.AddMembers(classMembers.ToArray());
                rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.AddMembers(classDeclarationWithMembers);
            }
        }

        var newCompilationUnit = compilationUnit.ReplaceNode(
            rootClassDeclaration,
            rootClassDeclarationWithMembers);
        var formattedCode = newCompilationUnit.GetCode();
        return formattedCode;
    }

    private CSharpProjectDocument EmitRuntimeCodeDocument(CSharpCodeGeneratorOptions options)
    {
        var templateCode = """

                           // To disable generating this file set `isEnabledGeneratingRuntimeCode` to `false` in the config file for generating C# code.

                           """
                           + CodeDocumentTemplate(isEnabledNullables: false);

        if (options.IsEnabledFileScopedNamespace)
        {
            templateCode += """

                            namespace Bindgen.Runtime;

                            """;
        }
        else
        {
            templateCode += """

                            namespace Bindgen.Runtime
                            {
                            }

                            """;
        }

        var compilationUnitCode = ParseSyntaxTree(templateCode).GetCompilationUnitRoot();
        var rootNamespaceOriginal = (BaseNamespaceDeclarationSyntax)compilationUnitCode.Members[0];
        var rootNamespaceWithMembers = rootNamespaceOriginal;

        var codeFileContents = GetRuntimeCodeFileContents();
        var members = GetRuntimeMemberDeclarations(codeFileContents);
        rootNamespaceWithMembers = rootNamespaceWithMembers.WithMembers(rootNamespaceWithMembers.Members.AddRange(members));

        var newCompilationUnit = compilationUnitCode.ReplaceNode(
            rootNamespaceOriginal,
            rootNamespaceWithMembers);
        var code = newCompilationUnit.GetCode();

        var document = new CSharpProjectDocument
        {
            FileName = "Bindgen.Runtime.g.cs",
            Contents = code
        };

        return document;
    }

    private ImmutableArray<string> GetRuntimeCodeFileContents()
    {
        var builderCodeFileContents = ImmutableArray.CreateBuilder<string>();

        var assembly = Assembly.GetExecutingAssembly();
        var manifestResourcesNames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
        foreach (var resourceName in manifestResourcesNames)
        {
            if (!resourceName.EndsWith(".cs", StringComparison.InvariantCulture))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var streamReader = new StreamReader(stream!);
            var code = streamReader.ReadToEnd()
                .Replace("namespace Bindgen.Runtime;", string.Empty, StringComparison.InvariantCulture);
            builderCodeFileContents.Add(code);
        }

        return builderCodeFileContents.ToImmutable();
    }

    private static ImmutableArray<MemberDeclarationSyntax> GetRuntimeMemberDeclarations(
        ImmutableArray<string> codeFileContents)
    {
        var builderMembers = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        foreach (var code in codeFileContents)
        {
            var syntaxTree = ParseSyntaxTree(code);
            if (syntaxTree.GetRoot() is not CompilationUnitSyntax compilationUnit)
            {
                continue;
            }

            foreach (var member in compilationUnit.Members)
            {
                builderMembers.Add(member);
            }
        }

        return builderMembers.ToImmutable();
    }

    private string CodeDocumentTemplate(bool isEnabledNullables = true)
    {
        var code = $"""

                    // <auto-generated>
                    //  This code was generated by the following tool on {_dateTimeStamp}:
                    //      https://github.com/bottlenoselabs/c2cs (v{_versionStamp})
                    //
                    //  Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
                    // </auto-generated>
                    // ReSharper disable All

                    #region Template

                    """;

        if (isEnabledNullables)
        {
            code += """
                    #nullable enable

                    """;
        }

        code += """
                #pragma warning disable CS1591
                #pragma warning disable CS8981
                using Bindgen.Runtime;
                using System;
                using System.Collections.Generic;
                using System.Globalization;
                using System.Runtime.InteropServices;
                using System.Runtime.CompilerServices;
                #endregion

                """;
        return code;
    }

    private static void Add<TCSharpNode>(
        ImmutableArray<TCSharpNode> nodes,
        Dictionary<string, List<MemberDeclarationSyntax>> members,
        CSharpCodeGeneratorContext context)
        where TCSharpNode : CSharpNode
    {
        if (nodes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var node in nodes)
        {
            if (!members.TryGetValue(node.ClassName, out var builder))
            {
                builder = new List<MemberDeclarationSyntax>();
                members.Add(node.ClassName, builder);
            }

            var member = context.GenerateCodeMemberSyntax(node);
            builder.Add(member);
        }
    }
}
