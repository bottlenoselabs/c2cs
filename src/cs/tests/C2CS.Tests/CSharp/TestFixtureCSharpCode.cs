// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using C2CS.Tests.C;
using C2CS.Tests.CSharp.Data.Models;
using C2CS.Tests.Foundation;
using C2CS.Tests.Foundation.CMake;
using C2CS.Tests.Foundation.Roslyn;
using C2CS.WriteCodeCSharp;
using C2CS.WriteCodeCSharp.Data.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests.CSharp;

public sealed class TestFixtureCSharpCode
{
    private readonly TestCSharpCodeAbstractSyntaxTree _abstractSyntaxTree;
    private readonly CCodeBuildResult _cCodeBuildResult;
    private readonly CSharpCodeBuildResult _cSharpCodeBuildResult;

    public TestFixtureCSharpCode()
    {
        var services = TestHost.Services;
        var readC = services.GetService<TestFixtureCCode>()!;
        Assert.True(readC.AbstractSyntaxTrees.Length != 0);

        var writerCSharpCode = services.GetService<IWriterCSharpCode>()!;

        var featureWriteCodeCSharp = services.GetService<FeatureWriteCodeCSharp>()!;
        var fileSystem = services.GetService<IFileSystem>()!;
        var cMakeLibraryBuilder = services.GetService<CMakeLibraryBuilder>()!;

        var outputWriteCSharp = featureWriteCodeCSharp.Execute(writerCSharpCode.Options!);
        Assert.True(outputWriteCSharp != null);
        var input = outputWriteCSharp!.Input;

        Assert.True(outputWriteCSharp.Diagnostics.Length == 0, "Diagnostics were reported when writing C# code.");
        Assert.True(outputWriteCSharp.IsSuccess, "Writing C# code failed.");

        var code = fileSystem.File.ReadAllText(input.OutputFilePath);

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var diagnostics = syntaxTree.GetDiagnostics().ToImmutableArray();
        var errors = diagnostics.Where(x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).ToImmutableArray();
        Assert.True(errors.Length == 0, "The code has diagnostic errors.");
        var warnings = diagnostics.Where(x => x.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).ToImmutableArray();
        Assert.True(warnings.Length == 0, "The code has diagnostic warnings.");
        var otherDiagnostics = diagnostics.Where(x =>
            x.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error && x.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).ToImmutableArray();
        Assert.True(otherDiagnostics.Length == 0, "The code has diagnostics which are not errors or warnings.");

        _abstractSyntaxTree = AbstractSyntaxTree(syntaxTree, input);
        _cSharpCodeBuildResult = CSharpBuildResult(syntaxTree);
        _cCodeBuildResult = CCodeBuildResult(cMakeLibraryBuilder);
    }

