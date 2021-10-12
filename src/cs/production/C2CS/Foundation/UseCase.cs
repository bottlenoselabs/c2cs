// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using JetBrains.Annotations;

namespace C2CS
{
    [PublicAPI]
    public abstract class UseCase<TRequest, TResponse>
        where TRequest : UseCaseRequest
        where TResponse : UseCaseResponse, new()
    {
        private readonly string _name;
        private readonly Stopwatch _stepStopwatch;
        private int _stepIndex;
        private int _stepCount;

        protected DiagnosticsSink Diagnostics { get; } = new();

        protected UseCase()
        {
            _name = GetName();
            _stepStopwatch = new Stopwatch();
        }

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
            _stepIndex = 0;
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

        protected void TotalSteps(int value)
        {
            _stepCount = value;
        }

        protected void Step<TInput>(string stepName, TInput input, Action<TInput> action)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            action(input);
            EndStep(_stepIndex, stepName);
        }

        protected void Step<TInput1, TInput2>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            Action<TInput1, TInput2> action)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            action(input1, input2);
            EndStep(_stepIndex, stepName);
        }

        protected TOutput Step<TInput, TOutput>(
            string stepName,
            TInput input,
            Func<TInput, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input);
            EndStep(_stepIndex, stepName);
            return output;
        }

        protected TOutput Step<TInput1, TInput2, TOutput>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            Func<TInput1, TInput2, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input1, input2);
            EndStep(_stepIndex, stepName);
            return output;
        }

        protected TOutput Step<TInput1, TInput2, TInput3, TOutput>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            Func<TInput1, TInput2, TInput3, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input1, input2, input3);
            EndStep(_stepIndex, stepName);
            return output;
        }

        protected TOutput Step<TInput1, TInput2, TInput3, TInput4, TOutput>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            Func<TInput1, TInput2, TInput3, TInput4, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input1, input2, input3, input4);
            EndStep(_stepIndex, stepName);
            return output;
        }

        protected TOutput Step<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            TInput5 input5,
            Func<TInput1, TInput2, TInput3, TInput4, TInput5, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input1, input2, input3, input4, input5);
            EndStep(_stepIndex, stepName);
            return output;
        }

        protected TOutput Step<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput>(
            string stepName,
            TInput1 input1,
            TInput2 input2,
            TInput3 input3,
            TInput4 input4,
            TInput5 input5,
            TInput6 input6,
            Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TOutput> func)
        {
            _stepIndex++;
            BeginStep(_stepIndex, stepName);
            var output = func(input1, input2, input3, input4, input5, input6);
            EndStep(_stepIndex, stepName);
            return output;
        }

		protected TOutput Step<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput>(
			string stepName,
			TInput1 input1,
			TInput2 input2,
			TInput3 input3,
			TInput4 input4,
			TInput5 input5,
			TInput6 input6,
			TInput7 input7,
			Func<TInput1, TInput2, TInput3, TInput4, TInput5, TInput6, TInput7, TOutput> func)
		{
			_stepIndex++;
			BeginStep(_stepIndex, stepName);
			var output = func(input1, input2, input3, input4, input5, input6, input7);
			EndStep(_stepIndex, stepName);
			return output;
		}

		private void BeginStep(int index, string stepName)
        {
            Console.WriteLine($"\tStarted step ({index}/{_stepCount}) '{stepName}'");
            _stepStopwatch.Start();
            GarbageCollect();
        }

        private void EndStep(int index, string stepName)
        {
            _stepStopwatch.Stop();
            Console.WriteLine(
                $"\tFinished step ({index}/{_stepCount}) '{stepName}' in {_stepStopwatch.Elapsed.TotalMilliseconds} ms");
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
}
