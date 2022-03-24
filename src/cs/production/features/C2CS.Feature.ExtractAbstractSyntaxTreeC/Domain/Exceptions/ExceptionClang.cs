// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;

#pragma warning disable CA1710
public class ExceptionClang : Exception
#pragma warning restore CA1710
{
    public ExceptionClang()
    {
    }

    public ExceptionClang(string message)
        : base(message)
    {
    }

    public ExceptionClang(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
