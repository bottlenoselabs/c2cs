// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;

internal static class Logging
{
    private static readonly Action<ILogger, string, Exception> ActionReadAbstractSyntaxTreeSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Serialize abstract syntax tree C: success."),
            "- Read abstract syntax tree C: Success. Path: {FilePath}");

    private static readonly Action<ILogger, string, Exception> ActionReadAbstractSyntaxTreeFailure =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Serialize abstract syntax tree C: failure."),
            "- Read abstract syntax tree C. Failed. Path: {FilePath}");

    private static readonly Action<ILogger, string, Exception> ActionWriteAbstractSyntaxTreeSuccess =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Write abstract syntax tree C: success."),
            "- Write abstract syntax tree C: Success. Path: {FilePath}");

    private static readonly Action<ILogger, string, Exception> ActionWriteAbstractSyntaxTreeFailure =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Write abstract syntax tree C: failure."),
            "- Write abstract syntax tree C. Failed. Path: {FilePath}");

    public static void ReadAbstractSyntaxTreeCSuccess(this ILogger logger, string filePath)
    {
        ActionReadAbstractSyntaxTreeSuccess(logger, filePath, null!);
    }

    public static void ReadAbstractSyntaxTreeCFailure(this ILogger logger, string filePath, Exception exception)
    {
        ActionReadAbstractSyntaxTreeFailure(logger, filePath, exception);
    }

    public static void WriteAbstractSyntaxTreeCSuccess(this ILogger logger, string filePath)
    {
        ActionWriteAbstractSyntaxTreeSuccess(logger, filePath, null!);
    }

    public static void WriteAbstractSyntaxTreeCFailure(this ILogger logger, string filePath, Exception exception)
    {
        ActionWriteAbstractSyntaxTreeFailure(logger, filePath, exception);
    }
}
