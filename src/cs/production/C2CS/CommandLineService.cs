// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Help;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace C2CS;

internal sealed class CommandLineService : IHostedService
{
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly string[] _commandLineArguments;
    private readonly RootCommand _rootCommand;

    public CommandLineService(
        IApplicationLifetime applicationLifetime,
        CommandLineArgumentsProvider commandLineArgumentsProvider,
        RootCommand command)
    {
        _applicationLifetime = applicationLifetime;
        _commandLineArguments = commandLineArgumentsProvider.CommandLineArguments;
        _rootCommand = command;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStarted.Register(() => Task.Run(Main, cancellationToken));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Main()
    {
        var exitCode = _rootCommand.Invoke(_commandLineArguments);

        // if (_commandLineArguments.Length != 0)
        // {
        //     exitCode = _rootCommand.Invoke(_commandLineArguments);
        // }
        // else
        // {
        //     var helpBuilder = new HelpBuilder(LocalizationResources.Instance, Console.WindowWidth);
        //     helpBuilder.Write(_rootCommand, Console.Out);
        //     exitCode = 0;
        // }

        Environment.ExitCode = exitCode;
        _applicationLifetime.StopApplication();
    }
}
