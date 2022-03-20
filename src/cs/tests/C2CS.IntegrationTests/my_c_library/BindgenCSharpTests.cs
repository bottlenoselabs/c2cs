// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707
#pragma warning disable SA1300
#pragma warning disable IDE1006
#pragma warning disable CA1034

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class BindgenCSharpTests
{
    [Fact]
    public void Test()
    {
        Assert.True(true);
    }
}
