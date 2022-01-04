// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;

namespace C2CS;

public class ProgramConfigurationException : Exception
{
    public ProgramConfigurationException()
        : this(string.Empty)
    {
    }

    public ProgramConfigurationException(string message)
        : base(message)
    {
    }

    public ProgramConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
