// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;
using C2CS.Foundation.Data;
using C2CS.Foundation.Diagnostics;
using C2CS.Foundation.UseCases.Exceptions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.UseCases;

[PublicAPI]
public abstract class UseCase<TConfiguration, TInput, TOutput>
    where TConfiguration : UseCaseConfiguration
    where TOutput : UseCaseOutput<TInput>, new()
{
    public readonly ILogger<UseCase<TConfiguration, TInput, TOutput>> Logger;

    private IDisposable? _loggerScopeStep;
    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _stepStopwatch;
    private readonly UseCaseValidator<TConfiguration, TInput> _validator;

    protected DiagnosticCollection Diagnostics { get; } = new();

    protected UseCase(
        ILogger<UseCase<TConfiguration, TInput, TOutput>> logger,
        UseCaseValidator<TConfiguration, TInput> validator)
    {
        Logger = logger;
        _stopwatch = new Stopwatch();
        _stepStopwatch = new Stopwatch();
        _validator = validator;
    }

    [DebuggerHidden]
    public TOutput Execute(TConfiguration configuration)
    {
        var output = new TOutput();

        var previousCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = configuration.WorkingDirectory ?? Environment.CurrentDirectory;

        Begin();
        try
        {
            output.Input = _validator.Validate(configuration);
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
        Logger.UseCaseStarted();
        _stopwatch.Start();
    }

    private void End(TOutput response)
    {
        _stopwatch.Stop();
        var timeSpan = _stopwatch.Elapsed;

        response.Complete(Diagnostics.GetAll());

        if (response.IsSuccess)
        {
            Logger.UseCaseSucceeded(timeSpan);
        }
        else
        {
            Logger.UseCaseFailed(timeSpan);
        }

        foreach (var diagnostic in response.Diagnostics)
        {
            diagnostic.Log(Logger);
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
        Logger.UseCaseStepStarted();
        _stepStopwatch.Start();
    }

    protected void EndStep()
    {
        _stepStopwatch.Stop();
        var timeSpan = _stepStopwatch.Elapsed;

        var isSuccess = !Diagnostics.HasFaulted;
        if (isSuccess)
        {
            Logger.UseCaseStepSucceeded(timeSpan);
        }
        else
        {
            Logger.UseCaseStepFailed(timeSpan);
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
