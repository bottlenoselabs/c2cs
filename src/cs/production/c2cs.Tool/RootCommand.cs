// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS;

internal sealed class RootCommand : System.CommandLine.RootCommand
{
    public RootCommand(
        BuildCLibrary.Command buildCLibraryCommand,
        GenerateCSharpCode.Command generateCSharpCodeCommand)
        : base("C2CS - C to C# bindings code generator.")
    {
        AddCommand(buildCLibraryCommand);
        AddCommand(generateCSharpCodeCommand);
    }
}
