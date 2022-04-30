// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Data.Serialization;
using C2CS.Feature.ReadCodeC.Domain;
using C2CS.Feature.ReadCodeC.Domain.ExploreCode;
using C2CS.Feature.ReadCodeC.Domain.InstallClang;
using C2CS.Feature.ReadCodeC.Domain.ParseCode;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Feature.ReadCodeC;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Data
        services.AddSingleton<CJsonSerializer>();

        // Logic
        services.AddSingleton<ClangInstaller>();
        services.AddSingleton<ClangArgumentsBuilder>();
        services.AddSingleton<TranslationUnitParser>();
        services.AddSingleton<TranslationUnitExplorer>();

        // Use case
        services.AddTransient<ReadCodeCUseCase>();
        services.AddSingleton<ReadCodeCValidator>();
    }
}
