// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using C2CS.Features.BuildCLibrary.Domain;
using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Features.WriteCodeCSharp;
using C2CS.Features.WriteCodeCSharp.Output;
using C2CS.Native;
using C2CS.Tests.Data.Models;
using C2CS.Tests.Foundation;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests;

public sealed class TestFixtureCSharpCode
{
    private readonly TestCSharpCodeAbstractSyntaxTree _abstractSyntaxTree;
    private const string ClassName = "bottlenoselabs._container_library";
    private readonly Type? _classType;

    public WriteCodeCSharpOutput Output { get; }

    public TestFixtureCSharpCode()
    {
        var sourceDirectoryPath = GetSourceDirectoryPath();
        var bindgenConfigFilePath = GetBindgenConfigFilePath(sourceDirectoryPath);
        GenerateCAbstractSyntaxTree(bindgenConfigFilePath, sourceDirectoryPath);

        BuildCLibrary(sourceDirectoryPath, ImmutableArray<string>.Empty);
        Output = Run(sourceDirectoryPath);
        Assert.True(Output.IsSuccess);
        _abstractSyntaxTree = CreateCSharpAbstractSyntaxTree(Output);

        var assembly = Output.CompilerResult?.Assembly;
        if (assembly != null)
        {
            _classType = assembly.GetType(ClassName);
            NativeLibrary.SetDllImportResolver(assembly, NativeLibraryResolver);
        }
    }

