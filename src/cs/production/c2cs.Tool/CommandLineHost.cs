// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace C2CS;

internal sealed class CommandLineHost(
    IHostApplicationLifetime applicationLifetime,
    CommandLineArgumentsProvider commandLineArgumentsProvider,
    System.CommandLine.RootCommand command) : IHostedService
{
    private readonly string[] _commandLineArguments = commandLineArgumentsProvider.CommandLineArguments;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = applicationLifetime.ApplicationStarted.Register(() => Task.Run(Main, cancellationToken));
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void Main()
    {
        Environment.ExitCode = command.Invoke(_commandLineArguments);
        applicationLifetime.StopApplication();
    }
}
