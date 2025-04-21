// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Runtime.CompilerServices;
using C2CS.BuildCLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VerifyTests.DiffPlex;
using Xunit.DependencyInjection.Logging;

namespace C2CS.Tests.Verify;

// Magic class "Startup" to set up DI
// See https://github.com/pengweiqhca/Xunit.DependencyInjection?tab=readme-ov-file#how-to-find-startup
public sealed class Startup
{
    [ModuleInitializer]
    public static void Initialize() => VerifyDiffPlex.Initialize(OutputType.Compact);

    [ModuleInitializer]
    public static void OtherInitialize()
    {
        VerifierSettings.InitializePlugins();
        VerifierSettings.ScrubLinesContaining("DiffEngineTray");
        VerifierSettings.IgnoreStackTrace();
    }

    public void ConfigureHost(IHostBuilder hostBuilder) => hostBuilder.BuildHostCommon();

    public void ConfigureServices(IServiceCollection services) => services
        .AddSingleton<CMakeLibraryBuilder>()
        .AddLogging(builder => builder.AddXunitOutput());
}
