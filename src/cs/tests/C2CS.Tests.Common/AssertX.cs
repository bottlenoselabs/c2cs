// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Xunit;
using Xunit.Sdk;

namespace C2CS.Tests.Common;

public static class AssertX
{
    public static void Equal(string expected, string actual, string userMessage)
    {
        try
        {
            Assert.Equal(expected, actual);
        }
        catch (XunitException e)
        {
            var userMessageProperty = e.GetType().GetProperty("UserMessage");
            userMessageProperty!.SetValue(e, userMessage);
            throw;
        }
    }
}
