// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Threading.Tasks;

namespace C2CS;

public readonly record struct UseCaseResponseUnit
{
    // ReSharper disable once UnassignedReadonlyField
    public static readonly UseCaseResponseUnit Value;

	public static Task<UseCaseResponseUnit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    public override string ToString() => "()";
}
