// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Data.Model;

public record CSharpAliasStruct(
        string Name,
        string CodeLocationComment,
        CSharpType UnderlyingType)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly CSharpType UnderlyingType = UnderlyingType;
}
