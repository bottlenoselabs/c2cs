// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.ComponentModel;
using System.Diagnostics;
using C2CS.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.Executors;

/// <summary>
///     A computation component.
/// </summary>
/// <typeparam name="TOptions">The un-sanitized input.</typeparam>
/// <typeparam name="TInput">The sanitized input.</typeparam>
/// <typeparam name="TOutput">The output.</typeparam>
[PublicAPI]
public abstract class Executor<TOptions, TInput, TOutput> : Executor
    where TOptions : ExecutorOptions
    where TOutput : ExecutorOutput<TInput>, new()
{
    private readonly Stopwatch _stepStopwatch;
    private readonly Stopwatch _stopwatch;
    private readonly ExecutorInputValidator<TOptions, TInput> _inputValidator;
    public readonly ILogger<Executor<TOptions, TInput, TOutput>> Logger;

    private IDisposable? _loggerScopeStep;

    protected Executor(
        ILogger<Executor<TOptions, TInput, TOutput>> logger,
        ExecutorInputValidator<TOptions, TInput> inputValidator)
        : base(logger)
    {
        Logger = logger;
        _stopwatch = new Stopwatch();
        _stepStopwatch = new Stopwatch();
        _inputValidator = inputValidator;
    }

    protected DiagnosticCollection Diagnostics { get; } = new();

    [DebuggerHidden]
    public TOutput Execute(TOptions options)
    {
        var output = new TOutput();

        var previousCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = options.WorkingFileDirectory ?? Environment.CurrentDirectory;

        Begin();
        try
        {
            output.Input = _inputValidator.Validate(options);
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

    protected abstract void Execute(TInput input, TOutput output);

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
            Logger.Log(logLevel, $"- {name} {message}");
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

    protected void BeginStep(string stepName)
    {
        _stepStopwatch.Reset();
        _loggerScopeStep = Logger.BeginScope(stepName);
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

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public partial class Executor
{
    private readonly ILogger<Executor> _logger;

    protected Executor(ILogger<Executor> logger)
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
