// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using C2CS.Contexts.WriteCodeCSharp;
using C2CS.Data.Serialization;
using C2CS.Tests.Common;
using C2CS.Tests.Common.Data.Model.CSharp;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

[PublicAPI]
public sealed class WriteCodeCSharpFixture : TestFixture
{
    public WriteCodeCSharpFixtureContext Context { get; }

    public WriteCodeCSharpFixture(
        WriteCodeCSharpUseCase useCase,
        IFileSystem fileSystem,
        BindgenConfigurationJsonSerializer configurationJsonSerializer,
        ReadCodeCFixture ast)
    {
        Assert.True(!ast.Contexts.IsDefaultOrEmpty);

#pragma warning disable CA1308
        var os = Native.OperatingSystem.ToString().ToLowerInvariant();
#pragma warning restore CA1308
        var configurationFilePath = $"c/tests/my_c_library/config_{os}.json";
        var configuration = configurationJsonSerializer.Read(configurationFilePath);
        var configurationWriteCSharp = configuration.WriteCSharpCode;
        Assert.True(configurationWriteCSharp != null);

        var output = useCase.Execute(configurationWriteCSharp!);
        Assert.True(output != null);
        var input = output!.Input;

        Assert.True(output.IsSuccess, "Writing C# code failed.");
        Assert.True(output.Diagnostics.Length == 0, "Diagnostics were reported when writing C# code.");

        var code = fileSystem.File.ReadAllText(input.OutputFilePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var diagnostics = syntaxTree.GetDiagnostics().ToImmutableArray();
        var errors = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        Assert.True(errors.Length == 0, "The code has diagnostic errors.");
        var warnings = diagnostics.Where(x => x.Severity == DiagnosticSeverity.Warning).ToImmutableArray();
        Assert.True(warnings.Length == 0, "The code has diagnostic warnings.");
        var otherDiagnostics = diagnostics.Where(x =>
            x.Severity != DiagnosticSeverity.Error && x.Severity != DiagnosticSeverity.Warning).ToImmutableArray();
        Assert.True(otherDiagnostics.Length == 0, "The code has diagnostics which are not errors or warnings.");

        var compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();

        Assert.True(compilationUnitSyntax.Members.Count == 1);
        var @namespace = compilationUnitSyntax.Members[0] as NamespaceDeclarationSyntax;
        Assert.True(@namespace != null);
        Assert.True(@namespace!.Name.ToString() == input.GeneratorOptions.NamespaceName);

        Assert.True(@namespace.Members.Count == 1);
        var @class = @namespace.Members[0] as ClassDeclarationSyntax;
        Assert.True(@class != null);
        Assert.True(@class!.Identifier.ToString() == input.GeneratorOptions.ClassName);

        var methodsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestFunction>();
        var enumsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestEnum>();
        var structsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestStruct>();
        var macroObjectsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestMacroObject>();

        foreach (var member in @class.Members)
        {
            switch (member)
            {
                case MethodDeclarationSyntax syntaxNode:
                {
                    var value = CreateTestFunction(syntaxNode);
                    methodsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                    break;
                }

                case EnumDeclarationSyntax syntaxNode:
                {
                    var value = CreateTestEnum(syntaxNode);
                    enumsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                    break;
                }

                case StructDeclarationSyntax syntaxNode:
                {
                    var value = CreateTestStruct(syntaxNode);
                    structsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                    break;
                }

                case FieldDeclarationSyntax syntaxNode:
                {
                    var fieldName = syntaxNode.Declaration.Variables[0].Identifier.Text;
                    if (fieldName == "LibraryName")
                    {
                        continue;
                    }

                    var value = CreateTestMacroObject(syntaxNode, fieldName);
                    macroObjectsByNameBuilder.Add(fieldName, value);
                    break;
                }
            }
        }

        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithPlatform(Platform.AnyCpu)
                .WithAllowUnsafe(true);
        var compilation = CSharpCompilation.Create(
            "TestAssemblyName",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            compilationOptions);
        using var dllStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(dllStream, pdbStream);

        Context = new WriteCodeCSharpFixtureContext(
            emitResult,
            methodsByNameBuilder.ToImmutable(),
            enumsByNameBuilder.ToImmutable(),
            structsByNameBuilder.ToImmutable(),
            macroObjectsByNameBuilder.ToImmutable());
    }

    private CSharpTestFunction CreateTestFunction(MethodDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var returnTypeName = syntaxNode.ReturnType.ToString();
        var parameters = CreateTestFunctionParameters(syntaxNode);

        var result = new CSharpTestFunction
        {
            Name = name,
            ReturnTypeName = returnTypeName,
            Parameters = parameters
        };
        return result;
    }

    private ImmutableArray<CSharpTestFunctionParameter> CreateTestFunctionParameters(MethodDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestFunctionParameter>();

        foreach (var syntaxNodeParameter in syntaxNode.ParameterList.Parameters)
        {
            var parameter = CreateTestFunctionParameter(syntaxNodeParameter);
            builder.Add(parameter);
        }

        return builder.ToImmutable();
    }

    private CSharpTestFunctionParameter CreateTestFunctionParameter(ParameterSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.ValueText;
        var typeName = syntaxNode.Type?.ToString() ?? string.Empty;

        var result = new CSharpTestFunctionParameter
        {
            Name = name,
            TypeName = typeName
        };
        return result;
    }

    private CSharpTestEnum CreateTestEnum(EnumDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var baseType = syntaxNode.BaseList!.Types[0].Type.ToString();
        var enumMembers = CreateTestEnumMembers(syntaxNode);

        var result = new CSharpTestEnum
        {
            Name = name,
            BaseType = baseType,
            Members = enumMembers
        };
        return result;
    }

    private ImmutableArray<CSharpTestEnumMember> CreateTestEnumMembers(EnumDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestEnumMember>();
        foreach (var syntaxNodeEnumMember in syntaxNode.Members)
        {
            var enumMember = CreateTestEnumMember(syntaxNodeEnumMember);
            builder.Add(enumMember);
        }

        return builder.ToImmutable();
    }

    private CSharpTestEnumMember CreateTestEnumMember(EnumMemberDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.ValueText;
        var value = syntaxNode.EqualsValue!.Value.GetText().ToString().Trim();

        var result = new CSharpTestEnumMember
        {
            Name = name,
            Value = value
        };
        return result;
    }

    private CSharpTestStruct CreateTestStruct(StructDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var fields = CreateTestStructFields(syntaxNode);
        var layout = CreateTestStructLayout(syntaxNode);

        var result = new CSharpTestStruct
        {
            Name = name,
            Layout = layout,
            Fields = fields
        };
        return result;
    }

    private ImmutableArray<CSharpTestStructField> CreateTestStructFields(StructDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestStructField>();

        foreach (var syntaxNodeMember in syntaxNode.Members)
        {
            if (syntaxNodeMember is not FieldDeclarationSyntax syntaxNodeField)
            {
                continue;
            }

            var field = CreateTestStructField(syntaxNodeField);
            builder.Add(field);
        }

        return builder.ToImmutable();
    }

    private CSharpTestStructField CreateTestStructField(FieldDeclarationSyntax syntaxNode)
    {
        var variableSyntaxNode = syntaxNode.Declaration;
        var name = variableSyntaxNode.Variables[0].Identifier.Text;
        var typeName = variableSyntaxNode.Type.ToString();
        var offsetOf = FieldOffsetOf(name, syntaxNode);

        var result = new CSharpTestStructField
        {
            Name = name,
            TypeName = typeName,
            OffsetOf = offsetOf!.Value
        };

        return result;
    }

    private CSharpTestStructLayout CreateTestStructLayout(StructDeclarationSyntax syntaxNode)
    {
        var attribute = GetAttribute("StructLayout", syntaxNode);
        var arguments = attribute.ArgumentList!.Arguments;

        var layoutKind = arguments[0].Expression.ToFullString();
        var sizeOfString = arguments[1].Expression.ToFullString();
        var sizeOf = int.Parse(sizeOfString, CultureInfo.InvariantCulture);
        var packOfString = arguments[2].Expression.ToFullString();
        var packOf = int.Parse(packOfString, CultureInfo.InvariantCulture);

        var result = new CSharpTestStructLayout
        {
            LayoutKind = layoutKind,
            Size = sizeOf,
            Pack = packOf
        };
        return result;
    }

    private CSharpTestMacroObject CreateTestMacroObject(FieldDeclarationSyntax syntaxNode, string fieldName)
    {
        var typeName = syntaxNode.Declaration.Type.ToString();
        var value = syntaxNode.Declaration.Variables[0].Initializer!.Value.ToString();

        var result = new CSharpTestMacroObject
        {
            Name = fieldName,
            TypeName = typeName,
            Value = value
        };
        return result;
    }

    private int? FieldOffsetOf(string name, FieldDeclarationSyntax syntaxNode)
    {
        int? offsetOf = null;

        var attribute = GetAttribute("FieldOffset", syntaxNode);
        var expression = attribute.ArgumentList!.Arguments[0].Expression;
        if (expression is LiteralExpressionSyntax literalExpression)
        {
            offsetOf = int.Parse(literalExpression.Token.ValueText, CultureInfo.InvariantCulture);
        }

        Assert.True(offsetOf != null, $"The field `{name}` does not have an offset.");

        return offsetOf;
    }

    private AttributeSyntax GetAttribute(string name, MemberDeclarationSyntax syntaxNode)
    {
        AttributeSyntax? result = null;

        foreach (var attributeList in syntaxNode.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == name)
                {
                    result = attribute;
                }
            }
        }

        Assert.True(result != null, $"The attribute `{name}` does not exist.");

        return result!;
    }
}
