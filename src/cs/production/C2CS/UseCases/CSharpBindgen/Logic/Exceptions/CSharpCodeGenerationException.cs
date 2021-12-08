// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.UseCases.CSharpBindgen;

public class CSharpCodeGenerationException : Exception
{
    public CSharpCodeGenerationException()
    {
    }

    public CSharpCodeGenerationException(string message)
        : base(message)
    {
    }

    public CSharpCodeGenerationException(string message, Exception innerException)
        : base(message, innerException)
	{
	}
}
