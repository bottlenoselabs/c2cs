// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.BindgenCSharp;

public record CSharpConstant : CSharpNode
{
    public string Type;
    public string Value;

    public CSharpConstant(
        string name,
        string locationComment,
        string type,
        string value)
        : base(name, locationComment)
    {
        Type = type;
        Value = value;
    }
}
