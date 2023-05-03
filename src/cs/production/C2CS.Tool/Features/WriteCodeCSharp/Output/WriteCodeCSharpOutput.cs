// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Features.WriteCodeCSharp.Input.Sanitized;
using C2CS.Foundation.Tool;

namespace C2CS.Features.WriteCodeCSharp.Output;

public class WriteCodeCSharpOutput : ToolOutput<WriteCodeCSharpInput>
{
    public string OutputFilePath { get; private set; } = string.Empty;

    protected override void OnComplete()
    {
        OutputFilePath = Input.OutputFilePath;
    }
}
