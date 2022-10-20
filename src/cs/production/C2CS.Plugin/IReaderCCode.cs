// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Options;

namespace C2CS;

public interface IReaderCCode
{
    ReaderCCodeOptions? Options { get; }

    bool IsOpaqueTypeName(string aliasTypeName)
    {
        return false;
    }

    bool CanVisitFunction(string name)
    {
        return true;
    }

    bool CanVisitVariable(string name)
    {
        return true;
    }

    bool IsMacroObjectNameAllowed(string name)
    {
        return true;
    }
}
