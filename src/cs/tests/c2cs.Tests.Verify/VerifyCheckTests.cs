// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace c2cs.Tests.Verify;

public sealed class VerifyCheckTests
{
    [Fact]
    public Task Run()
    {
        return VerifyChecks.Run();
    }
}
