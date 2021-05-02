// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;
using System.IO;

namespace C2CS.Tools
{
    public static class Shell
    {
        public static string Bash(this string command, string? workingDirectory = null)
        {
            if (workingDirectory != null && !Directory.Exists(workingDirectory))
            {
                return string.Empty;
            }

            var escapedArgs = command.Replace("\"", "\\\"");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                CreateNoWindow = true
            };

            var process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();
            var result = process.StandardOutput.ReadToEnd().TrimEnd('\n', '\r');
            process.WaitForExit();

            return result;
        }
    }
}