    public void AssertCompiles()
    {
        Assert.True(_cCodeBuildResult.IsSuccess, $"C library did not compile successfully for purposes of P/Invoke from C#.");

        var emitResult = _cSharpCodeBuildResult.EmitResult;

        foreach (var diagnostic in emitResult.Diagnostics)
        {
            var isWarningOrError = diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Warning &&
                                   diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error;
            Assert.True(isWarningOrError, $"C# code compilation diagnostic: {diagnostic}.");
        }

        Assert.True(emitResult.Success, "C# code did not compile successfully.");

        var setupMethod = _cSharpCodeBuildResult.ClassType.GetMethod("Setup");
        setupMethod!.Invoke(null, null);
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _abstractSyntaxTree.Enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");

        var enumType = _cSharpCodeBuildResult.ClassType.GetNestedType(name);
        Assert.True(enumType != null, $"The enum type `{name}` does not exist.");
        var enumValues = enumType!.GetEnumValues();

        AssertPInvokeEnum(name, enumValues);

        return value!;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _abstractSyntaxTree.Methods.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CSharpTestStruct GetStruct(string name)
    {
        var exists = _abstractSyntaxTree.Structs.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        return value!;
    }

    public CSharpTestMacroObject GetMacroObject(string name)
    {
        var exists = _abstractSyntaxTree.MacroObjects.TryGetValue(name, out var value);
        Assert.True(exists, $"The macro object `{name}` does not exist.");
        return value!;
    }

    private void AssertPInvokeEnum(string name, Array enumValues)
    {
        // TODO: Inter process communication

        var enumPrintMethodName = $"{name}__print_{name}";
        var enumPrintMethod = _cSharpCodeBuildResult.ClassType.GetMethod(enumPrintMethodName);
        Assert.True(enumPrintMethod != null, $"The enum method `{enumPrintMethodName}` does not exist.");
        foreach (var enumValue in enumValues)
        {
            var enumPrintMethodResult = enumPrintMethod!.Invoke(null, new[] { enumValue });
            Assert.True(enumPrintMethodResult == null, $"Unexpected result from enum print method `{enumPrintMethodName}`");
        }

        var enumReturnMethodName = $"{name}__return_{name}";
        var enumReturnMethod = _cSharpCodeBuildResult.ClassType.GetMethod(enumReturnMethodName);
        Assert.True(enumReturnMethod != null, $"The enum method `{enumReturnMethodName}` does not exist.");

        foreach (var enumValue in enumValues)
        {
            var enumReturnMethodResult = enumReturnMethod!.Invoke(null, new[] { enumValue });
            Assert.True(
                enumReturnMethodResult!.Equals(enumValue),
                $"Unexpected result from enum return method `{enumReturnMethodName}`");
        }
    }

    private TestCSharpCodeAbstractSyntaxTree AbstractSyntaxTree(
        SyntaxTree syntaxTree,
        WriteCodeCSharpInput input)
    {
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
            CreateTestNode(
                member,
                methodsByNameBuilder,
                enumsByNameBuilder,
                structsByNameBuilder,
                macroObjectsByNameBuilder);
        }

        var ast = new TestCSharpCodeAbstractSyntaxTree(
            enumsByNameBuilder.ToImmutable(),
            methodsByNameBuilder.ToImmutable(),
            macroObjectsByNameBuilder.ToImmutable(),
            structsByNameBuilder.ToImmutable());
        return ast;
    }

    private CCodeBuildResult CCodeBuildResult(CMakeLibraryBuilder cMakeLibraryBuilder)
    {
        const string cMakeDirectoryPath = "../../../../src/c/tests/_container_library/";
        const string libraryOutputDirectoryPath = ".";
        var result = cMakeLibraryBuilder.BuildLibrary(cMakeDirectoryPath, libraryOutputDirectoryPath);
        Assert.True(result.IsSuccess);

        return result;
    }

    private static CSharpCodeBuildResult CSharpBuildResult(SyntaxTree syntaxTree)
    {
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

        var assembly = Assembly.Load(dllStream.ToArray());

        NativeLibrary.SetDllImportResolver(assembly, NativeLibraryResolver);

        var cSharpCodeBuiltResult = new CSharpCodeBuildResult(emitResult, assembly);
        return cSharpCodeBuiltResult;
    }

    private static nint NativeLibraryResolver(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        var fileName = Native.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "container_library.dll",
            NativeOperatingSystem.macOS => "lib_container_library.dylib",
            NativeOperatingSystem.Linux => "lib_container_library.so",
            _ => throw new NotImplementedException()
        };

        var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        var handle = NativeLibrary.Load(filePath);
        return handle;
    }

    private void CreateTestNode(
        MemberDeclarationSyntax member,
        ImmutableDictionary<string, CSharpTestFunction>.Builder methodsByNameBuilder,
        ImmutableDictionary<string, CSharpTestEnum>.Builder enumsByNameBuilder,
        ImmutableDictionary<string, CSharpTestStruct>.Builder structsByNameBuilder,
        ImmutableDictionary<string, CSharpTestMacroObject>.Builder macroObjectsByNameBuilder)
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
                    return;
                }

                var value = CreateTestMacroObject(syntaxNode, fieldName);
                macroObjectsByNameBuilder.Add(fieldName, value);
                break;
            }
        }
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
