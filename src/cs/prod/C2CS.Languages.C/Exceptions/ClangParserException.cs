// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    public class ClangParserException : Exception
    {
        public ClangParserException(string message)
            : base(message)
        {
        }
    }
}
