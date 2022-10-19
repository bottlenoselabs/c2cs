// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Options;

namespace C2CS.Foundation.UseCases;

public abstract class UseCaseValidator<TConfiguration, TInput>
    where TConfiguration : UseCaseOptions
{
    public abstract TInput Validate(TConfiguration options);
}
