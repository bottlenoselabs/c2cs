// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Data.Serialization;
using C2CS.Feature.WriteCodeCSharp;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class BindgenCSharpFixture
{
    public readonly ImmutableDictionary<string, MethodDeclarationSyntax> FunctionsByName;
    public readonly ImmutableDictionary<string, EnumDeclarationSyntax> EnumsByName;

    public BindgenCSharpFixture(
        WriteCodeCSharpUseCase useCase,
        IFileSystem fileSystem,
        ConfigurationJsonSerializer configurationJsonSerializer,
        ExtractAbstractSyntaxTreeCFixture ast)
    {
        Assert.True(!ast.AbstractSyntaxTrees.IsDefaultOrEmpty);

        var configurationFilePath = Native.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "my_c_library/config_windows.json",
            NativeOperatingSystem.Linux => "my_c_library/config_linux.json",
            NativeOperatingSystem.macOS => "my_c_library/config_macos.json",
            _ => throw new InvalidOperationException()
        };
        var configuration = configurationJsonSerializer.Read(configurationFilePath);
        var request = configuration.WriteCSharp;
        Assert.True(request != null);

        var output = useCase.Execute(request!);
        Assert.True(output != null);
        var input = output!.Input;

        Assert.True(output.IsSuccessful);
        Assert.True(output.Diagnostics.Length == 0);

        var code = fileSystem.File.ReadAllText(input.OutputFilePath);
        var compilationUnitSyntax = CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot();

        Assert.True(compilationUnitSyntax.Members.Count == 1);
        var @namespace = compilationUnitSyntax.Members[0] as NamespaceDeclarationSyntax;
        Assert.True(@namespace != null);
        Assert.True(@namespace!.Name.ToString() == input.NamespaceName);

        Assert.True(@namespace.Members.Count == 1);
        var @class = @namespace.Members[0] as ClassDeclarationSyntax;
        Assert.True(@class != null);
        Assert.True(@class!.Identifier.ToString() == input.ClassName);

        var methodsByNameBuilder = ImmutableDictionary.CreateBuilder<string, MethodDeclarationSyntax>();
        var enumsByNameBuilder = ImmutableDictionary.CreateBuilder<string, EnumDeclarationSyntax>();

        foreach (var member in @class.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                    methodsByNameBuilder.Add(method.Identifier.Text, method);
                    break;
                case EnumDeclarationSyntax @enum:
                    enumsByNameBuilder.Add(@enum.Identifier.Text, @enum);
                    break;
            }
        }

        FunctionsByName = methodsByNameBuilder.ToImmutable();
        EnumsByName = enumsByNameBuilder.ToImmutable();
    }
}
