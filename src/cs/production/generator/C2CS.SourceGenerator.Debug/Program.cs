// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Buildalyzer;
using Buildalyzer.Environment;
using C2CS.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace C2CS;

public static class Program
{
    public static int Main(string[] args)
    {
        var projectFilePath = FindCSharpProject("macOS.MessageBox.csproj");

        var manager = new AnalyzerManager();
        var analyzer = manager.GetProject(projectFilePath);
        var results = analyzer.Build();
        var result = results.First();

        var syntaxTrees = new List<SyntaxTree>();
        foreach (var sourceCodeFilePath in result.SourceFiles)
        {
            var sourceCode = File.ReadAllText(sourceCodeFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: sourceCodeFilePath);
            syntaxTrees.Add(syntaxTree);
        }

        var metadataReferences = new List<MetadataReference>();
        foreach (var reference in result.References)
        {
            var metadataReference = MetadataReference.CreateFromFile(reference);
            metadataReferences.Add(metadataReference);
        }

        var options = new CSharpCompilationOptions(
            OutputKind.ConsoleApplication,
            deterministic: true,
            optimizationLevel: OptimizationLevel.Debug);
        var compilation = CSharpCompilation.Create(
            "cs", syntaxTrees, metadataReferences, options);

        var generator = new BindgenGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation, out var outputCompilation, out var diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            Console.Write(diagnostic);
        }

        return 0;
    }

    private static string FindCSharpProject(string projectFileName)
    {
        var gitRepositoryPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", ".."));
        var sourceDirectoryPath = Path.Combine(gitRepositoryPath, "src", "cs");
        var fileSystemEntries = Directory.EnumerateFileSystemEntries(
            sourceDirectoryPath, "*.csproj", SearchOption.AllDirectories);
        foreach (var filePath in fileSystemEntries)
        {
            var fileName = Path.GetFileName(filePath);
            if (fileName == projectFileName)
            {
                return filePath;
            }
        }

        throw new InvalidOperationException("Could not find the project file: " + projectFileName);
    }
}
