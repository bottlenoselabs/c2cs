// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public class ExtractAbstractSyntaxTreeUseCase : UseCase<
    ExtractAbstractSyntaxTreeRequest, ExtractAbstractSyntaxTreeInput, ExtractAbstractSyntaxTreeResponse>
{
    private static string _clangNativeLibraryPath = null!;
    private readonly CJsonSerializer _cJsonSerializer;

    public ExtractAbstractSyntaxTreeUseCase(ILogger logger, CJsonSerializer cJsonSerializer)
        : base("Extract AST C", logger, new ExtractAbstractSyntaxTreeValidator())
    {
        _cJsonSerializer = cJsonSerializer;
    }

    protected override ExtractAbstractSyntaxTreeResponse Execute(ExtractAbstractSyntaxTreeInput input)
    {
        SetupClang();

        var translationUnit = Parse(
            input.InputFilePath,
            input.IsEnabledFindSdk,
            input.IncludeDirectories,
            input.ClangDefines,
            input.TargetPlatform,
            input.ClangArguments);

        var abstractSyntaxTreeC = Explore(
            translationUnit,
            input.IncludeDirectories,
            input.ExcludedHeaderFiles,
            input.OpaqueTypeNames,
            input.FunctionNamesWhitelist,
            input.TargetPlatform);

        Write(
            input.OutputFilePath,
            abstractSyntaxTreeC);

        var response = new ExtractAbstractSyntaxTreeResponse
        {
            FilePath = input.OutputFilePath
        };

        return response;
    }

    private void SetupClang()
    {
        BeginStep("Setup Clang");

        var operatingSystem = RuntimePlatform.Host.OperatingSystem;
        if (operatingSystem == RuntimeOperatingSystem.macOS)
        {
            _clangNativeLibraryPath = "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib";
            if (!File.Exists(_clangNativeLibraryPath))
            {
                throw new InvalidOperationException(
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

        EndStep();

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
            NativeLibrary.SetDllImportResolver(typeof(bottlenoselabs.clang).Assembly, ResolveClang);
        }
        catch (ArgumentException)
        {
            // already set; ignore
        }
    }

    private static IntPtr ResolveClang(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!NativeLibrary.TryLoad(_clangNativeLibraryPath, out var handle))
        {
            throw new ClangException($"Could not load libclang: {_clangNativeLibraryPath}");
        }

        return handle;
    }

    private CXTranslationUnit Parse(
        string inputFilePath,
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        RuntimePlatform targetPlatform,
        ImmutableArray<string> clangArguments)
    {
        BeginStep("Parse C code from disk");

        var clangArgs = ClangArgumentsBuilder.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            defines,
            targetPlatform,
            clangArguments);
        var result = ClangTranslationUnitParser.Parse(
            Diagnostics, inputFilePath, clangArgs);

        EndStep();
        return result;
    }

    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> excludedHeaderFiles,
        ImmutableArray<string> opaqueTypeNames,
        ImmutableArray<string> functionNamesWhitelist,
        RuntimePlatform targetPlatform)
    {
        BeginStep("Extract C abstract syntax tree");

        var clangExplorer = new ClangTranslationUnitExplorer(
            Diagnostics,
            includeDirectories,
            excludedHeaderFiles,
            opaqueTypeNames,
            functionNamesWhitelist,
            targetPlatform);
        var result = clangExplorer.AbstractSyntaxTree(translationUnit);

        EndStep();
        return result;
    }

    private void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
    {
        BeginStep("Write C abstract syntax tree to disk");
        _cJsonSerializer.Write(abstractSyntaxTree, outputFilePath);
        EndStep();
    }
}
