// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ReadCodeC.Domain.InstallClang;

public sealed class ClangInstaller
{
    private bool _isInstalled;
    private readonly object _lock = new();

    private string _clangNativeLibraryFilePath = null!;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public ClangInstaller(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public void Install(NativeOperatingSystem operatingSystem)
    {
        lock (_lock)
        {
            if (_isInstalled)
            {
                _logger.InstallClangSuccessAlreadyInstalled();
                return;
            }

            _clangNativeLibraryFilePath = GetClangFilePath(operatingSystem);

            try
            {
                NativeLibrary.SetDllImportResolver(typeof(bottlenoselabs.clang).Assembly, ResolveClang);
            }
            catch (InvalidOperationException)
            {
                // already set; ignore
            }

            _logger.InstallClangSuccess();
            _isInstalled = true;
        }
    }

    private string GetClangFilePath(NativeOperatingSystem operatingSystem)
    {
        string result;

        try
        {
            result = operatingSystem switch
            {
                NativeOperatingSystem.Windows => GetClangFilePathWindows(),
                NativeOperatingSystem.Linux => GetClangFilePathLinux(),
                NativeOperatingSystem.macOS => GetClangFilePathMacOs(),
                _ => string.Empty
            };
        }
        catch (Exception e)
        {
            _logger.InstallClangFailed(e);
            throw;
        }

        return result;
    }

    private string GetClangFilePathWindows()
    {
        var filePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "libclang.dll"),
            Path.Combine(AppContext.BaseDirectory, "clang.dll"),
            @"C:\Program Files\LLVM\bin\libclang.dll" // choco install llvm
        };

        const string errorMessage = "`libclang.dll` or `clang.dll` is missing. Please put a `libclang.dll` or `clang.dll` file next to this application or install Clang for Windows. To install Clang for Windows using Chocolatey, use the command `choco install llvm`.";
        return SearchForClangFilePath(errorMessage, filePaths);
    }

    private string GetClangFilePathLinux()
    {
        var filePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "libclang.so"),
            "/usr/lib/llvm-10/lib/libclang.so.1" // apt-get install clang
        };

        const string errorMessage = "`libclang.so`is missing. Please put a `libclang.so` file next to this application or install Clang for Linux. To install Clang for Debian-based linux distributions, use the command `apt-get update && apt-get install clang`.";
        return SearchForClangFilePath(errorMessage, filePaths);
    }

    private string GetClangFilePathMacOs()
    {
        var filePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "libclang.dylib"),
            "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib" // xcode-select --install
        };

        const string errorMessage = "`libclang.dylib` is missing. Please put a `libclang.dylib` file next to this application or install CommandLineTools for macOS using the command `xcode-select --install`.";
        return SearchForClangFilePath(errorMessage, filePaths);
    }

    private string SearchForClangFilePath(string errorMessage, params string[] filePaths)
    {
        var installedFilePath = string.Empty;
        foreach (var filePath in filePaths)
        {
            if (!_fileSystem.File.Exists(filePath))
            {
                continue;
            }

            installedFilePath = filePath;
        }

        if (string.IsNullOrEmpty(installedFilePath))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return installedFilePath;
    }

    private IntPtr ResolveClang(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (!NativeLibrary.TryLoad(_clangNativeLibraryFilePath, out var handle))
        {
            throw new ClangException($"Could not load libclang: {_clangNativeLibraryFilePath}");
        }

        return handle;
    }
}
