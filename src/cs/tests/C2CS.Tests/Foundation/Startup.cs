// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.C;
using C2CS.Tests.CSharp;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Tests.Foundation;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CMake.CMakeLibraryBuilder>();

        services.AddSingleton<TestCCode>();
        services.AddSingleton<TestFixtureCCode>();
        services.AddSingleton<IReaderCCode>(new TestReaderCCode());

        services.AddSingleton<TestCSharpCode>();
        services.AddSingleton<TestFixtureCSharpCode>();
        services.AddSingleton<IWriterCSharpCode>(new TestWriterCSharpCode());
    }
}
