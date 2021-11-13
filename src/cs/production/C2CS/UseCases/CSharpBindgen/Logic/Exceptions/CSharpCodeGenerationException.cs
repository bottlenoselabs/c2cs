// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.UseCases.CSharpBindgen;

public class CSharpCodeGenerationException : Exception
{
    public CSharpCodeGenerationException(string message)
        : base(message)
    {
    }
}
