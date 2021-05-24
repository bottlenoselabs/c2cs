// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace C2CS
{
    [PublicAPI]
    public abstract class UseCase<TInput, TOutput, TState>
    	where TOutput : UseCaseOutput, new()
    {
        protected delegate void UseCaseStepAction(TInput input, ref TState state, DiagnosticsSink diagnostics);

        protected readonly struct UseCaseStep
        {
            public readonly string Name;
            public readonly UseCaseStepAction Action;

            public UseCaseStep(string name, UseCaseStepAction action)
            {
                Name = name;
                Action = action;
            }
        }

        private readonly string _name;
        private readonly Stopwatch _stopwatch;
        private readonly UseCaseStep[] _steps;
        private TState _state;
        private readonly DiagnosticsSink _diagnostics = new();

        protected UseCase(params UseCaseStep[] steps)
        {
            _name = Name();
            _stopwatch = new Stopwatch();
            _state = default!;
            _steps = steps;

            string Name()
            {
                var type = GetType()!;
                var namespaceName = type.Namespace;
                var lastNamespaceIndex = namespaceName!.LastIndexOf('.');
                var name = namespaceName![(lastNamespaceIndex + 1)..];
                return name;
            }
        }

        public TOutput Execute(TInput input)
        {
            _state = default!;
            _stopwatch.Reset();
            GarbageCollect();

            for (var i = 0; i < _steps.Length; i++)
            {
                ExecuteStep(input, i);
            }

            var output = new TOutput();
            output.WithDiagnostics(_diagnostics.GetAll());

            if (output.Status == UseCaseOutputStatus.Success)
            {
                Console.Write(
                    $"{_name}: Finished successfully.");
                if (output.Diagnostics.Length > 0)
                {
                    Console.WriteLine($" However there are {output.Diagnostics.Length} diagnostics to report. This may be indicative of unexpected results. Please review the following diagnostics:");
                }
            }
            else
            {
                Console.WriteLine(
                    $"{_name}: Finished unsuccessfully. Review the following diagnostics for reason(s) why:");
            }

            var diagnostics = output.Diagnostics;
            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine(diagnostic);
            }

            Finish(input, output, _state);
            GarbageCollect();
            return output;
        }

        private void ExecuteStep(TInput input, int i)
        {
            ref var step = ref _steps[i];
            Console.WriteLine($"{_name}: Started step ({i + 1}/{_steps.Length}) '{step.Name}'");
            _stopwatch.Start();

            try
            {
                step.Action(input, ref _state, _diagnostics);
            }
            catch (Exception e)
            {
                _stopwatch.Reset();
                var diagnostic = new PanicDiagnostic(e);
                _diagnostics.Add(diagnostic);
                if (Debugger.IsAttached)
                {
                    throw;
                }
            }

            _stopwatch.Stop();
            Console.WriteLine(
                $"{_name}: Finished step ({i + 1}/{_steps.Length}) '{step.Name}' in {_stopwatch.Elapsed.TotalMilliseconds} ms");

            _stopwatch.Reset();
            GarbageCollect();
        }

        private static void GarbageCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        protected abstract void Finish(TInput input, TOutput output, TState state);
    }
}
