// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Options;

namespace C2CS.Tests.CSharp;

public class TestWriterCSharpCode : IWriterCSharpCode
{
    public WriterCSharpCodeOptions? Options { get; set; }

    public TestWriterCSharpCode()
    {
        Options = CreateOptions();
    }

    private WriterCSharpCodeOptions CreateOptions()
    {
        var result = new WriterCSharpCodeOptions
        {
            InputAbstractSyntaxTreesFileDirectory = "./c/tests/_container_library/ast",
            OutputCSharpCodeFilePath = "./my_c_library.cs",
            NamespaceName = "bottlenoselabs"
        };

        return result;
    }
}
