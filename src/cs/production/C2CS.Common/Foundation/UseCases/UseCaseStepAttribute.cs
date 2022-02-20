// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS;

[AttributeUsage(
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = false)]
public sealed class UseCaseStepAttribute : Attribute
{
    public string StepName { get; }

    public UseCaseStepAttribute(string stepName)
    {
        StepName = stepName;
    }
}
