// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.IntegrationTests;

public abstract class IntegrationTest
{
    protected static IServiceProvider Services => TestHost.Services;
}
