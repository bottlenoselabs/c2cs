// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using Microsoft.Extensions.Logging;

namespace C2CS;

public static class Logging
{
    private static readonly Action<ILogger, string, Exception> ActionConfigurationSuccess = LoggerMessage.Define<string>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Configuration load: success."),
        "Configuration load: Success. Path: {FilePath}.");

    private static readonly Action<ILogger, string, Exception> ActionConfigurationFailure = LoggerMessage.Define<string>(
        LogLevel.Information,
        LoggingEventRegistry.CreateEventIdentifier("Configuration load: failure."),
        "Configuration load. Failed. Path: {FilePath}.");

    public static void ConfigurationLoadSuccess(this ILogger logger, string filePath)
    {
        ActionConfigurationSuccess(logger, filePath, null!);
    }

    public static void ConfigurationLoadFailure(this ILogger logger, string filePath, Exception exception)
    {
        ActionConfigurationFailure(logger, filePath, exception);
    }
}
