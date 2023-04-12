// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Foundation.Tool;

public sealed class ToolInputSanitizationException : Exception
{
    public ToolInputSanitizationException()
    {
    }

    public ToolInputSanitizationException(string message)
        : base(message)
    {
    }

    public ToolInputSanitizationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
