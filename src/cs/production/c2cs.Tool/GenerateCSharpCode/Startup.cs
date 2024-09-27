// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.GenerateCSharpCode;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<Command>();
        services.AddSingleton<Tool>();
        services.AddSingleton<InputSanitizer>();

        services.AddSingleton<CodeGeneratorDocumentPInvoke>();
        services.AddSingleton<CodeGeneratorDocumentAssemblyAttributes>();
        services.AddSingleton<CodeGeneratorDocumentInteropRuntime>();
        services.AddSingleton<NameMapper>();
        AddNodeCodeGenerators(services);
    }

    private static void AddNodeCodeGenerators(IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(p => !p.IsAbstract && typeof(CodeGeneratorNodeBase).IsAssignableFrom(p))
            .ToImmutableArray();
        foreach (var type in types)
        {
            services.AddSingleton(type, type);
        }
    }
}
