// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ParseCode;

internal static class Logging
{
    private static readonly Action<ILogger, string, string, Exception> ActionFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Parse translation unit: Failed"),
            "- Failed. Path: {FilePath} ; Clang arguments: {Arguments}");

    private static readonly Action<ILogger, string, string, int, Exception> ActionSuccess =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Parse translation unit: Success"),
            "- Success. Path: {FilePath} ; Clang arguments: {Arguments} ; Diagnostics: {DiagnosticsCount}");

    public static void ParseTranslationUnitFailed(this ILogger logger, string filePath, string arguments, Exception exception)
    {
        ActionFailed(logger, filePath, arguments, exception);
    }

    public static void ParseTranslationUnitSuccess(this ILogger logger, string filePath, string arguments, int diagnosticsCount)
    {
        ActionSuccess(logger, filePath, arguments, diagnosticsCount, null!);
    }
}
