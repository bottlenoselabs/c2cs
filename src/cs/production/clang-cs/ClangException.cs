// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

public class ClangException : Exception
{
    public ClangException(string message)
        : base(message)
    {
    }
}
