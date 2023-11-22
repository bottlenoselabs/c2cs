// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;
using bottlenoselabs.Common.Tools;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Features.WriteCodeCSharp.Input.Sanitized;

namespace C2CS.Features.WriteCodeCSharp.Output;

public class WriteCodeCSharpOutput : ToolOutput<WriteCodeCSharpInput>
{
    public string OutputFileDirectory { get; private set; } = string.Empty;

    public CSharpProject? Project { get; set; }

    public Assembly? Assembly { get; set; }

    protected override void OnComplete()
    {
        OutputFileDirectory = Input.OutputFileDirectory;
    }
}
