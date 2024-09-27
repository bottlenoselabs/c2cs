// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.GenerateCSharpCode;

public class CodeProject
{
    public ImmutableArray<CodeProjectDocument> Documents { get; set; } = ImmutableArray<CodeProjectDocument>.Empty;
}
