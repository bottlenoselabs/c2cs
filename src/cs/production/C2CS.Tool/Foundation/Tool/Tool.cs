// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.Tool;

/// <summary>
///     A computation component.
/// </summary>
/// <typeparam name="TUnsanitizedInput">The un-sanitized input.</typeparam>
/// <typeparam name="TInput">The sanitized input.</typeparam>
/// <typeparam name="TOutput">The output.</typeparam>
[PublicAPI]
public abstract class Tool<TUnsanitizedInput, TInput, TOutput> : Tool
    where TUnsanitizedInput : ToolUnsanitizedInput
    where TOutput : ToolOutput<TInput>, new()
{
    private readonly Stopwatch _stepStopwatch;
    private readonly Stopwatch _stopwatch;
    private readonly ToolInputSanitizer<TUnsanitizedInput, TInput> _inputSanitizer;
    private readonly ILogger<Tool<TUnsanitizedInput, TInput, TOutput>> _logger;

    private IDisposable? _loggerScopeStep;
    private readonly IFileSystem _fileSystem;

    protected Tool(
        ILogger<Tool<TUnsanitizedInput, TInput, TOutput>> logger,
        ToolInputSanitizer<TUnsanitizedInput, TInput> inputSanitizer,
        IFileSystem fileSystem)
        : base(logger)
    {
        _logger = logger;
        _stopwatch = new Stopwatch();
        _stepStopwatch = new Stopwatch();
        _inputSanitizer = inputSanitizer;
        _fileSystem = fileSystem;
    }

    protected DiagnosticCollection Diagnostics { get; } = new();

    public TOutput Run(string configurationFilePath)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(configurationFilePath);
        if (!_fileSystem.File.Exists(fullFilePath))
        {
            throw new ToolInputSanitizationException($"The tool configuration file '{fullFilePath}' does not exist.");
        }

        var fileContents = _fileSystem.File.ReadAllText(fullFilePath);
        if (string.IsNullOrEmpty(fileContents))
        {
            throw new ToolInputSanitizationException($"The extract options file '{fullFilePath}' is empty.");
        }

        var serializerOptions = new JsonSerializerOptions
        {
            AllowTrailingCommas = true
        };
        var unsanitizedInput = JsonSerializer.Deserialize<TUnsanitizedInput>(fileContents, serializerOptions);
        if (unsanitizedInput == null)
        {
            throw new ToolInputSanitizationException("Failed to deserialize the tool configuration file path.");
        }

        var workingDirectory = _fileSystem.Path.GetDirectoryName(fullFilePath)!;
        var output = Run(unsanitizedInput, workingDirectory);
        return output;
    }

    public TOutput Run(TUnsanitizedInput unsanitizedInput)
    {
        var output = Run(unsanitizedInput, Environment.CurrentDirectory);
        return output;
    }

    protected abstract void Execute(TInput input, TOutput output);

    protected void BeginStep(string stepName)
    {
        _stepStopwatch.Reset();
        _loggerScopeStep = _logger.BeginScope(stepName);
        GarbageCollect();
        LogStepStarted();
        _stepStopwatch.Start();
    }

    protected void EndStep()
    {
        _stepStopwatch.Stop();
        var timeSpan = _stepStopwatch.Elapsed;

        var isSuccess = !Diagnostics.HasFaulted;
        if (isSuccess)
        {
            LogStepSuccess(timeSpan);
        }
        else
        {
            LogStepFailure(timeSpan);
        }

        _loggerScopeStep?.Dispose();
        _loggerScopeStep = null;
        GarbageCollect();
    }

    [DebuggerHidden]
    private TOutput Run(TUnsanitizedInput unsanitizedInput, string? workingFileDirectory)
    {
        var output = new TOutput();

        var previousCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = workingFileDirectory ?? Environment.CurrentDirectory;

        Begin();
        try
        {
            output.Input = _inputSanitizer.Sanitize(unsanitizedInput);
            Execute(output.Input, output);
        }
        catch (Exception e)
        {
            if (Debugger.IsAttached)
            {
                throw;
            }
            else
            {
                Panic(e);
            }
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrentDirectory;
        }

        End(output);
        return output;
    }

    private void Begin()
    {
        _stopwatch.Reset();
        _stepStopwatch.Reset();
        GarbageCollect();
        LogStarted();
        _stopwatch.Start();
    }

    private void End(TOutput response)
    {
        _stopwatch.Stop();
        var timeSpan = _stopwatch.Elapsed;

        response.Complete(Diagnostics.GetAll());

        if (response.IsSuccess)
        {
            LogSuccess(timeSpan);
        }
        else
        {
            LogFailure(timeSpan);
        }

        foreach (var diagnostic in response.Diagnostics)
        {
            var name = diagnostic.GetName();
            var message = diagnostic.Message;

            var logLevel = diagnostic.Severity switch
            {
                DiagnosticSeverity.Information => LogLevel.Information,
                DiagnosticSeverity.Warning => LogLevel.Warning,
                DiagnosticSeverity.Error => LogLevel.Error,
                DiagnosticSeverity.Panic => LogLevel.Critical,
                _ => LogLevel.Information
            };

#pragma warning disable CA1848
#pragma warning disable CA2254
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            _logger.Log(logLevel, $"- {name} {message}");
#pragma warning restore CA2254
#pragma warning restore CA1848
        }

        GarbageCollect();
    }

    private void Panic(Exception e)
    {
        var diagnostic = new DiagnosticPanic(e);
        Diagnostics.Add(diagnostic);
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public partial class Tool
{
    private readonly ILogger<Tool> _logger;

    protected Tool(ILogger<Tool> logger)
    {
        _logger = logger;
    }

    [LoggerMessage(0, LogLevel.Information, "- Started")]
    protected partial void LogStarted();

    [LoggerMessage(1, LogLevel.Information, @"- Success in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogSuccess(TimeSpan elapsed);

    [LoggerMessage(2, LogLevel.Information, @"- Failed in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogFailure(TimeSpan elapsed);

    [LoggerMessage(3, LogLevel.Information, "- Step started")]
    protected partial void LogStepStarted();

    [LoggerMessage(4, LogLevel.Information, @"- Step success in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogStepSuccess(TimeSpan elapsed);

    [LoggerMessage(5, LogLevel.Information, @"- Step failed in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogStepFailure(TimeSpan elapsed);
}
