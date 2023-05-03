// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.CommandLine;
using C2CS.Features.BuildCLibrary;
using C2CS.Features.WriteCodeCSharp;

namespace C2CS.Foundation;

internal sealed class CommandLineInterface : RootCommand
{
    public CommandLineInterface(
        BuildCLibraryCommand buildCLibraryCommand,
        WriteCodeCSharpCommand writeCodeCSharpCommand)
        : base("C2CS - C to C# bindings code generator.")
    {
        AddCommand(buildCLibraryCommand);
        AddCommand(writeCodeCSharpCommand);
    }
}
