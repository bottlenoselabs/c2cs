// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.InstallClang;

internal static class Logging
{
    private const string Name = "Install Clang";

    private static readonly Action<ILogger, Exception> ActionFailed =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Failed."),
            "- " + Name + ": Failed.");

    private static readonly Action<ILogger, Exception> ActionSuccess =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Success."),
            "- " + Name + ": Success.");

    public static void InstallClangFailed(this ILogger logger, Exception exception)
    {
        ActionFailed(logger, exception);
    }

    public static void InstallClangSuccess(this ILogger logger)
    {
        ActionSuccess(logger, null!);
    }
}
