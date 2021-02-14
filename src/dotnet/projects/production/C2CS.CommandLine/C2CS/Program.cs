// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using ClangSharp.Interop;

namespace C2CS
{
	internal class Program
	{
		private readonly ProgramState _state;

		public Program(ProgramState state)
		{
			_state = state;
		}

		public void Execute()
		{
			var translationUnit = ParseCodeC();
			var code = GenerateCodeCSharp(translationUnit);
			WriteToFileCodeCSharp(code);
		}

		private CXTranslationUnit ParseCodeC()
		{
			var clangArgsConcat = string.Join(" ", _state.ClangArgs);
			Console.WriteLine($"Parsing C code... libclang arguments: {clangArgsConcat}");

			var codeParser = new ClangCodeParser();
			_state.Stopwatch.Restart();

			if (!codeParser.TryParseFile(_state.InputFilePath, _state.ClangArgs, out var translationUnit))
			{
				Console.Error.WriteLine($"Parsing C code... failed: {_state.InputFilePath}");
				Environment.Exit(-1);
			}

			var canContinue = true;
			var clangDiagnosticsCount = translationUnit.NumDiagnostics;
			if (clangDiagnosticsCount != 0)
			{
				Console.Error.WriteLine($"Clang diagnostics for: {_state.InputFilePath}");

				for (uint i = 0; i < clangDiagnosticsCount; ++i)
				{
					using var diagnostic = translationUnit.GetDiagnostic(i);

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
				Console.Error.WriteLine("Parsing C code... failed because one or more errors listed above.");
				Environment.Exit(-1);
			}

			_state.Stopwatch.Stop();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Console.WriteLine($"Parsing C code... finished in {_state.Stopwatch.Elapsed}");
			return translationUnit;
		}

		private string GenerateCodeCSharp(CXTranslationUnit translationUnit)
		{
			Console.WriteLine("Generating C# code...");
			_state.Stopwatch.Restart();

			var generator = new GeneratePlatformInvokeCodeUseCase(_state.LibraryName!);
			var code = generator.GenerateCode(translationUnit, _state.LibraryName!);

			_state.Stopwatch.Stop();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Console.WriteLine($"Generating C# code... finished in {_state.Stopwatch.Elapsed}");
			return code;
		}

		private void WriteToFileCodeCSharp(string code)
		{
			Console.WriteLine("Writing generated code to file...");
			_state.Stopwatch.Restart();

			File.WriteAllText(_state.OutputFilePath, code);

			_state.Stopwatch.Stop();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Console.WriteLine($"Writing generated code to file... finished in {_state.Stopwatch.Elapsed}");
			Console.WriteLine($"Output: {_state.OutputFilePath}");
		}
	}
}
