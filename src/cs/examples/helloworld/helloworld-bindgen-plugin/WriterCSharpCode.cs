// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS;
using C2CS.Options;

public class WriterCSharpCode : IWriterCSharpCode
{
    public WriterOptionsCSharpCode? Options { get; set; } = new();

    public WriterCSharpCode()
    {
        Options!.OutputFilePath =
            "../../../src/cs/examples/helloworld/helloworld-app/my_c_library.cs";
        Options.NamespaceName = "my_c_library_namespace";
    }
}
