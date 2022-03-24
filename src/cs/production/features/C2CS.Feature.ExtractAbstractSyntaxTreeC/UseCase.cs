// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;
using static bottlenoselabs.clang;
using CJsonSerializerContext = C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.CJsonSerializerContext;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public class UseCase : UseCase<RequestExtractAbstractSyntaxTreeC, Input, Response>
{
    private static string _clangNativeLibraryPath = null!;

    public UseCase()
        : base(new Validator())
    {
    }

    protected override Response Execute(Input input)
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

        var response = new Response
        {
            AbstractSyntaxTree = abstractSyntaxTreeC
        };

        return response;
    }

    [UseCaseStep("Setup Clang")]
    private void SetupClang()
    {
        BeginStep();

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
            throw new ExceptionClang($"Could not load libclang: {_clangNativeLibraryPath}");
        }

        return handle;
    }

    [UseCaseStep("Parse C code from disk")]
    private CXTranslationUnit Parse(
        string inputFilePath,
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        RuntimePlatform targetPlatform,
        ImmutableArray<string> clangArguments)
    {
        BeginStep();
        var clangArgs = BuilderClangArguments.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            defines,
            targetPlatform,
            clangArguments);
        var result = ParserClangTranslationUnit.Parse(
            Diagnostics, inputFilePath, clangArgs);
        EndStep();
        return result;
    }

    [UseCaseStep("Extract C abstract syntax tree")]
    private CAbstractSyntaxTree Explore(
        CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> excludedHeaderFiles,
        ImmutableArray<string> opaqueTypeNames,
        ImmutableArray<string> functionNamesWhitelist,
        RuntimePlatform targetPlatform)
    {
        BeginStep();
        var clangExplorer = new ExplorerClangTranslationUnit(
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

    [UseCaseStep("Write C abstract syntax tree to disk")]
    private void Write(
        string outputFilePath, CAbstractSyntaxTree abstractSyntaxTree)
    {
        BeginStep();
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

        var serializerContext = CJsonSerializerContext.Create();
        var fileContents = JsonSerializer.Serialize(abstractSyntaxTree, serializerContext.Options);

        // File.WriteAllText doesn't flush until process exits on macOS .NET 5 lol
        using var fileStream = new FileStream(outputFilePath, FileMode.OpenOrCreate);
        using var textWriter = new StreamWriter(fileStream);
        textWriter.Write(fileContents);
        textWriter.Close();
        fileStream.Close();

        Console.WriteLine(outputFilePath);
        EndStep();
    }
}
