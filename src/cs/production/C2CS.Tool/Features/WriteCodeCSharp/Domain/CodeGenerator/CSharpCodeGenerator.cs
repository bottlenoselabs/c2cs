// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Handlers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Formatter = Microsoft.CodeAnalysis.Formatting.Formatter;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class CSharpCodeGenerator
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
        var context = new CSharpCodeGeneratorContext(handlers, _options);

        var builder = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();
        AddSyntaxNodes(context, abstractSyntaxTree, builder);

        var members = builder.ToImmutable();
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
        ImmutableArray<MemberDeclarationSyntax>.Builder builder)
    {
        var membersApi = new List<MemberDeclarationSyntax>();
        var membersTypes = new List<MemberDeclarationSyntax>();

        AddApi(abstractSyntaxTree, membersApi, context);
        AddTypes(abstractSyntaxTree, membersTypes, context);

        builder.AddRange(membersApi);
        builder.AddRange(membersTypes);
    }

    private void AddApi(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        List<MemberDeclarationSyntax> members,
        CSharpCodeGeneratorContext context)
    {
        Add(abstractSyntaxTree.Functions, members, context);

        if (members.Count <= 0)
        {
            return;
        }

        members[0] = members[0].AddRegionStart("API", false);
        members[^1] = members[^1].AddRegionEnd();
    }

    private void AddTypes(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        List<MemberDeclarationSyntax> members,
        CSharpCodeGeneratorContext context)
    {
        Add(abstractSyntaxTree.FunctionPointers, members, context);
        Add(abstractSyntaxTree.Structs, members, context);
        Add(abstractSyntaxTree.Enums, members, context);
        Add(abstractSyntaxTree.OpaqueStructs, members, context);
        Add(abstractSyntaxTree.AliasStructs, members, context);
        Add(abstractSyntaxTree.MacroObjects, members, context);
        Add(abstractSyntaxTree.Constants, members, context);

        if (members.Count <= 0)
        {
            return;
        }

        members[0] = members[0].AddRegionStart("Types", false);
        members[^1] = members[^1].AddRegionEnd();
    }

    private CompilationUnitSyntax CompilationUnit(
        CSharpCodeGeneratorOptions options, ImmutableArray<MemberDeclarationSyntax> members)
    {
        var code = CompilationUnitTemplateCode(options);
        var syntaxTree = ParseSyntaxTree(code);
        var compilationUnit = syntaxTree.GetCompilationUnitRoot();
        var namespaceDeclaration = (NamespaceDeclarationSyntax)compilationUnit.Members[0];
        var classDeclaration = (ClassDeclarationSyntax)namespaceDeclaration.Members[0];
        var classDeclarationWithMembers = classDeclaration.AddMembers(members.ToArray());

        if (_options.IsEnabledGenerateCSharpRuntimeCode)
        {
            var runtimeClassDeclaration = RuntimeClass();
            classDeclarationWithMembers = classDeclarationWithMembers.AddMembers(runtimeClassDeclaration);
        }

        var newCompilationUnit = compilationUnit.ReplaceNode(classDeclaration, classDeclarationWithMembers);
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
using static {options.NamespaceName}.{options.ClassName}.Runtime;

{options.HeaderCodeRegion}

[assembly: DefaultDllImportSearchPathsAttribute(DllImportSearchPath.SafeDirectories)]

#if NET7_0_OR_GREATER
[assembly: DisableRuntimeMarshalling]
#endif

namespace {options.NamespaceName}
{{
    public static unsafe partial class {options.ClassName}
    {{
        private const string LibraryName = ""{options.LibraryName}"";
    }}
}}
{options.FooterCodeRegion}
";
        return code;
    }

    private void Add<TCSharpNode>(
        ImmutableArray<TCSharpNode> nodes,
        List<MemberDeclarationSyntax> members,
        CSharpCodeGeneratorContext context)
        where TCSharpNode : CSharpNode
    {
        if (nodes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var node in nodes)
        {
            var member = context.GenerateCodeMemberSyntax(node);
            members.Add(member);
        }
    }
}
