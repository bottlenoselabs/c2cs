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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public sealed partial class CSharpCodeGenerator
{
    private readonly IServiceProvider _services;
    private readonly CSharpCodeGeneratorOptions _options;

    public CSharpCodeGenerator(
        IServiceProvider services,
        CSharpCodeGeneratorOptions options)
    {
        _services = services;
        _options = options;
    }

    public string EmitCode(CSharpAbstractSyntaxTree abstractSyntaxTree)
    {
        var handlers = CreateHandlers(_services);
        var context = new CSharpCodeGeneratorContext(handlers, _options, abstractSyntaxTree.Functions);

        var builder = new Dictionary<string, List<MemberDeclarationSyntax>>();
        AddSyntaxNodes(context, abstractSyntaxTree, builder);

        var members = builder.ToImmutableSortedDictionary();
        var compilationUnit = CompilationUnit(_options, members);

        var code = compilationUnit.ToFullString().Trim();
        return code;
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

    private CompilationUnitSyntax CompilationUnit(
        CSharpCodeGeneratorOptions options,
        ImmutableSortedDictionary<string, List<MemberDeclarationSyntax>> membersByClassName)
    {
        var code = CompilationUnitTemplateCode(options);
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

        if (_options.IsEnabledGenerateCSharpRuntimeCode)
        {
            var runtimeClassDeclaration = RuntimeClass();
            rootClassDeclarationWithMembers = rootClassDeclarationWithMembers.AddMembers(runtimeClassDeclaration);
        }

        var newCompilationUnit = compilationUnit.ReplaceNode(
            rootClassDeclarationOriginal,
            rootClassDeclarationWithMembers);
        using var workspace = new AdhocWorkspace();
        var newCompilationUnitFormatted = (CompilationUnitSyntax)Formatter.Format(newCompilationUnit, workspace);
        return newCompilationUnitFormatted;
    }

    private static ClassDeclarationSyntax RuntimeClass()
    {
        var builderMembers = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        var assembly = typeof(CBool).Assembly;
        var manifestResourcesNames = assembly.GetManifestResourceNames();
        foreach (var resourceName in manifestResourcesNames)
        {
            if (!resourceName.EndsWith(".cs", StringComparison.InvariantCulture))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var streamReader = new StreamReader(stream!);
            var code = streamReader.ReadToEnd();
            var syntaxTree = ParseSyntaxTree(code);
            var compilationUnit = (CompilationUnitSyntax)syntaxTree.GetRoot();
            foreach (var member in compilationUnit.Members)
            {
                builderMembers.Add(member);
            }
        }

        var members = builderMembers.ToArray();

        const string runtimeClassName = "Runtime";
        var runtimeClass = ClassDeclaration(runtimeClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(members)
            .AddRegionStart(runtimeClassName, true)
            .AddRegionEnd();
        return runtimeClass;
    }

    private static string CompilationUnitTemplateCode(CSharpCodeGeneratorOptions options)
    {
        var assemblyAttributesCode = string.Empty;
        if (options.IsEnabledAssemblyAttributes)
        {
            assemblyAttributesCode = @"
#if NET7_0_OR_GREATER
[assembly: DisableRuntimeMarshalling]
#endif
";

            assemblyAttributesCode += @"
[assembly: DefaultDllImportSearchPathsAttribute(DllImportSearchPath.SafeDirectories)]
";
        }

        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz", CultureInfo.InvariantCulture);
        var version = Assembly.GetEntryAssembly()!.GetName().Version;
        var code = $@"
// <auto-generated>
//  This code was generated by the following tool on {dateTime}:
//      https://github.com/bottlenoselabs/c2cs (v{version})
//
//  Changes to this file may cause incorrect behavior and will be lost if the code is
//      regenerated. To extend or add functionality use a partial class in a new file.
// </auto-generated>
// ReSharper disable All

#nullable enable
#pragma warning disable CS1591
#pragma warning disable CS8981
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
{(options.IsEnabledGenerateCSharpRuntimeCode ? $"using static {options.NamespaceName}.{options.ClassName}.Runtime;" : "using static bottlenoselabs.C2CS.Runtime")}

{options.HeaderCodeRegion}
{assemblyAttributesCode}

namespace {options.NamespaceName};

public static unsafe partial class {options.ClassName}
{{
    private const string LibraryName = ""{options.LibraryName}"";
}}

{options.FooterCodeRegion}
";

        var replacedCode = MultipleNewLinesRegex().Replace(code, "\n\n");
        return replacedCode;
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

    [GeneratedRegex("(\\n){2,}")]
    private static partial Regex MultipleNewLinesRegex();
}
