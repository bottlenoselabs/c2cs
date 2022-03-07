// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace C2CS;

[PublicAPI]
public abstract class UseCaseHandler<TInput, TOutput>
    where TInput : UseCaseInput<TOutput>
    where TOutput : UseCaseOutput, new()
{
    private readonly string _useCaseName;
    private readonly Stopwatch _stepStopwatch;
    private readonly UseCaseStepMetaData[] _stepsMetaData;
    private int _stepIndex;

    protected UseCaseHandler()
    {
        _useCaseName = GetUseCaseName();
        _stepStopwatch = new Stopwatch();
        _stepsMetaData = GetStepsMetaData().ToArray();
    }

    private IEnumerable<UseCaseStepMetaData> GetStepsMetaData()
    {
        var methods = GetType().GetRuntimeMethods();
        var useCaseStepAttributes = methods
            .Select(x => x.GetCustomAttribute<UseCaseStepAttribute>())
            .Where(x => x != null)
            .Cast<UseCaseStepAttribute>()
            .ToArray();

        foreach (var attribute in useCaseStepAttributes)
        {
            var useCaseStepMetaData = new UseCaseStepMetaData
            {
                Name = attribute.StepName
            };

            yield return useCaseStepMetaData;
        }
    }

    protected DiagnosticsSink Diagnostics { get; } = new();

    [DebuggerHidden]
    public TOutput Execute(TInput input)
    {
        var response = Begin(input);

        try
        {
            Execute(input, response);
        }
        catch (Exception e)
        {
            if (Debugger.IsAttached)
            {
                throw;
            }

            Panic(e);
        }

        End(response);
        return response;
    }

    protected abstract void Execute(TInput input, TOutput output);

    private TOutput Begin(TInput request)
    {
        _stepStopwatch.Reset();
        GarbageCollect();
        Console.WriteLine(
            $"{_useCaseName}: Started.");
        return new TOutput();
    }

    private void End(TOutput response)
    {
        // _stepIndex = 0;
        response.WithDiagnostics(Diagnostics.GetAll());

        if (response.Status == UseCaseOutputStatus.Success)
        {
            Console.Write(
                $"{_useCaseName}: Finished successfully.");
            if (response.Diagnostics.Length > 0)
            {
                Console.WriteLine(
                    $" However there are {response.Diagnostics.Length} diagnostics to report. This may be indicative of unexpected results. Please review the following diagnostics:");
            }
            else
            {
                Console.WriteLine();
            }
        }
        else
        {
            Console.WriteLine(
                $"{_useCaseName}: Finished unsuccessfully. Review the following diagnostics for reason(s) why:");
        }

        PrintDiagnostics(response.Diagnostics);
        GarbageCollect();
    }

    private void Panic(Exception e)
    {
        var diagnostic = new DiagnosticPanic(e);
        Diagnostics.Add(diagnostic);
    }

    protected void BeginStep()
    {
        var stepMetaData = _stepsMetaData[_stepIndex++];
        var stepCount = _stepsMetaData.Length;
        Console.WriteLine($"\tStarted step ({_stepIndex}/{stepCount}) '{stepMetaData.Name}'");
        _stepStopwatch.Start();
        GarbageCollect();
    }

    protected void EndStep()
    {
        var stepMetaData = _stepsMetaData[_stepIndex - 1];
        var stepCount = _stepsMetaData.Length;
        _stepStopwatch.Stop();
        Console.WriteLine(
            $"\tFinished step ({_stepIndex}/{stepCount}) '{stepMetaData.Name}' in {_stepStopwatch.Elapsed.TotalMilliseconds} ms");
        _stepStopwatch.Reset();
        GarbageCollect();
    }

    private string GetUseCaseName()
    {
        var type = GetType()!;
        var namespaceName = type.Namespace;
        var lastNamespaceIndex = namespaceName!.LastIndexOf('.');
        var name = namespaceName![(lastNamespaceIndex + 1)..];
        return name;
    }

    private static void PrintDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine(diagnostic);
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
