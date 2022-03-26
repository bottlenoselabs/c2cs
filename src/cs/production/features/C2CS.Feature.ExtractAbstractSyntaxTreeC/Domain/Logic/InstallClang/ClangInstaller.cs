// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.InstallClang;

public sealed class ClangInstaller
{
    private string _clangNativeLibraryPath = null!;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public ClangInstaller(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public void Install()
    {
        var operatingSystem = RuntimePlatform.Host.OperatingSystem;

        try
        {
            _clangNativeLibraryPath = operatingSystem switch
            {
                RuntimeOperatingSystem.Windows => InstallWindows(),
                RuntimeOperatingSystem.Linux => InstallLinux(),
                RuntimeOperatingSystem.macOS => InstallMacOs(),
                _ => string.Empty
            };
        }
        catch (Exception e)
        {
            _logger.InstallClangFailed(e);
            throw;
        }

        try
        {
            NativeLibrary.SetDllImportResolver(typeof(bottlenoselabs.clang).Assembly, ResolveClang);
        }
        catch (ArgumentException)
        {
            // already set; ignore
        }

        _logger.InstallClangSuccess();
    }

    private string InstallWindows()
    {
        var filePath = _fileSystem.Path.Combine(AppContext.BaseDirectory, "libclang.dll");
        if (!_fileSystem.File.Exists(filePath))
        {
            DownloadLibClang("win-x64", filePath);
        }

        return filePath;
    }

    private string InstallLinux()
    {
        var filePath = _fileSystem.Path.Combine(AppContext.BaseDirectory, "libclang.so");
        if (!_fileSystem.File.Exists(filePath))
        {
            DownloadLibClang("ubuntu.20.04-x64", filePath);
        }

        return filePath;
    }

    private string InstallMacOs()
    {
        const string filePath = "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib";
        if (!_fileSystem.File.Exists(filePath))
        {
            throw new InvalidOperationException(
                "Please install CommandLineTools for macOS. This will install `libclang.dylib`. Use the command `xcode-select --install`.");
        }

        return filePath;
    }

    private void DownloadLibClang(string runtimeIdentifier, string target)
    {
        var zipFilePath = _fileSystem.Path.Combine(AppContext.BaseDirectory, "libclang.zip");
        if (_fileSystem.File.Exists(zipFilePath))
        {
            _fileSystem.File.Delete(zipFilePath);
        }

        DownloadFile(
            $"https://www.nuget.org/api/v2/package/libclang.runtime.{runtimeIdentifier}",
            zipFilePath);

        var extractDirectory = _fileSystem.Path.Combine(AppContext.BaseDirectory, "libclang");
        if (_fileSystem.Directory.Exists(extractDirectory))
        {
            _fileSystem.Directory.Delete(extractDirectory, true);
        }

        _fileSystem.Directory.CreateDirectory(extractDirectory);
        ZipFile.ExtractToDirectory(zipFilePath, extractDirectory);

        var fileExtension = _fileSystem.Path.GetExtension(target);
        _fileSystem.File.Copy(
            _fileSystem.Path.Combine(extractDirectory, $"runtimes/{runtimeIdentifier}/native/libclang{fileExtension}"),
            target);
    }

    private void DownloadFile(string url, string filePath)
    {
        using var client = new HttpClient();
        var uri = new Uri(url);
        using var response = client.GetStreamAsync(uri).Result;
        using var fileStream = _fileSystem.File.Create(filePath);
        response.CopyToAsync(fileStream).Wait();
    }

    private IntPtr ResolveClang(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!NativeLibrary.TryLoad(_clangNativeLibraryPath, out var handle))
        {
            throw new ClangException($"Could not load libclang: {_clangNativeLibraryPath}");
        }

        return handle;
    }
}
