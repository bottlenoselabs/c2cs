// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace C2CS.Tests;

#pragma warning disable SA1649
public static class CSharpTestFunctionParameterExtensions
#pragma warning restore SA1649
{
    public static CSharpTestFunctionParameterAssertions Should(this CSharpTestFunctionParameter? instance)
    {
        return new(instance);
    }
}

public class CSharpTestFunctionParameterAssertions(CSharpTestFunctionParameter? instance)
    : ReferenceTypeAssertions<CSharpTestFunctionParameter?, CSharpTestFunctionParameterAssertions>(instance)
{
    protected override string Identifier => nameof(CSharpTestFunctionParameter);

    [CustomAssertion]
    public AndConstraint<CSharpTestFunctionParameterAssertions> BeParameterWithTypeVoid(string parameterName)
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(x =>
            {
                if (x == null)
                {
                    return false;
                }

                if (parameterName != x.Name)
                {
                    return false;
                }

                return TypeIsVoid(x.Type);
            })
            .FailWith(Subject == null
                ? "Expected the function parameter to be non-null."
                : $"Expected the function parameter '{Subject.Name}' to be type of 'void', but the names, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestFunctionParameterAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<CSharpTestFunctionParameterAssertions> BeParameterWithTypeVoidPointer(string parameterName)
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(x =>
            {
                if (x == null)
                {
                    return false;
                }

                if (parameterName != x.Name)
                {
                    return false;
                }

                return TypeIsVoidPointer(x.Type);
            })
            .FailWith(Subject == null
                ? "Expected the function parameter to be non-null."
                : $"Expected the function parameter '{Subject.Name}' to be type of 'void*', but the names, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestFunctionParameterAssertions>(this);
    }

    [CustomAssertion]
    public AndConstraint<CSharpTestFunctionParameterAssertions> BeParameterWithType(string parameterName, string typeName, int sizeOf)
    {
        _ = Execute.Assertion
            .Given(() => Subject)
            .ForCondition(x => Predicate(x, parameterName, typeName, sizeOf))
            .FailWith(Subject == null
                ? "Expected the function parameter to be non-null."
                : $"Expected parameter '{Subject.Name}' to be type of '{typeName}', but the names, size of, or inner type do not match.");

        return new AndConstraint<CSharpTestFunctionParameterAssertions>(this);

        static bool Predicate(CSharpTestFunctionParameter? parameter, string parameterName, string typeName, int sizeOf)
        {
            if (parameter == null)
            {
                return false;
            }

            if (parameter.Name != parameterName)
            {
                return false;
            }

            var type = parameter.Type;
            if (type.Name != typeName)
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

    private static bool TypeIsVoidPointer(CSharpTestType? type)
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
