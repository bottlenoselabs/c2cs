// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.Annotations;

// ReSharper disable once EmptyNamespace
namespace C2CS;

[PublicAPI]
public static class Terminal
{
    public static string ShellCaptureOutput(
        this string command,
        string? workingDirectory = null,
        string? fileName = null,
        bool windowsUsePowerShell = true)
    {
        using var process = CreateShellProcess(command, workingDirectory, fileName, windowsUsePowerShell);

        process.Start();
        process.WaitForExit();

        var rawResult = process.StandardOutput.ReadToEnd();
        var result = rawResult.Trim('\n', '\r');
        return result;
    }

    public static bool Shell(
        this string command,
        string? workingDirectory,
        string? fileName = null,
        bool windowsUsePowerShell = true)
    {
        using var process = CreateShellProcess(command, workingDirectory, fileName, windowsUsePowerShell);

        var outputStrings = new List<string>();

        process.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                outputStrings.Add(args.Data);
            }
        };
        process.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                outputStrings.Add(args.Data);
            }
        };

        process.Start();

        process.BeginOutputReadLine();
        process.WaitForExit();

        Console.WriteLine(string.Join('\n', outputStrings));
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    private static Process CreateShellProcess(string command, string? workingDirectory, string? fileName, bool windowsUsePowerShell)
    {
        if (workingDirectory != null && !Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException(workingDirectory);
        }

        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(fileName))
        {
            processStartInfo.FileName = fileName;
            processStartInfo.Arguments = command;
        }
        else
        {
            var platform = RuntimePlatform.Host;
            if (platform.OperatingSystem == RuntimeOperatingSystem.Windows)
            {
                if (windowsUsePowerShell)
                {
                    processStartInfo.FileName = "powershell.exe";
                }
                else
                {
                    var bashFilePath = WindowsBashFilePath();
                    if (string.IsNullOrEmpty(bashFilePath))
                    {
                        throw new FileNotFoundException(
                            "Failed to find a `git-bash.exe` or `bash.exe` on Windows. Did you forget to install Git Bash and/or add it to your PATH?");
                    }

                    processStartInfo.FileName = bashFilePath;
                }
            }
            else
            {
                processStartInfo.FileName = "bash";
            }

            var escapedArgs = command.Replace("\"", "\\\"", StringComparison.InvariantCulture);
            processStartInfo.Arguments = $"-c \"{escapedArgs}\"";
        }

        var process = new Process
        {
            StartInfo = processStartInfo
        };
        return process;
    }

    private static string WindowsBashFilePath()
    {
        var candidateBashFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin", "bash.exe");
        if (File.Exists(candidateBashFilePath))
        {
            return candidateBashFilePath;
        }

        candidateBashFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin", "bash.exe");
        if (File.Exists(candidateBashFilePath))
        {
            return candidateBashFilePath;
        }

        var environmentVariablePath = Environment.GetEnvironmentVariable("PATH");
        var searchDirectories = environmentVariablePath?.Split(';') ?? Array.Empty<string>();

        foreach (var searchDirectory in searchDirectories)
        {
            candidateBashFilePath = Path.Combine(searchDirectory, "bash.exe");
            if (File.Exists(candidateBashFilePath))
            {
                return candidateBashFilePath;
            }
        }

        return string.Empty;
    }

    public static bool CMake(string rootDirectory, string cMakeDirectoryPath, string libraryOutputDirectoryPath)
    {
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        if (!Directory.Exists(cMakeDirectoryPath))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        var libraryOutputDirectoryPathNormalized = libraryOutputDirectoryPath.Replace("\\", "/", StringComparison.InvariantCulture);
        var isSuccess = $"cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_LIBRARY_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE={libraryOutputDirectoryPathNormalized}"
            .Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (!isSuccess)
        {
            return false;
        }

        isSuccess = "cmake --build cmake-build-release --config Release"
            .Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (!isSuccess)
        {
            return false;
        }

        var outputDirectoryPath = Path.Combine(cMakeDirectoryPath, "lib");
        if (!Directory.Exists(outputDirectoryPath))
        {
            return false;
        }

        var runtimePlatform = RuntimePlatform.Host;
        var libraryFileNameExtension = RuntimePlatform.LibraryFileNameExtension(runtimePlatform.OperatingSystem);
        var outputFilePaths = Directory.EnumerateFiles(
            outputDirectoryPath, $"*{libraryFileNameExtension}", SearchOption.AllDirectories);
        foreach (var outputFilePath in outputFilePaths)
        {
            var targetFilePath = outputFilePath.Replace(
                    outputDirectoryPath, libraryOutputDirectoryPath, StringComparison.InvariantCulture);
            var targetFileName = Path.GetFileName(targetFilePath);

            if (runtimePlatform.OperatingSystem == RuntimeOperatingSystem.Windows)
            {
                if (targetFileName.StartsWith("lib", StringComparison.InvariantCulture))
                {
                    targetFileName = targetFileName[3..];
                }
            }

            var targetFileDirectoryPath = Path.GetDirectoryName(targetFilePath)!;
            targetFilePath = Path.Combine(targetFileDirectoryPath, targetFileName);
            if (!Directory.Exists(targetFileDirectoryPath))
            {
                Directory.CreateDirectory(targetFileDirectoryPath);
            }

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            File.Copy(outputFilePath, targetFilePath);
        }

        Directory.Delete(outputDirectoryPath, true);
        Directory.Delete($"{cMakeDirectoryPath}/cmake-build-release", true);

        return true;
    }

    public static string DotNetPath()
    {
        Version version = new(0, 0, 0, 0);
        var path = string.Empty;
        var runtimesString = "dotnet --list-runtimes".ShellCaptureOutput();
        var runtimeStrings =
            runtimesString.Split(new[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var fullRuntimeString in runtimeStrings)
        {
            var parse = fullRuntimeString.Split(" [", StringSplitOptions.RemoveEmptyEntries);
            var runtimeString = parse[0];
            var runtimeStringParse = runtimeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var runtimeName = runtimeStringParse[0];
            var runtimeVersionString = runtimeStringParse[1];
            var runtimePath = parse[1].Trim(']');

            if (!runtimeName.Contains("Microsoft.NETCore.App", StringComparison.InvariantCulture))
            {
                continue;
            }

            var versionCharIndexHyphen = runtimeVersionString.IndexOf('-', StringComparison.InvariantCulture);
            if (versionCharIndexHyphen != -1)
            {
                // can possibly happen for release candidates of .NET
                runtimeVersionString = runtimeVersionString[..versionCharIndexHyphen];
            }

            var candidateVersion = Version.Parse(runtimeVersionString);
            if (candidateVersion <= version)
            {
                continue;
            }

            version = candidateVersion;
            path = Path.Combine(runtimePath, runtimeVersionString);
        }

        return path;
    }
}
