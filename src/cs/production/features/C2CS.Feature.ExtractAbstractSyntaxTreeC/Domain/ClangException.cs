// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;

public sealed class ClangException : Exception
{
    public ClangException()
    {
    }

    public ClangException(string message)
        : base(message)
    {
    }

    public ClangException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
