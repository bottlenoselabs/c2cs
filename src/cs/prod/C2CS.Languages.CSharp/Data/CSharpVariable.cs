// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.CSharp
{
    public record CSharpVariable : CSharpCommon
    {
        public CSharpType Type;

        public CSharpVariable(
            string name,
            string locationComment,
            CSharpType type)
            : base(name, locationComment)
        {
            Type = type;
        }
    }
}
