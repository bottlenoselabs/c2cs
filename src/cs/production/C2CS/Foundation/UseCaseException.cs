// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS;

public class UseCaseException : Exception
{
    public UseCaseException()
    {
    }

    public UseCaseException(string message)
        : base(message)
    {
    }

    public UseCaseException(string message, Exception innerException)
        : base(message, innerException)
	{
	}
}
