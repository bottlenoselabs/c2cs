// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS;

public class BindgenTargetConfiguration
{
    public string InputFilePath { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string NamespaceName { get; set; } = string.Empty;

    public string LibraryName { get; set; } = string.Empty;

    public BindgenTargetConfigurationAttributes Attributes { get; set; } = null!;
}
