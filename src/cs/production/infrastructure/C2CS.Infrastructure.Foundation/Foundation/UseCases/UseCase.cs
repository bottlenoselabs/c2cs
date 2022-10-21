// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using C2CS.Foundation.UseCases.Exceptions;
using C2CS.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.UseCases;

[PublicAPI]
public abstract class UseCase<TOptions, TInput, TOutput> : UseCase
    where TOptions : UseCaseOptions
    where TOutput : UseCaseOutput<TInput>, new()
{
    private readonly Stopwatch _stepStopwatch;
    private readonly Stopwatch _stopwatch;
    private readonly UseCaseValidator<TOptions, TInput> _validator;
    public readonly ILogger<UseCase<TOptions, TInput, TOutput>> Logger;

    private IDisposable? _loggerScopeStep;

    protected UseCase(
        ILogger<UseCase<TOptions, TInput, TOutput>> logger,
        UseCaseValidator<TOptions, TInput> validator)
        : base(logger)
    {
        Logger = logger;
        _stopwatch = new Stopwatch();
        _stepStopwatch = new Stopwatch();
        _validator = validator;
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
            output.Input = _validator.Validate(options);
            Execute(output.Input, output);
        }
        catch (UseCaseStepFailedException)
        {
            // used as a way to exit the control flow of the current use case execution and end immediately
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
        LogUseCaseStarted();
        _stopwatch.Start();
    }

    private void End(TOutput response)
    {
        _stopwatch.Stop();
        var timeSpan = _stopwatch.Elapsed;

        response.Complete(Diagnostics.GetAll());

        if (response.IsSuccess)
        {
            LogUseCaseSuccess(timeSpan);
        }
        else
        {
            LogUseCaseFailure(timeSpan);
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
        LogUseCaseStepStarted();
        _stepStopwatch.Start();
    }

    protected void EndStep()
    {
        _stepStopwatch.Stop();
        var timeSpan = _stepStopwatch.Elapsed;

        var isSuccess = !Diagnostics.HasFaulted;
        if (isSuccess)
        {
            LogUseCaseStepSuccess(timeSpan);
        }
        else
        {
            LogUseCaseStepFailure(timeSpan);
        }

        _loggerScopeStep?.Dispose();
        _loggerScopeStep = null;
        GarbageCollect();

        if (!isSuccess)
        {
            throw new UseCaseStepFailedException();
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
public partial class UseCase
{
    private readonly ILogger<UseCase> _logger;

    protected UseCase(ILogger<UseCase> logger)
    {
        _logger = logger;
    }

    [LoggerMessage(0, LogLevel.Information, "- Use case started")]
    protected partial void LogUseCaseStarted();

    [LoggerMessage(1, LogLevel.Information, @"- Use case success in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogUseCaseSuccess(TimeSpan elapsed);

    [LoggerMessage(2, LogLevel.Information, @"- Use case failed in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogUseCaseFailure(TimeSpan elapsed);

    [LoggerMessage(3, LogLevel.Information, "- Use case step started")]
    protected partial void LogUseCaseStepStarted();

    [LoggerMessage(4, LogLevel.Information, @"- Use case step success in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogUseCaseStepSuccess(TimeSpan elapsed);

    [LoggerMessage(5, LogLevel.Information, @"- Use case step failed in {Elapsed:s\\.ffff} seconds")]
    protected partial void LogUseCaseStepFailure(TimeSpan elapsed);
}
