// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS;
using C2CS.Options;
using JetBrains.Annotations;

[PublicAPI]
public class ReaderCCode : IReaderCCode
{
    public ReaderCCodeOptions Options { get; } = new();

    public bool IsOpaqueTypeName(string aliasTypeName)
    {
        return false;
    }

    public bool CanVisitFunction(string name)
    {
        return true;
    }

    public bool CanVisitVariable(string name)
    {
        return true;
    }

    public bool IsMacroObjectNameAllowed(string name)
    {
        return true;
    }

    public ReaderCCode()
    {
        Options.InputHeaderFilePath =
            "../../../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/my_c_library/include/my_c_library.h";
        Options.OutputAbstractSyntaxTreesFileDirectory =
            "../../../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/my_c_library/ast";
    }

    public virtual bool CanVisitFunction()
    {
        return true;
    }
}
