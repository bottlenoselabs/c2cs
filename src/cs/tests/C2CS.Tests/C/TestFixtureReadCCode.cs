// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Tests.C;

public class TestFixtureReadCCode
{
    public ImmutableArray<TestReadCCodeAbstractSyntaxTree> AbstractSyntaxTrees { get; }

    public TestFixtureReadCCode(ImmutableArray<TestReadCCodeAbstractSyntaxTree> abstractSyntaxTrees)
    {
        AbstractSyntaxTrees = abstractSyntaxTrees;
    }
}
