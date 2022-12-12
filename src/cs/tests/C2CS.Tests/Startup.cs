// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.Common;
using C2CS.Tests.test_c_library.Fixtures.C;
using C2CS.Tests.test_c_library.Fixtures.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Tests;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        SourceDirectory.SetPath();

        services.AddSingleton<ReadCCodeFixture>();
        services.AddSingleton<IReaderCCode>(new ReadCCodeFixtureReader());

        services.AddSingleton<WriteCSharpCodeFixture>();
        services.AddSingleton<IWriterCSharpCode>(new WriteCSharpCodeFixtureWriter());
    }
}
