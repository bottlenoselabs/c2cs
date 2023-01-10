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
