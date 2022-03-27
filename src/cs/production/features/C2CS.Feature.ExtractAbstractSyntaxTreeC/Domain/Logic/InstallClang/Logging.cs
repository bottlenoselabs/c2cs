// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.InstallClang;

internal static class Logging
{
    private static readonly Action<ILogger, Exception> ActionFailed =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Install Clang: Failed."),
            "- Failed");

    private static readonly Action<ILogger, Exception> ActionSuccess =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Install Clang: Success"),
            "- Success");

    public static void InstallClangFailed(this ILogger logger, Exception exception)
    {
        ActionFailed(logger, exception);
    }

    public static void InstallClangSuccess(this ILogger logger)
    {
        ActionSuccess(logger, null!);
    }
}
