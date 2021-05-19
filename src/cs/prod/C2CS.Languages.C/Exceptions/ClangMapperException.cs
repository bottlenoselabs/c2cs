// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.Languages.C
{
    public class ClangMapperException : Exception
    {
        public ClangMapperException()
            : base("The header file used has unforeseen conditions. Please create an issue on GitHub with the stack trace along with the header file.")
        {
        }
    }
}
