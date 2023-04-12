// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Tool;
using C2CS.WriteCodeCSharp.Input.Sanitized;

namespace C2CS.WriteCodeCSharp.Output;

public class WriteCodeCSharpOutput : ToolOutput<WriteCodeCSharpInput>
{
    public string OutputFilePath { get; private set; } = string.Empty;

    protected override void OnComplete()
    {
        OutputFilePath = Input.OutputFilePath;
    }
}
