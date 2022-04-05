// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation;
using C2CS.Foundation.Logging;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ReadCodeC.Domain.InstallClang;

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

    private static readonly Action<ILogger, Exception> ActionSuccessAlreadyInstalled =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Install Clang: Success, already installed"),
            "- Success, already installed");

    public static void InstallClangFailed(this ILogger logger, Exception exception)
    {
        ActionFailed(logger, exception);
    }

    public static void InstallClangSuccess(this ILogger logger)
    {
        ActionSuccess(logger, null!);
    }

    public static void InstallClangSuccessAlreadyInstalled(this ILogger logger)
    {
        ActionSuccessAlreadyInstalled(logger, null!);
    }
}
