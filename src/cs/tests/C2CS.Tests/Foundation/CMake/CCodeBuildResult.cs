// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Foundation.CMake;

public class CCodeBuildResult
{
    public bool IsSuccess { get; }

    public CCodeBuildResult(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }

    public static readonly CCodeBuildResult Failure = new(false);
}
