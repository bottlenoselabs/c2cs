// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS;

[PublicAPI]
public abstract class UseCase<TRequest, TInput, TResponse>
    where TRequest : UseCaseRequest
    where TResponse : UseCaseResponse, new()
{
    public readonly ILogger Logger;
    private IDisposable? _loggerScope;
    private IDisposable? _loggerScopeStep;
    private readonly string _name;
    private readonly Stopwatch _stopwatch;
    private readonly Stopwatch _stepStopwatch;
    private readonly UseCaseValidator<TRequest, TInput> _validator;

    protected UseCase(string name, ILogger logger, UseCaseValidator<TRequest, TInput> validator)
    {
        Logger = logger;
        _name = name;
        _stopwatch = new Stopwatch();
        _stepStopwatch = new Stopwatch();
        _validator = validator;
    }

    protected DiagnosticsSink Diagnostics { get; } = new();

    [DebuggerHidden]
    public TResponse Execute(TRequest? request)
    {
        if (request == null)
        {
            return new TResponse
            {
                IsSuccessful = false
            };
        }

        var previousCurrentDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory;

        Begin();
        TResponse? response = null;
        try
        {
            var input = _validator.Validate(request);
            response = Execute(input);
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

        response ??= new TResponse();
        End(response);
        return response;
    }

    protected abstract TResponse? Execute(TInput input);

    private void Begin()
    {
        _loggerScope = Logger.BeginScope(_name);
        _stopwatch.Reset();
        _stepStopwatch.Reset();
        GarbageCollect();
        Logger.UseCaseStarted();
        _stopwatch.Start();
    }

    private void End(TResponse response)
    {
        _stopwatch.Stop();
        var timeSpan = _stopwatch.Elapsed;

        response.WithDiagnostics(Diagnostics.GetAll());

        if (response.IsSuccessful)
        {
            Logger.UseCaseSucceeded(timeSpan);
        }
        else
        {
            Logger.UseCaseFailed(timeSpan);
        }

        LogDiagnostics(response.Diagnostics);

        _loggerScope?.Dispose();
        _loggerScope = null;
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
        Logger.UseCaseStepFinished(timeSpan);
        _loggerScopeStep?.Dispose();
        _loggerScopeStep = null;
        GarbageCollect();

        if (!Diagnostics.HasError)
        {
            return;
        }

        var diagnostics = Diagnostics.GetAll();
        throw new UseCaseException(diagnostics);
    }

    private void LogDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            diagnostic.Log(Logger);
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
