// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;
using C2CS.Foundation;
using C2CS.Native;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public class CSharpLibraryCompiler
{
    public CSharpLibraryCompilerResult? Compile(
        CSharpProject project,
        CSharpCodeGeneratorOptions options,
        DiagnosticCollection diagnostics)
    {
        // NOTE: Because `LibraryImportAttribute` uses a C# source generator which can not be referenced in code, we use the .NET SDK directly instead of using Roslyn.

        if (!CanCompile())
        {
            diagnostics.Add(new CSharpCompileSkipDiagnostic(".NET 7+ SDK not found"));
            return null;
        }

        var temporaryDirectoryPath = Directory.CreateTempSubdirectory("c2cs-").FullName;
        try
        {
            TryCompile(temporaryDirectoryPath, project, diagnostics);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Directory.Delete(temporaryDirectoryPath, true);
            diagnostics.Add(new DiagnosticPanic(e));
        }

        return null;
    }

    private static void TryCompile(
        string directoryPath,
        CSharpProject project,
        DiagnosticCollection diagnostics)
    {
        var cSharpProjectFilePath = Path.Combine(directoryPath, "Project.csproj");

        CreateCSharpProjectFile(cSharpProjectFilePath);
        CreateDocumentFiles(directoryPath, project.Documents);

        var compilationOutput = $"dotnet build {cSharpProjectFilePath} --verbosity quiet".ExecuteShell();
        if (compilationOutput.ExitCode != 0)
        {
            diagnostics.Add(new CSharpCompileDiagnostic(compilationOutput.Output));
        }
    }

    private static void CreateDocumentFiles(string directoryPath, ImmutableArray<CSharpProjectDocument> documents)
    {
        foreach (var document in documents)
        {
            File.WriteAllText(Path.Combine(directoryPath, document.FileName), document.Contents);
        }
    }

    private static void CreateCSharpProjectFile(string cSharpProjectFilePath)
    {
        var cSharpProjectFileContents = @"
<Project Sdk=""Microsoft.NET.Sdk"">
    <PropertyGroup>
        <TargetFramework>net7.0</TargetFramework>
        <Nullable>enable</Nullable>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>
</Project>
".Trim();
        File.WriteAllText(cSharpProjectFilePath, cSharpProjectFileContents);
    }

    private static bool CanCompile()
    {
        var shellOutput = "dotnet --list-sdks".ExecuteShell();
        if (shellOutput.ExitCode != 0)
        {
            return false;
        }

        var lines = shellOutput.Output.Split(Environment.NewLine);
        if (lines.Length == 0)
        {
            return false;
        }

        foreach (var line in lines)
        {
            var parse = line.Split('[', StringSplitOptions.RemoveEmptyEntries);
            var versionString = parse[0].Trim();

            if (!Version.TryParse(versionString, out var version))
            {
                continue;
            }

            if (version.Major < 5)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
