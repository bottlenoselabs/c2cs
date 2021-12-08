// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

public class
    CExtractAbstractSyntaxTreeUseCase : UseCase<CExtractAbstractSyntaxTreeRequest, CExtractAbstractSyntaxTreeResponse>
{
    private static string _clangNativeLibraryPath = null!;

    protected override void Execute(
        CExtractAbstractSyntaxTreeRequest request,
        CExtractAbstractSyntaxTreeResponse response)
    {
        Validate(request);
        TotalSteps(4);

        Step("Setup Clang", Runtime.OperatingSystem, SetupClang);

        var translationUnit = Step(
            "Parse C code from disk",
            request.InputFilePath,
            request.AutomaticallyFindSoftwareDevelopmentKit,
            request.IncludeDirectories,
            request.Defines,
            request.Bitness,
            request.ClangArgs,
            Parse);

        var abstractSyntaxTreeC = Step(
            "Extract C abstract syntax tree",
            translationUnit,
            request.IncludeDirectories,
            request.IgnoredFiles,
            request.OpaqueTypes,
            request.WhitelistFunctionNames,
            request.Bitness ?? (RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.X64 ? 64 : 32),
            Explore);

        Step(
            "Write C abstract syntax tree to disk",
            request.OutputFilePath,
            abstractSyntaxTreeC,
            Write);
    }

    private static void SetupClang(RuntimeOperatingSystem operatingSystem)
    {
        if (operatingSystem == RuntimeOperatingSystem.macOS)
        {
            _clangNativeLibraryPath = "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib";
            if (!File.Exists(_clangNativeLibraryPath))
            {
                throw new ClangException(
                    "Please install CommandLineTools for macOS. This will install `libclang.dylib`. Use the command `xcode-select --install`.");
            }
        }
        else if (operatingSystem == RuntimeOperatingSystem.Linux)
        {
            _clangNativeLibraryPath = Path.Combine(AppContext.BaseDirectory, "libclang.so");
            if (!File.Exists(_clangNativeLibraryPath))
            {
                DownloadLibClang("ubuntu.20.04-x64", _clangNativeLibraryPath);
            }
        }
        else if (operatingSystem == RuntimeOperatingSystem.Windows)
        {
            _clangNativeLibraryPath = Path.Combine(AppContext.BaseDirectory, "libclang.dll");
            if (!File.Exists(_clangNativeLibraryPath))
            {
                DownloadLibClang("win-x64", _clangNativeLibraryPath);
            }
        }

        static void DownloadLibClang(string runtimeIdentifier, string target)
        {
            var zipFilePath = Path.Combine(AppContext.BaseDirectory, "libclang.zip");
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            DownloadFile(
                $"https://www.nuget.org/api/v2/package/libclang.runtime.{runtimeIdentifier}",
                zipFilePath);

            var extractDirectory = Path.Combine(AppContext.BaseDirectory, "libclang");
            if (Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, true);
            }

            Directory.CreateDirectory(extractDirectory);
            ZipFile.ExtractToDirectory(zipFilePath, extractDirectory);

            var fileExtension = Path.GetExtension(target);
            File.Copy(
                Path.Combine(extractDirectory, $"runtimes/{runtimeIdentifier}/native/libclang{fileExtension}"),
                target);
        }

        static void DownloadFile(string url, string filePath)
        {
            using var client = new HttpClient();
            var uri = new Uri(url);
            using var response = client.GetStreamAsync(uri).Result;
            using var fileStream = new FileStream(filePath, FileMode.CreateNew);
            response.CopyToAsync(fileStream).Wait();
        }

        try
        {
            NativeLibrary.SetDllImportResolver(typeof(clang).Assembly, ResolveClang);
        }
        catch (ArgumentException)
        {
            // already set; ignore
        }
    }

    private static IntPtr ResolveClang(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        return NativeLibrary.Load(_clangNativeLibraryPath);
    }

    private static void Validate(CExtractAbstractSyntaxTreeRequest cExtractAbstractSyntaxTreeRequest)
    {
        if (!File.Exists(cExtractAbstractSyntaxTreeRequest.InputFilePath))
        {
            throw new UseCaseException($"File does not exist: `{cExtractAbstractSyntaxTreeRequest.InputFilePath}`.");
        }
    }

    private static clang.CXTranslationUnit Parse(
        string inputFilePath,
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        int? bitness,
        ImmutableArray<string> clangArguments)
    {
        var clangArgs = ClangArgumentsBuilder.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            defines,
            bitness,
            clangArguments);
        return ClangTranslationUnitParser.Parse(inputFilePath, clangArgs);
    }

    private CAbstractSyntaxTree Explore(
        clang.CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> ignoredFiles,
        ImmutableArray<string> opaqueTypes,
        ImmutableArray<string> whitelistFunctionNames,
        int bitness)
    {
        var clangExplorer = new ClangTranslationUnitExplorer(
            Diagnostics, includeDirectories, ignoredFiles, opaqueTypes, whitelistFunctionNames);
        return clangExplorer.AbstractSyntaxTree(translationUnit, bitness);
    }

    private static void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
    {
        var outputDirectory = Path.GetDirectoryName(outputFilePath)!;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = AppContext.BaseDirectory;
            outputFilePath = Path.Combine(Environment.CurrentDirectory, outputFilePath);
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        var serializerOptions = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
        var serializerContext = new CJsonSerializerContext(serializerOptions);
        var fileContents = JsonSerializer.Serialize(abstractSyntaxTree, serializerContext.Options);

        // File.WriteAllText doesn't flush until process exits on macOS .NET 5 lol
        using var fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate);
        using var textWriter = new StreamWriter(fileStream);
        textWriter.Write(fileContents);
        textWriter.Close();
        fileStream.Close();

        Console.WriteLine(outputFilePath);
    }
}
