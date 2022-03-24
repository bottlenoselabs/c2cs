// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public class Response : UseCaseResponse
{
    public CAbstractSyntaxTree? AbstractSyntaxTree { get; set; }
}
