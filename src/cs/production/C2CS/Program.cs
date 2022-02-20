// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Linq;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;

namespace C2CS;

public static class Program
{
    public static int Main(string[]? args = null)
    {
        if (args != null && args.Length == 2 && args[0] == "build")
        {
            return Feature.BuildLibraryC.Program.Main(args.Skip(1).ToArray());
        }
        else
        {
            var configuration = Configuration.GetFrom(args);
            return EntryPoint(configuration);
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static int EntryPoint(Configuration configuration)
    {
        var jsonFilePath = ExtractAbstractSyntaxTreeC(configuration);
        BindgenCSharp(jsonFilePath, configuration);
        return Environment.ExitCode;
    }

    private static string ExtractAbstractSyntaxTreeC(Configuration c)
    {
        var request = new Feature.ExtractAbstractSyntaxTreeC.Input(
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
        var useCase = new UseCase();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }

        return request.OutputFilePath;
    }

    private static void BindgenCSharp(string inputFilePath, Configuration c)
    {
        var request = new Feature.BindgenCSharp.Input(
            inputFilePath,
            c.OutputFilePath,
            c.LibraryName,
            c.NamespaceName,
            c.ClassName,
            c.MappedTypeNames,
            c.IgnoredTypeNames,
            c.HeaderCodeRegionFilePath,
            c.FooterCodeRegionFilePath);
        var useCase = new Feature.BindgenCSharp.UseCase();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }
    }
}
