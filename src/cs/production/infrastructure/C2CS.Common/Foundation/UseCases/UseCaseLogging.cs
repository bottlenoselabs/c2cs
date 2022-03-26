// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace C2CS;

internal static class UseCaseLogging
{
    private static readonly Action<ILogger, Exception> ActionUseCaseStarted = LoggerMessage.Define(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case started"),
        "- Started");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseSucceeded = LoggerMessage.Define<TimeSpan>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case success"),
        "- Success in {Elapsed:s\\.fff} seconds");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseFailed = LoggerMessage.Define<TimeSpan>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case failed"),
        "- Failed in {Elapsed:s\\.fff} seconds");

    private static readonly Action<ILogger, Exception> ActionUseCaseStepStarted = LoggerMessage.Define(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case step started"),
        "- Step started");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseStepFinished = LoggerMessage.Define<TimeSpan>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case step finished"),
        "- Step finished in {Elapsed:s\\.fff} seconds");

    public static void UseCaseStarted(this ILogger logger)
    {
        ActionUseCaseStarted(logger, null!);
    }

    public static void UseCaseSucceeded(this ILogger logger, TimeSpan timeSpan)
    {
        ActionUseCaseSucceeded(logger, timeSpan, null!);
    }

    public static void UseCaseFailed(this ILogger logger, TimeSpan timeSpan)
    {
        ActionUseCaseFailed(logger, timeSpan, null!);
    }

    public static void UseCaseStepStarted(this ILogger logger)
    {
        ActionUseCaseStepStarted(logger, null!);
    }

    public static void UseCaseStepFinished(this ILogger logger, TimeSpan timeSpan)
    {
        ActionUseCaseStepFinished(logger, timeSpan, null!);
    }
}
