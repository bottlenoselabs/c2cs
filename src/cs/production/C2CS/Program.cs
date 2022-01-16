// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.UseCases.BindgenCSharp;
using C2CS.UseCases.ExtractAbstractSyntaxTreeC;

namespace C2CS;

public static class Program
{
    public static int Main(string[]? args = null)
    {
        var configuration = GetConfigurationFrom(args);
        return EntryPoint(configuration);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static int EntryPoint(ProgramConfiguration configuration)
    {
        var jsonFilePath = ExtractAbstractSyntaxTreeC(configuration);
        BindgenCSharp(jsonFilePath, configuration);
        return Environment.ExitCode;
    }

    private static string ExtractAbstractSyntaxTreeC(ProgramConfiguration c)
    {
        var request = new RequestExtractAbstractSyntaxTreeC(
            c.InputFilePath,
            c.AbstractSyntaxTreeOutputFilePath,
            c.IsEnabledFindSdk,
            c.MachineBitWidth,
            c.IncludeDirectories,
            c.ExcludedHeaderFiles,
            c.OpaqueTypeNames,
            c.FunctionNamesWhiteList,
            c.Defines,
            c.ClangArguments);
        var useCase = new UseCaseExtractAbstractSyntaxTreeC();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }

        return request.OutputFilePath;
    }

    private static void BindgenCSharp(string inputFilePath, ProgramConfiguration c)
    {
        var request = new RequestBindgenCSharp(
            inputFilePath,
            c.OutputFilePath,
            c.LibraryName,
            c.NamespaceName,
            c.ClassName,
            c.MappedTypeNames,
            c.IgnoredTypeNames,
            c.HeaderCodeRegionFilePath,
            c.FooterCodeRegionFilePath);
        var useCase = new UseCaseBindgenCSharp();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }
    }

    private static ProgramConfiguration GetConfigurationFrom(IReadOnlyList<string>? args)
    {
        var argsCount = args?.Count ?? 0;
        var configurationFilePath = argsCount switch
        {
            1 => args![0],
            0 => Path.Combine(Environment.CurrentDirectory, "config.json"),
            _ => throw new InvalidOperationException(
                "Unsupported number of arguments. Please specify zero arguments or one argument. For documentation please visit: https://github.com/bottlenoselabs/c2cs/blob/main/docs/README.md")
        };

        try
        {
            var fileContents = File.ReadAllText(configurationFilePath);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,

                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
            var serializerContext = new ProgramConfigurationSerializerContext(jsonSerializerOptions);
            var configuration = JsonSerializer.Deserialize(fileContents, serializerContext.ProgramConfiguration)!;
            return configuration;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
