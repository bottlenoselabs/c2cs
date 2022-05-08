// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS;

public class BindgenTarget
{
    public string WorkingDirectory { get; set; } = string.Empty;

    public string ConfigurationFilePath { get; set; } = string.Empty;

    public string OutputLogFilePath { get; set; } = string.Empty;

    public string CSharpInputFilePath { get; set; } = string.Empty;

    public bool AddAsSource { get; set; } = true;

    public BindgenTargetConfiguration Configuration { get; set; } = null!;
}
