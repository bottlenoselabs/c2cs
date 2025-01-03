// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common.Model;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace C2CS.Tests.Common.Assertions;

#pragma warning disable SA1649

public class CSharpTestTypeAssertions(CSharpTestType? instance) : ReferenceTypeAssertions<CSharpTestType?, CSharpTestTypeAssertions>(instance)
{
    protected override string Identifier => nameof(CSharpTestType);

    [CustomAssertion]
    public AndConstraint<CSharpTestTypeAssertions> BeTypeVoid()
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(TypeIsVoid)
            .FailWith($"Expected the type '{Subject}' to be type of 'void', but the name, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestTypeAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<CSharpTestTypeAssertions> BeTypeVoidPointer()
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(Predicate)
            .FailWith($"Expected the type '{Subject}' to be type of 'void*', but the name, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestTypeAssertions>(this);

        static bool Predicate(CSharpTestType? type)
        {
            if (type == null)
            {
                return false;
            }

            if (type.Name != "void*")
            {
                return false;
            }

            if (type.SizeOf == null || type.SizeOf != IntPtr.Size)
            {
                return false;
            }

            return TypeIsVoid(type.InnerType);
        }
    }

    [CustomAssertion]
    public AndConstraint<CSharpTestTypeAssertions> BeType(string name, int sizeOf)
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(x => Predicate(x, name, sizeOf))
            .FailWith($"Expected the type '{Subject}' to be type of '{name}', but the name, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestTypeAssertions>(this);

        static bool Predicate(CSharpTestType? type, string name, int sizeOf)
        {
            if (type == null)
            {
                return false;
            }

            if (type.Name != name)
            {
                return false;
            }

            if (type.SizeOf == null || type.SizeOf != sizeOf)
            {
                return false;
            }

            if (type.InnerType != null)
            {
                return false;
            }

            return true;
        }
    }

    private static bool TypeIsVoid(CSharpTestType? type)
    {
        if (type == null)
        {
            return false;
        }

        if (type.Name != "void")
        {
            return false;
        }

        if (type.SizeOf != null)
        {
            return false;
        }

        if (type.InnerType != null)
        {
            return false;
        }

        return true;
    }
}
