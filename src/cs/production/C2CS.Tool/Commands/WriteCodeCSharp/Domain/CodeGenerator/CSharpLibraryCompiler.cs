// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using bottlenoselabs.Common;
using bottlenoselabs.Common.Diagnostics;
using C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;

public class CSharpLibraryCompiler
{
    public Assembly? Compile(
        CSharpProject project,
        CSharpCodeGeneratorOptions options,
        DiagnosticsSink diagnostics)
    {
        // NOTE: Because `LibraryImportAttribute` uses a C# source generator which can not be referenced in code, we use the .NET SDK directly instead of using Roslyn.

        if (!IsDotNetSdkInstalled())
        {
            diagnostics.Add(new CSharpCompileSkipDiagnostic(".NET 8 SDK not found"));
            return null;
        }

        return TryCompile(project, options, diagnostics);
    }

    private static Assembly? TryCompile(
        CSharpProject project,
        CSharpCodeGeneratorOptions options,
        DiagnosticsSink diagnostics)
    {
        var temporaryDirectoryPath = Directory.CreateTempSubdirectory("c2cs-").FullName;
        try
        {
            return Compile(temporaryDirectoryPath, project, options, diagnostics);
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            Directory.Delete(temporaryDirectoryPath, true);
            diagnostics.Add(new DiagnosticPanic(e));
            return null;
        }
    }

    private static Assembly? Compile(
        string directoryPath,
        CSharpProject project,
        CSharpCodeGeneratorOptions options,
        DiagnosticsSink diagnostics)
    {
        var cSharpProjectFilePath = Path.Combine(directoryPath, "Project.csproj");

        CreateCSharpProjectFile(cSharpProjectFilePath, options);
        CreateDocumentFiles(directoryPath, project.Documents);

        var compilationOutput = "dotnet build --verbosity quiet --nologo --no-incremental".ExecuteShellCommand(workingDirectory: directoryPath);
        if (compilationOutput.ExitCode != 0)
        {
            var lines = compilationOutput.Output.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                diagnostics.Add(new CSharpCompileDiagnostic(line));
            }

            return null;
        }

        var assemblyFilePath = Path.Combine(directoryPath, "bin/Debug/net8.0/Project.dll");
        try
        {
            return Assembly.LoadFile(assemblyFilePath);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (Exception e)
        {
            diagnostics.Add(new DiagnosticPanic(e));
            throw;
        }
    }

    private static void CreateDocumentFiles(string directoryPath, ImmutableArray<CSharpProjectDocument> documents)
    {
        foreach (var document in documents)
        {
            File.WriteAllText(Path.Combine(directoryPath, document.FileName), document.Contents);
        }
    }

    private static void CreateCSharpProjectFile(
        string cSharpProjectFilePath,
        CSharpCodeGeneratorOptions options)
    {
        var fileContents = """

                           <Project Sdk="Microsoft.NET.Sdk">

                               <PropertyGroup>
                                   <TargetFramework>net8.0</TargetFramework>
                                   <Nullable>enable</Nullable>
                                   <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                                   <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                                   <NoWarn>$(NoWarn);CS8981</NoWarn>
                               </PropertyGroup>

                           """.Trim();

        if (!options.IsEnabledGenerateCSharpRuntimeCode)
        {
            fileContents += """

                                <!-- NuGet package references -->
                                <ItemGroup>
                                    <PackageReference Include="bottlenoselabs.Bindgen.Runtime" Version="*" />
                                </ItemGroup>

                            """;
        }

        fileContents += @"
</Project>
".Trim();

        File.WriteAllText(cSharpProjectFilePath, fileContents);
    }

    private static bool IsDotNetSdkInstalled()
    {
        var shellOutput = "dotnet --list-sdks".ExecuteShellCommand();
        if (shellOutput.ExitCode != 0)
        {
            return false;
        }

        var lines = shellOutput.Output.Trim().Split(Environment.NewLine);
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

            if (version.Major < 8)
            {
                continue;
            }

            return true;
        }

        return false;
    }
}
