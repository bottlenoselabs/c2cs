// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.IntegrationTests.my_c_library.Fixtures;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class BindgenCSharpTests
{
    private BindgenCSharpFixture _fixture;

    public BindgenCSharpTests(BindgenCSharpFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Test()
    {
        Assert.True(true);
    }
}
