// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using bottlenoselabs.Common.Tools;

namespace C2CS.GenerateCSharpCode;

public class Output : ToolOutput<InputSanitized>
{
    public string OutputFileDirectory { get; private set; } = string.Empty;

    public CodeProject? Project { get; set; }

    protected override void OnComplete()
    {
        OutputFileDirectory = Input.OutputFileDirectory;
    }
}
