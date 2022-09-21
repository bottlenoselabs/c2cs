// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Configuration;

namespace C2CS;

public interface IBindgenController
{
    ConfigurationBindgen Configuration { get; }
}
