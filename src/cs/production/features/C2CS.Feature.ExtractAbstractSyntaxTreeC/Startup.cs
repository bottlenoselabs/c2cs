// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Serialization;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ExploreCode;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.InstallClang;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ParseCode;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Data
        services.AddSingleton<CJsonSerializer>();

        // Logic
        services.AddSingleton<ClangInstaller>();
        services.AddSingleton<ClangTranslationUnitParser>();
        services.AddSingleton<ClangTranslationUnitExplorer>();

        // Use case
        services.AddTransient<ExtractAbstractSyntaxTreeUseCase>();
        services.AddSingleton<ExtractAbstractSyntaxTreeValidator>();
    }
}
