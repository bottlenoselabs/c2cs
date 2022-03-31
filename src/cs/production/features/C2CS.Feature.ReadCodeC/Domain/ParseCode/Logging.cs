// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ReadCodeC.Domain.ParseCode;

internal static class Logging
{
    private static readonly Action<ILogger, string, string, Exception> ActionParseFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Parse translation unit: Failed"),
            "- Failed. Path: {FilePath} ; Clang arguments: {Arguments}");

    private static readonly Action<ILogger, string, string, int, Exception> ActionParseSuccess =
        LoggerMessage.Define<string, string, int>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Parse translation unit: Success"),
            "- Success. Path: {FilePath} ; Clang arguments: {Arguments} ; Diagnostics: {DiagnosticsCount}");

    private static readonly Action<ILogger, string, Exception> ActionSystemIncludeDirectoryDoesNotExist =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("System include directory does not exist."),
            "- The system include directory does not exist: {DirectoryPath}.");

    public static void ParseTranslationUnitFailed(this ILogger logger, string filePath, string arguments, Exception exception)
    {
        ActionParseFailed(logger, filePath, arguments, exception);
    }

    public static void ParseTranslationUnitSuccess(this ILogger logger, string filePath, string arguments, int diagnosticsCount)
    {
        ActionParseSuccess(logger, filePath, arguments, diagnosticsCount, null!);
    }

    public static void SystemIncludeDirectoryDoesNotExist(this ILogger logger, string directoryPath)
    {
        ActionSystemIncludeDirectoryDoesNotExist(logger, directoryPath, null!);
    }
}
