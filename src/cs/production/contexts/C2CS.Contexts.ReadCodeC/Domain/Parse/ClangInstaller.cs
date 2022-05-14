// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse;

public sealed partial class ClangInstaller
{
    private bool _isInstalled;
    private readonly object _lock = new();

    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ClangInstaller> _logger;
    private string _clangNativeLibraryFilePath = string.Empty;

    public ClangInstaller(
        ILogger<ClangInstaller> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public bool Install(NativeOperatingSystem operatingSystem)
    {
        try
        {
            lock (_lock)
            {
                if (_isInstalled)
                {
                    LogAlreadyInstalled(_clangNativeLibraryFilePath);
                    return true;
                }

                var filePath = GetClangFilePath(operatingSystem);
                if (string.IsNullOrEmpty(filePath))
                {
                    LogFailure();
                    return false;
                }

                _clangNativeLibraryFilePath = filePath;
                LoadFunctions(filePath);
                LogSuccessInstalled(filePath);
                _isInstalled = true;
                return true;
            }
        }
        catch (Exception e)
        {
            LogException(e);
            return false;
        }
    }

    private void LoadFunctions(string libraryFilePath)
    {
        var previousCurrentDirectory = Environment.CurrentDirectory;
        var installDirectoryPath = Path.GetDirectoryName(libraryFilePath);
        Environment.CurrentDirectory = installDirectoryPath;

        var type = typeof(bottlenoselabs.clang);
        const BindingFlags bindingAttributes = BindingFlags.DeclaredOnly |
                                               BindingFlags.NonPublic |
                                               BindingFlags.Public |
                                               BindingFlags.Instance |
                                               BindingFlags.Static;
        var methods = type.GetMethods(bindingAttributes);
        foreach (var method in methods)
        {
            if (method.ContainsGenericParameters)
            {
                continue;
            }

            if ((method.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract)
            {
                continue;
            }

            RuntimeHelpers.PrepareMethod(method.MethodHandle);
        }

        Environment.CurrentDirectory = previousCurrentDirectory;
    }

    private string GetClangFilePath(NativeOperatingSystem operatingSystem)
    {
        var result = operatingSystem switch
        {
            NativeOperatingSystem.Windows => GetClangFilePathWindows(),
            NativeOperatingSystem.Linux => GetClangFilePathLinux(),
            NativeOperatingSystem.macOS => GetClangFilePathMacOs(),
            _ => string.Empty
        };

        return result;
    }

    private string GetClangFilePathWindows()
    {
        var filePaths = new[]
        {
            // ReSharper disable StringLiteralTypo
            Path.Combine(AppContext.BaseDirectory, "libclang.dll"),
            Path.Combine(AppContext.BaseDirectory, "clang.dll"),
            @"C:\Program Files\LLVM\bin\libclang.dll" // choco install llvm
            // ReSharper restore StringLiteralTypo
        };

        const string errorMessage = "`libclang.dll` or `clang.dll` is missing. Please put a `libclang.dll` or `clang.dll` file next to this application or install Clang for Windows. To install Clang for Windows using Chocolatey, use the command `choco install llvm`.";
        return SearchForClangFilePath(errorMessage, filePaths);
    }

    private string GetClangFilePathLinux()
    {
        var filePaths = new[]
        {
            // ReSharper disable StringLiteralTypo
            Path.Combine(AppContext.BaseDirectory, "libclang.so"),
            "/usr/lib/llvm-10/lib/libclang.so.1" // apt-get install clang
            // ReSharper restore StringLiteralTypo
        };

        const string errorMessage = "`libclang.so`is missing. Please put a `libclang.so` file next to this application or install Clang for Linux. To install Clang for Debian-based linux distributions, use the command `apt-get update && apt-get install clang`.";
        return SearchForClangFilePath(errorMessage, filePaths);
    }

    private string GetClangFilePathMacOs()
    {
        var filePaths = new[]
        {
            // ReSharper disable StringLiteralTypo
            Path.Combine(AppContext.BaseDirectory, "libclang.dylib"),
            "/Applications/Xcode.app/Contents/Developer/Toolchains/XcodeDefault.xctoolchain/usr/lib/libclang.dylib", // XCode
            "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib" // xcode-select --install
            // ReSharper restore StringLiteralTypo
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
            break;
        }

        if (string.IsNullOrEmpty(installedFilePath))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return installedFilePath;
    }

    // private IntPtr ResolveClang(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    // {
    //     if (!NativeLibrary.TryLoad(_clangNativeLibraryFilePath, out var handle))
    //     {
    //         throw new ClangException($"Could not load libclang: {_clangNativeLibraryFilePath}");
    //     }
    //
    //     return handle;
    // }

    [LoggerMessage(0, LogLevel.Critical, "- Exception")]
    private partial void LogException(Exception exception);

    [LoggerMessage(1, LogLevel.Error, "- Failure, could not determine path to libclang")]
    private partial void LogFailure();

    [LoggerMessage(2, LogLevel.Debug, "- Success, installed, file path: {FilePath}")]
    private partial void LogSuccessInstalled(string filePath);

    [LoggerMessage(3, LogLevel.Debug, "- Success, already installed, file path: {FilePath}")]
    private partial void LogAlreadyInstalled(string filePath);
}
