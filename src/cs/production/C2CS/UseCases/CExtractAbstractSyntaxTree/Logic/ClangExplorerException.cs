// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static clang;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

internal class ClangExplorerException : Exception
{
    public ClangExplorerException(string message)
    {
        Message = message;
    }

    public override string Message { get; }
}