    public void AssertCSharpCodeCompiles(WriteCodeCSharpOutput output)
    {
        if (!Output.Input.GeneratorOptions.IsEnabledVerifyCSharpCodeCompiles)
        {
            return;
        }

        Assert.True(output.CompilerResult != null, "Error compiling generated C# code.");
        Assert.True(output.CompilerResult!.Assembly != null, "Error compiling generated C# code.");
        Assert.True(output.CompilerResult.EmitResult != null, "Error compiling generated C# code.");

        foreach (var diagnostic in output.CompilerResult.EmitResult!.Diagnostics)
        {
            var isWarningOrError = diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Warning &&
                                   diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error;
            Assert.True(isWarningOrError, $"C# code compilation diagnostic: {diagnostic}.");
        }

        Assert.True(output.CompilerResult.EmitResult.Success, "Generated C# code did not compile successfully.");
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _abstractSyntaxTree.Enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");

        Assert.True(_classType != null, $"The class `{ClassName}` does not exist.");
        var enumType = _classType!.GetNestedType(name);
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

    private static void BuildCLibrary(string sourceDirectoryPath, ImmutableArray<string> additionalCMakeArguments)
    {
        var services = TestHost.Services;
        var cmakeLibraryBuilder = services.GetService<CMakeLibraryBuilder>()!;

        var cMakeDirectoryPath =
            Path.GetFullPath(Path.Combine(sourceDirectoryPath, "..", "..", "..", "..", "src", "c", "tests", "_container_library"));

        var input = new BuildCLibraryInput
        {
            CMakeDirectoryPath = cMakeDirectoryPath,
            OutputDirectoryPath = AppContext.BaseDirectory
        };
        var result = cmakeLibraryBuilder.BuildLibrary(input, additionalCMakeArguments);
        Assert.True(result, "Failed to build C library.");
    }

    private static void GenerateCAbstractSyntaxTree(
        string bindgenConfigFilePath,
        string sourceDirectoryPath)
    {
        var extractShellOutput = $"castffi extract --config {bindgenConfigFilePath}".ExecuteShell();
        Assert.True(extractShellOutput.ExitCode == 0, "error extracting platform ASTs");

        var abstractSyntaxTreeDirectoryPath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "ast"));
        var mergedAbstractSyntaxTreeFilePath =
            Path.GetFullPath(Path.Combine(sourceDirectoryPath, "ast", "cross-platform.json"));
        var astShellOutput =
            $"castffi merge --inputDirectoryPath {abstractSyntaxTreeDirectoryPath} --outputFilePath {mergedAbstractSyntaxTreeFilePath}"
                .ExecuteShell();
        Assert.True(astShellOutput.ExitCode == 0, "error merging platform ASTs");
    }

    private WriteCodeCSharpOutput Run(string sourceDirectoryPath)
    {
        var services = TestHost.Services;
        var writeCodeCSharpTool = services.GetService<WriteCodeCSharpTool>()!;

        var configGenerateCSharpCodeFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, "config-cs.json"));
        var outputWriteCSharp = writeCodeCSharpTool.Run(configGenerateCSharpCodeFilePath);
        Assert.True(outputWriteCSharp != null);
        Assert.True(outputWriteCSharp!.Diagnostics.Length == 0, $"Diagnostics were reported when writing C# code: {outputWriteCSharp.OutputFileDirectory}");
        Assert.True(outputWriteCSharp.IsSuccess, "Writing C# code failed.");
        return outputWriteCSharp;
    }

    private void AssertPInvokeEnum(string name, Array enumValues)
    {
        // TODO: Inter process communication

        var enumPrintMethodName = $"{name}__print_{name}";
        Assert.True(_classType != null, $"The class `{ClassName}` does not exist.");
        var enumPrintMethod = _classType!.GetMethod(enumPrintMethodName);
        Assert.True(enumPrintMethod != null, $"The enum method `{enumPrintMethodName}` does not exist.");
        foreach (var enumValue in enumValues)
        {
            var enumPrintMethodResult = enumPrintMethod!.Invoke(null, new[] { enumValue });
            Assert.True(enumPrintMethodResult == null, $"Unexpected result from enum print method `{enumPrintMethodName}`");
        }

        var enumReturnMethodName = $"{name}__return_{name}";
        var enumReturnMethod = _classType.GetMethod(enumReturnMethodName);
        Assert.True(enumReturnMethod != null, $"The enum method `{enumReturnMethodName}` does not exist.");

        foreach (var enumValue in enumValues)
        {
            var enumReturnMethodResult = enumReturnMethod!.Invoke(null, new[] { enumValue });
            Assert.True(
                enumReturnMethodResult!.Equals(enumValue),
                $"Unexpected result from enum return method `{enumReturnMethodName}`");
        }
    }

    private TestCSharpCodeAbstractSyntaxTree CreateCSharpAbstractSyntaxTree(
        WriteCodeCSharpOutput output)
    {
        var codeFilePath =
            Path.Combine(output.OutputFileDirectory, $"{output.Input.GeneratorOptions.ClassName}.gen.cs");
        var code = File.ReadAllText(codeFilePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();
        var generatorOptions = output.Input.GeneratorOptions;

        Assert.True(compilationUnitSyntax.Members.Count == 1);
        var @namespace = compilationUnitSyntax.Members[0] as BaseNamespaceDeclarationSyntax;
        Assert.True(@namespace != null);
        Assert.True(@namespace!.Name.ToString() == generatorOptions.NamespaceName);

        Assert.True(@namespace.Members.Count == 1);
        var @class = @namespace.Members[0] as ClassDeclarationSyntax;
        Assert.True(@class != null);
        Assert.True(@class!.Identifier.ToString() == generatorOptions.ClassName);

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

    private static nint NativeLibraryResolver(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        var fileName = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "_container_library.dll",
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
        var layout = CreateTestStructLayout(syntaxNode);
        var fields = CreateTestStructFields(syntaxNode, layout);

        var result = new CSharpTestStruct
        {
            Name = name,
            Layout = layout,
            Fields = fields
        };
        return result;
    }

    private ImmutableArray<CSharpTestStructField> CreateTestStructFields(
        StructDeclarationSyntax syntaxNode,
        CSharpTestStructLayout layout)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestStructField>();

        foreach (var syntaxNodeMember in syntaxNode.Members)
        {
            if (syntaxNodeMember is not FieldDeclarationSyntax syntaxNodeField)
            {
                continue;
            }

            var field = CreateTestStructField(syntaxNodeField, layout);
            builder.Add(field);
        }

        return builder.ToImmutable();
    }

    private CSharpTestStructField CreateTestStructField(
        FieldDeclarationSyntax syntaxNode,
        CSharpTestStructLayout structLayout)
    {
        var variableSyntaxNode = syntaxNode.Declaration;
        var name = variableSyntaxNode.Variables[0].Identifier.Text;
        var typeName = variableSyntaxNode.Type.ToString();

        int? offsetOf;
        if (structLayout.LayoutKind == "LayoutKind.Explicit")
        {
            offsetOf = FieldOffsetOf(name, syntaxNode);
        }
        else
        {
            offsetOf = null;
        }

        var result = new CSharpTestStructField
        {
            Name = name,
            TypeName = typeName,
            OffsetOf = offsetOf
        };

        return result;
    }

    private CSharpTestStructLayout CreateTestStructLayout(StructDeclarationSyntax syntaxNode)
    {
        var attribute = GetAttribute("StructLayout", syntaxNode);
        var arguments = attribute.ArgumentList!.Arguments;

        int? sizeOf;
        int? packOf;

        var layoutKind = arguments[0].Expression.ToFullString();
        if (layoutKind == "LayoutKind.Explicit")
        {
            var sizeOfString = arguments[1].Expression.ToFullString();
            sizeOf = int.Parse(sizeOfString, CultureInfo.InvariantCulture);
            var packOfString = arguments[2].Expression.ToFullString();
            packOf = int.Parse(packOfString, CultureInfo.InvariantCulture);
        }
        else
        {
            sizeOf = null;
            packOf = null;
        }

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

    private static string GetSourceDirectoryPath()
    {
        var gitRepositoryPath = GetGitRepositoryPath();
        var sourceDirectoryPath =
            Path.GetFullPath(Path.Combine(gitRepositoryPath, "src", "cs", "tests", "C2CS.Tests"));
        return sourceDirectoryPath;
    }

    private static string GetGitRepositoryPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var directoryInfo = new DirectoryInfo(baseDirectory);
        while (true)
        {
            var files = directoryInfo.GetFiles(".gitignore", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                return directoryInfo.FullName;
            }

            directoryInfo = directoryInfo.Parent;
            if (directoryInfo == null)
            {
                return string.Empty;
            }
        }
    }

    private static string GetBindgenConfigFilePath(string sourceDirectoryPath)
    {
        var bindgenConfigFileName = NativeUtility.OperatingSystem switch
        {
            NativeOperatingSystem.Windows => "config-windows.json",
            NativeOperatingSystem.macOS => "config-macos.json",
            NativeOperatingSystem.Linux => "config-linux.json",
            _ => throw new NotImplementedException()
        };

        var bindgenConfigFilePath = Path.GetFullPath(Path.Combine(sourceDirectoryPath, bindgenConfigFileName));
        return bindgenConfigFilePath;
    }
}
