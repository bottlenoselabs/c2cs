// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Data.Serialization;
using C2CS.Feature.WriteCodeCSharp;
using C2CS.Tests.Common.Data.Model;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class WriteCodeCSharpFixture
{
    private readonly ImmutableDictionary<string, CSharpGeneratedFunction> _functionsByName;
    private readonly ImmutableDictionary<string, CSharpGeneratedEnum> _enumsByName;

    public WriteCodeCSharpFixture(
        WriteCodeCSharpUseCase useCase,
        IFileSystem fileSystem,
        ConfigurationJsonSerializer configurationJsonSerializer,
        ReadCodeCFixture ast)
    {
        Assert.True(!ast.AbstractSyntaxTrees.IsDefaultOrEmpty);

#pragma warning disable CA1308
        var os = Native.OperatingSystem.ToString().ToLowerInvariant();
#pragma warning restore CA1308
        var configurationFilePath = $"c/tests/my_c_library/config_{os}.json";
        var configuration = configurationJsonSerializer.Read(configurationFilePath);
        var configurationWriteCSharp = configuration.WriteCSharp;
        Assert.True(configurationWriteCSharp != null);

        var output = useCase.Execute(configurationWriteCSharp!);
        Assert.True(output != null);
        var input = output!.Input;

        Assert.True(output.IsSuccessful, "Writing C# code failed.");
        Assert.True(output.Diagnostics.Length == 0, "Diagnostics were reported when writing C# code.");

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

        var methodsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpGeneratedFunction>();
        var enumsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpGeneratedEnum>();

        foreach (var member in @class.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                {
                    var value = Function(method);
                    methodsByNameBuilder.Add(method.Identifier.Text, value);
                    break;
                }

                case EnumDeclarationSyntax @enum:
                {
                    var value = Enum(@enum);
                    enumsByNameBuilder.Add(@enum.Identifier.Text, value);
                    break;
                }
            }
        }

        _functionsByName = methodsByNameBuilder.ToImmutable();
        _enumsByName = enumsByNameBuilder.ToImmutable();
    }

    private CSharpGeneratedFunction Function(MethodDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var returnTypeName = syntaxNode.ReturnType.ToString();
        var parameters = FunctionParameters(syntaxNode);

        var result = new CSharpGeneratedFunction
        {
            Name = name,
            ReturnTypeName = returnTypeName,
            Parameters = parameters
        };
        return result;
    }

    private ImmutableArray<CSharpGeneratedFunctionParameter> FunctionParameters(MethodDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpGeneratedFunctionParameter>();

        foreach (var syntaxNodeChild in syntaxNode.ParameterList.Parameters)
        {
            var parameter = FunctionParameter(syntaxNodeChild);
            builder.Add(parameter);
        }

        return builder.ToImmutable();
    }

    private CSharpGeneratedFunctionParameter FunctionParameter(ParameterSyntax syntaxNode)
    {
        var typeName = syntaxNode.Type?.ToString() ?? string.Empty;

        var result = new CSharpGeneratedFunctionParameter
        {
            TypeName = typeName
        };
        return result;
    }

    private CSharpGeneratedEnum Enum(EnumDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var baseType = syntaxNode.BaseList!.Types[0].Type.ToString();
        var enumMembers = EnumMembers(syntaxNode);

        var result = new CSharpGeneratedEnum
        {
            Name = name,
            BaseType = baseType,
            Members = enumMembers
        };
        return result;
    }

    private ImmutableArray<CSharpGeneratedEnumMember> EnumMembers(EnumDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpGeneratedEnumMember>();
        foreach (var syntaxNodeChild in syntaxNode.Members)
        {
            var enumMember = EnumMember(syntaxNodeChild);
            builder.Add(enumMember);
        }

        return builder.ToImmutable();
    }

    private CSharpGeneratedEnumMember EnumMember(EnumMemberDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.ValueText;
        var value = syntaxNode.EqualsValue!.Value.GetText().ToString().Trim();

        var result = new CSharpGeneratedEnumMember
        {
            Name = name,
            Value = value
        };
        return result;
    }

    public CSharpGeneratedEnum GetEnum(string name)
    {
        var exists = _enumsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");
        return value!;
    }

    public CSharpGeneratedFunction GetFunction(string name)
    {
        var exists = _functionsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }
}
