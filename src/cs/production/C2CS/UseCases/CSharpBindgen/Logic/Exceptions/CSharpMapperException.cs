// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.UseCases.CSharpBindgen;

public class CSharpMapperException : Exception
{
    public CSharpMapperException(string message)
        : base(message)
    {
    }
}
