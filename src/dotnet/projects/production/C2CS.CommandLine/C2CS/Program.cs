// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClangSharp;
using ClangSharp.Interop;

namespace C2CS
{
    internal class Program
    {
        private State _state;

        public struct State
        {
            public readonly string InputFilePath;
            public string OutputFilePath;
            public readonly bool IsUnattended;
            public string? LibraryName;
            public readonly IEnumerable<string>? IncludeDirectories;
            public readonly IEnumerable<string>? DefineMacros;
            public readonly IEnumerable<string>? AdditionalArgs;

            public State(
                string inputFilePath,
                string outputFilePath,
                bool isUnattended,
                string? libraryName,
                IEnumerable<string>? includeDirectories,
                IEnumerable<string>? defineMacros,
                IEnumerable<string>? additionalArgs)
            {
                InputFilePath = inputFilePath;
                OutputFilePath = outputFilePath;
                IsUnattended = isUnattended;
                LibraryName = libraryName;
                IncludeDirectories = includeDirectories;
                DefineMacros = defineMacros;
                AdditionalArgs = additionalArgs;
            }
        }

        public Program(State state)
        {
            _state = state;
        }

        public void Execute()
        {
            CheckInputFile();
            CheckOutputFile();
            CheckLibraryName();
            var translationUnit = ParseCodeC();
            var code = GenerateCodeCSharp(translationUnit);
            WriteToFileCodeCSharp(code);
        }

        private void CheckInputFile()
        {
            if (File.Exists(_state.InputFilePath))
            {
                return;
            }

            Console.WriteLine($"File doesn't exist: {_state.InputFilePath}");
            Environment.Exit(-1);
        }

        private void CheckOutputFile()
        {
            _state.OutputFilePath = Path.GetFullPath(_state.OutputFilePath);

            if (!File.Exists(_state.OutputFilePath))
            {
                return;
            }

            if (!_state.IsUnattended)
            {
                Console.WriteLine($"The file already exists: {_state.OutputFilePath}");
                Console.WriteLine("Do you want to overwrite it? [Y/N]");

                var consoleKeyInfo = Console.ReadKey();
                if (consoleKeyInfo.Key == ConsoleKey.Y)
                {
                    File.Delete(_state.OutputFilePath);
                }

                Console.WriteLine();
            }
            else
            {
                File.Delete(_state.OutputFilePath);
            }
        }

        private void CheckLibraryName()
        {
            if (string.IsNullOrEmpty(_state.LibraryName))
            {
                _state.LibraryName = Path.GetFileNameWithoutExtension(_state.InputFilePath);
            }
        }

        private TranslationUnit ParseCodeC()
        {
            var commandLineArgs = new[]
            {
                "--language=c",
                "--std=c11",
                "-Wno-pragma-once-outside-header"
            };

            // TODO: Find and use default include directories
            // macOS
            // includeDirectories.Add("/Applications/Xcode.app/Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk/usr/include");
            // includeDirectories.Add("/usr/local/Cellar/llvm/10.0.0_3/lib/clang/10.0.0/include");
            // Windows
            // includeDirectories.Add("C:\Program Files (x86)\Windows Kits\10\Include\10.0.18362.0\um");
            // includeDirectories.Add("C:\Program Files (x86)\Windows Kits\10\Include\10.0.18362.0\shared");
            // includeDirectories.Add("C:\Program Files (x86)\Windows Kits\10\Include\10.0.18362.0\ucrt");

            if (_state.IncludeDirectories != null)
            {
                commandLineArgs = commandLineArgs.Concat(_state.IncludeDirectories.Select(x => "--include-directory=" + x)).ToArray();
            }

            if (_state.DefineMacros != null)
            {
                commandLineArgs = commandLineArgs.Concat(_state.DefineMacros.Select(x => "--define-macro=" + x)).ToArray();
            }

            if (_state.AdditionalArgs != null)
            {
                commandLineArgs = commandLineArgs.Concat(_state.AdditionalArgs).ToArray();
            }

            Console.WriteLine("Parsing C code... libclang arguments: " + string.Join(" ", commandLineArgs));

            var codeParser = new CodeCParser();

            if (!codeParser.TryParseFile(_state.InputFilePath, commandLineArgs, out var translationUnit))
            {
                Console.WriteLine($"Parsing C code... failed: {_state.InputFilePath}");
                Environment.Exit(-1);
            }

            var canContinue = true;
            var clangDiagnosticsCount = translationUnit.Handle.NumDiagnostics;
            if (clangDiagnosticsCount != 0)
            {
                Console.WriteLine($"Clang diagnostics for: {_state.InputFilePath}");

                for (uint i = 0; i < clangDiagnosticsCount; ++i)
                {
                    using var diagnostic = translationUnit.Handle.GetDiagnostic(i);

                    Console.Write("    ");
                    Console.WriteLine(diagnostic.Format(CXDiagnostic.DefaultDisplayOptions).ToString());

                    if (diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Error ||
                        diagnostic.Severity == CXDiagnosticSeverity.CXDiagnostic_Fatal)
                    {
                        canContinue = false;
                    }
                }
            }

            if (!canContinue)
            {
                Console.WriteLine("Parsing C code... failed because one or more errors listed above.");
                Environment.Exit(-1);
            }

            Console.WriteLine("Parsing C code... finished");
            return translationUnit;
        }

        private string GenerateCodeCSharp(TranslationUnit translationUnit)
        {
            Console.WriteLine("Generating C# code...");

            var generator = new GeneratePlatformInvokeCodeUseCase(_state.LibraryName!);
            var code = generator.GenerateCode(translationUnit, _state.LibraryName!, _state.IncludeDirectories);

            Console.WriteLine("Generating C# code... finished");
            return code;
        }

        private void WriteToFileCodeCSharp(string code)
        {
            Console.WriteLine("Writing generated code to file...");

            File.WriteAllText(_state.OutputFilePath, code);

            Console.WriteLine("Writing generated code to file... finished");
            Console.WriteLine($"Output: {_state.OutputFilePath}");
        }
    }
}
