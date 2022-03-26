// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public sealed class ExtractAbstractSyntaxTreeResponse : UseCaseResponse
{
    public string? FilePath { get; init; }
}
