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
        "- Use case started");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseSucceeded = LoggerMessage.Define<TimeSpan>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case success"),
        "- Use case success in {Elapsed:s\\.fff} seconds");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseFailed = LoggerMessage.Define<TimeSpan>(
        LogLevel.Error,
        LoggingEventRegistry.CreateEventIdentifier("Use case failed"),
        "- Use case failed in {Elapsed:s\\.fff} seconds");

    private static readonly Action<ILogger, Exception> ActionUseCaseStepStarted = LoggerMessage.Define(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case step started"),
        "- Use case step started");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseStepSucceeded = LoggerMessage.Define<TimeSpan>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Use case step success"),
        "- Use case step success in {Elapsed:s\\.fff} seconds");

    private static readonly Action<ILogger, TimeSpan, Exception> ActionUseCaseStepFailed = LoggerMessage.Define<TimeSpan>(
        LogLevel.Error,
        LoggingEventRegistry.CreateEventIdentifier("Use case step failed"),
        "- Use case step failed in {Elapsed:s\\.fff} seconds");

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

    public static void UseCaseStepSucceeded(this ILogger logger, TimeSpan timeSpan)
    {
        ActionUseCaseStepSucceeded(logger, timeSpan, null!);
    }

    public static void UseCaseStepFailed(this ILogger logger, TimeSpan timeSpan)
    {
        ActionUseCaseStepFailed(logger, timeSpan, null!);
    }
}
