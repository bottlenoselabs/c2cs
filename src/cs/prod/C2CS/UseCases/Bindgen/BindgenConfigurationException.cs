// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.UseCases.Bindgen
{
    public class BindgenConfigurationException : Exception
    {
        public BindgenConfigurationException(string message)
            : base(message)
        {
        }
    }
}
