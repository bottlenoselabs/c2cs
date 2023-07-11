// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Handlers;
using C2CS.Foundation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

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

    public CSharpProject? Generate(CSharpAbstractSyntaxTree abstractSyntaxTree, DiagnosticCollection diagnostics)
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
                var runtimeCodeDocument = EmitRuntimeCodeDocument();
                documentsBuilder.Add(runtimeCodeDocument);
            }

            if (_options.IsEnabledGenerateAssemblyAttributes)
            {
                var assemblyAttributesCodeDocument = EmitAssemblyAttributesCodeDocument();
                documentsBuilder.Add(assemblyAttributesCodeDocument);
            }

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
        var code = @"// To disable generating this file set `isEnabledGenerateAssemblyAttributes` to `false` in the config file for generating C# code."
                           + CodeDocumentTemplate();

        code += @"
#if NET7_0_OR_GREATER
[assembly: DisableRuntimeMarshalling]
#endif
";

        code += @"
[assembly: DefaultDllImportSearchPathsAttribute(DllImportSearchPath.SafeDirectories)]
";

        var document = new CSharpProjectDocument
        {
            FileName = "AssemblyAttributes.gen.cs",
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
            FileName = $"{_options.ClassName}.gen.cs",
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
        code += @$"
{options.HeaderCodeRegion}

namespace {options.NamespaceName};

public static unsafe partial class {options.ClassName}
{{
    private const string LibraryName = ""{options.LibraryName}"";
}}

{options.FooterCodeRegion}
";

        var syntaxTree = ParseSyntaxTree(code);
        var compilationUnit = syntaxTree.GetCompilationUnitRoot();
        var rootNamespace = (FileScopedNamespaceDeclarationSyntax)compilationUnit.Members[0];
        var rootClassDeclarationOriginal = (ClassDeclarationSyntax)rootNamespace.Members[0];
        var rootClassDeclarationWithMembers = rootClassDeclarationOriginal;

        foreach (var (className, classMembers) in membersByClassName)
        {
            if (string.IsNullOrEmpty(className))
            {
                rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.AddMembers(classMembers.ToArray());
            }
            else
            {
                var classCode = @$"
public static unsafe partial class {className}
{{
}}
";
                var classDeclaration = (ClassDeclarationSyntax)ParseMemberDeclaration(classCode)!;
                var classDeclarationWithMembers = classDeclaration.AddMembers(classMembers.ToArray());
                rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.AddMembers(classDeclarationWithMembers);
            }
        }

        var newCompilationUnit = compilationUnit.ReplaceNode(
            rootClassDeclarationOriginal,
            rootClassDeclarationWithMembers);
        var formattedCode = newCompilationUnit.Format();
        return formattedCode;
    }

    private CSharpProjectDocument EmitRuntimeCodeDocument()
    {
        var templateCode = @"// To disable generating this file set `isEnabledGeneratingRuntimeCode` to `false` in the config file for generating C# code."
                           + CodeDocumentTemplate();
        templateCode += @"

namespace bottlenoselabs.C2CS;

public static unsafe partial class Runtime
{
}
";

        var compilationUnitCode = ParseSyntaxTree(templateCode).GetCompilationUnitRoot();
        var rootNamespace = (FileScopedNamespaceDeclarationSyntax)compilationUnitCode.Members[0];
        var rootClassDeclarationOriginal = (ClassDeclarationSyntax)rootNamespace.Members[0];
        var rootClassDeclarationWithMembers = rootClassDeclarationOriginal;

        var members = typeof(bottlenoselabs.C2CS.Runtime.CBool).Assembly.GetManifestResourceMemberDeclarations();
        rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.WithMembers(rootClassDeclarationWithMembers.Members.AddRange(members));

        var newCompilationUnit = compilationUnitCode.ReplaceNode(
            rootClassDeclarationOriginal,
            rootClassDeclarationWithMembers);
        var code = newCompilationUnit.Format();

        var document = new CSharpProjectDocument
        {
            FileName = "Runtime.gen.cs",
            Contents = code
        };

        return document;
    }

    private string CodeDocumentTemplate()
    {
        var code = $@"
// <auto-generated>
//  This code was generated by the following tool on {_dateTimeStamp}:
//      https://github.com/bottlenoselabs/c2cs (v{_versionStamp})
//
//  Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
// ReSharper disable All

#region Template
#nullable enable
#pragma warning disable CS1591
#pragma warning disable CS8981
global using bottlenoselabs.C2CS.Runtime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
#endregion
";
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
