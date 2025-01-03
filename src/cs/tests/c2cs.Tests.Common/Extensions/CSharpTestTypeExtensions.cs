// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common.Assertions;
using C2CS.Tests.Common.Model;

namespace C2CS.Tests.Common.Extensions;

public static class CSharpTestTypeExtensions
#pragma warning restore SA1649
{
    public static CSharpTestTypeAssertions Should(this CSharpTestType? instance)
    {
        return new(instance);
    }
}
