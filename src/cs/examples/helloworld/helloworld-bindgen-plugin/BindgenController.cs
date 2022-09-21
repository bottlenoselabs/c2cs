// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS;
using C2CS.Configuration;
using JetBrains.Annotations;

[PublicAPI]
public class BindgenController : IBindgenController
{
    public ConfigurationBindgen Configuration { get; } = new();

    public BindgenController()
    {
        Configuration.InputOutputFileDirectory =
            "../../../src/cs/examples/helloworld/helloworld-my_c_library/my_c_library/ast";
        Configuration.ReadCCode.InputFilePath =
            "../../../src/cs/examples/helloworld/helloworld-my_c_library/my_c_library/include/my_c_library.h";

        Configuration.WriteCSharpCode.OutputFilePath =
            "../../../src/cs/examples/helloworld/helloworld-app/my_c_library.cs";
        Configuration.WriteCSharpCode.NamespaceName = "my_c_library_namespace";
    }
}
