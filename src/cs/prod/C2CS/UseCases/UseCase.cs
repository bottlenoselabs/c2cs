// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;

namespace C2CS.UseCases
{
    public abstract class UseCase<TRequest, TResponse, TState>
    {
        public delegate void UseCaseStepAction(TRequest request, ref TState state);

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

        public TResponse? Execute(TRequest request)
        {
            _state = default!;

            _stopwatch.Reset();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            for (var i = 0; i < _steps.Length; i++)
            {
                ref var step = ref _steps[i];
                Console.WriteLine($"{_name}: Started step ({i + 1}/{_steps.Length}) '{step.Name}'");
                _stopwatch.Start();

                try
                {
                    step.Action(request, ref _state);
                }
                catch (Exception e)
                {
                    _stopwatch.Reset();
                    Console.Error.WriteLine($"{_name}, step ({i + 1}/{_steps.Length}) '{step.Name}': Exception");
                    Console.Error.WriteLine(e);
                    throw;
                }

                _stopwatch.Stop();
                Console.WriteLine($"{_name}: Finished step ({i + 1}/{_steps.Length}) '{step.Name}' in {_stopwatch.Elapsed.TotalMilliseconds} ms");

                _stopwatch.Reset();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }

            var result = ReturnResult(request, _state);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return result;
        }

        protected abstract TResponse ReturnResult(TRequest request, TState state);
    }
}
