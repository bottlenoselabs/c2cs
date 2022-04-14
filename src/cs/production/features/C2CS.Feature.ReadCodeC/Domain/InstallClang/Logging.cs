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

    private static readonly Action<ILogger, string, Exception> ActionSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Install Clang: Success"),
            "- Success, file path: {FilePath}");

    private static readonly Action<ILogger, string, Exception> ActionSuccessAlreadyInstalled =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Install Clang: Success, already installed"),
            "- Success, already installed, file path: {FilePath}");

    public static void InstallClangFailed(this ILogger logger, Exception exception)
    {
        ActionFailed(logger, exception);
    }

    public static void InstallClangSuccess(this ILogger logger, string filePath)
    {
        ActionSuccess(logger, filePath, null!);
    }

    public static void InstallClangSuccessAlreadyInstalled(this ILogger logger, string filePath)
    {
        ActionSuccessAlreadyInstalled(logger, filePath, null!);
    }
}
