// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static clang;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

public class ClangExplorerException : Exception
{
    public override string Message { get; }

    public ClangExplorerException()
    {
        Message = string.Empty;
    }

    public ClangExplorerException(string message)
    {
        Message = message;
    }

    public ClangExplorerException(string message, Exception innerException)
        : base(message, innerException)
    {
        Message = message;
    }
}
