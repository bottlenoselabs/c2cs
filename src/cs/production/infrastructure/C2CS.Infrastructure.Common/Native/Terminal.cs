// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using JetBrains.Annotations;

// ReSharper disable once EmptyNamespace
namespace C2CS;

[PublicAPI]
public static class Terminal
{
#pragma warning disable CA1034
    [PublicAPI]
    public class ShellOutput
#pragma warning restore CA1034
    {
        public int ExitCode { get; set; }

        public string Output { get; set; } = string.Empty;
    }

    public static ShellOutput ExecuteShell(
        this string command,
        string? workingDirectory = null,
        string? fileName = null,
        bool windowsUsePowerShell = true)
    {
        using var process = CreateShellProcess(command, workingDirectory, fileName, windowsUsePowerShell);
        var stringBuilder = new StringBuilder();
        using var stringWriter = new StringWriter(stringBuilder);
        var spinLock = default(SpinLock);

        process.OutputDataReceived += OnProcessOnErrorDataReceived;
        process.ErrorDataReceived += OnProcessOnErrorDataReceived;

        void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            var gotLock = false;
            spinLock.Enter(ref gotLock);
            // ReSharper disable once AccessToDisposedClosure
            stringWriter.WriteLine(args.Data);
            if (gotLock)
            {
                spinLock.Exit();
            }
        }

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
        process.WaitForExit();

        var outputString = stringBuilder.ToString().Trim('\n', '\r');

        var result = new ShellOutput
        {
            ExitCode = process.ExitCode,
            Output = outputString
        };
        return result;
    }

    private static Process CreateShellProcess(
        string command, string? workingDirectory, string? fileName, bool windowsUsePowerShell = true)
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

#pragma warning disable CA1307
            var escapedArgs = command.Replace("\"", "\\\"");
#pragma warning restore CA1307
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
}
