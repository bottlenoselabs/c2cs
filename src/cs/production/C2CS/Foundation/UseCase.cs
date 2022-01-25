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
public abstract class UseCase<TRequest, TResponse>
    where TRequest : UseCaseRequest
    where TResponse : UseCaseResponse, new()
{
    private readonly string _name;
    private readonly Stopwatch _stepStopwatch;
    private readonly UseCaseStepMetaData[] _stepsMetaData;
    private int _stepIndex;

    protected UseCase()
    {
        _name = GetName();
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

    public TResponse Execute(TRequest request)
    {
        var response = Begin(request);

        try
        {
            Execute(request, response);
        }
        catch (Exception e)
        {
            Panic(e);

            if (Debugger.IsAttached)
            {
                throw;
            }
        }

        End(response);
        return response;
    }

    protected abstract void Execute(TRequest request, TResponse response);

    private TResponse Begin(TRequest request)
    {
        _stepStopwatch.Reset();
        GarbageCollect();
        Console.WriteLine(
            $"{_name}: Started.");
        return new TResponse();
    }

    private void End(TResponse response)
    {
        // _stepIndex = 0;
        response.WithDiagnostics(Diagnostics.GetAll());

        if (response.Status == UseCaseOutputStatus.Success)
        {
            Console.Write(
                $"{_name}: Finished successfully.");
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
                $"{_name}: Finished unsuccessfully. Review the following diagnostics for reason(s) why:");
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

    private string GetName()
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
