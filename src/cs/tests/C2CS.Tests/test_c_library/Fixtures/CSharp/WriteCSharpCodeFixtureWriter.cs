// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Options;

namespace C2CS.Tests.test_c_library.Fixtures.CSharp;

public class WriteCSharpCodeFixtureWriter : IWriterCSharpCode
{
    public WriterCSharpCodeOptions? Options { get; set; }

    public WriteCSharpCodeFixtureWriter()
    {
        Options = CreateOptions();
    }

    private WriterCSharpCodeOptions CreateOptions()
    {
        var result = new WriterCSharpCodeOptions
        {
            InputAbstractSyntaxTreesFileDirectory = "./c/tests/test_c_library/ast",
            OutputCSharpCodeFilePath = "./my_c_library.cs",
            NamespaceName = "bottlenoselabs"
        };

        return result;
    }
}
