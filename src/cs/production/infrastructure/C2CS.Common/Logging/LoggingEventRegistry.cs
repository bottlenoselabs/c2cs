// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS;

public static class LoggingEventRegistry
{
    private static int _count;

    public static EventId CreateEventIdentifier(string name)
    {
        return new EventId(_count++, name);
    }
}
