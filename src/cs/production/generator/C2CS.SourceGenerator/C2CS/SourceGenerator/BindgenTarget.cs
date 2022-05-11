// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data;

namespace C2CS.SourceGenerator;

public class BindgenTarget
{
    public string WorkingDirectory { get; set; } = string.Empty;

    public string OutputLogFilePath { get; set; } = string.Empty;

    public string OutputConfigurationFilePath { get; set; } = string.Empty;

    public bool IsEnabledDeleteOutput { get; set; }

    public BindgenConfiguration Configuration { get; set; } = null!;
}
