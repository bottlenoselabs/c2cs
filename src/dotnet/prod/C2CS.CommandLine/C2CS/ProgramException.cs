// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS
{
    public class ProgramException : Exception
    {
        public ProgramException(string message)
            : base(message)
        {
        }
    }
}
