// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using C2CS.UseCases.CSharpBindgen;

namespace C2CS;

public static class Program
{
    public static int Main(string[] args)
    {
        return CreateRootCommand().InvokeAsync(args).Result;
    }

    private static Command CreateRootCommand()
    {
        // Create a root command with some options
        var rootCommand = new RootCommand("C2CS - C to C# bindings code generator.");

        var abstractSyntaxTreeCommand = CommandAbstractSyntaxTreeC();
        rootCommand.Add(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = CommandBindgenCSharp();
        rootCommand.Add(bindgenCSharpCommand);

        return rootCommand;
    }

    private static Command CommandAbstractSyntaxTreeC()
    {
        var command =
            new Command("ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");

        var inputFileOption = new Option<string>(
            new[] { "--inputFile", "-i" },
            "File path of the input `.h` header file.")
        {
            IsRequired = true
        };
        command.AddOption(inputFileOption);

        var outputFileOption = new Option<string>(
            new[] { "--outputFile", "-o" },
            "File path of the output abstract syntax tree `.json` file.")
        {
            IsRequired = true
        };
        command.AddOption(outputFileOption);

        var automaticallyFindSoftwareDevelopmentKitOption = new Option<IEnumerable<string>?>(
            new[] { "--automaticallyFindSoftwareDevelopmentKit", "-f" },
            "Find software development kit for C/C++ automatically. Default is `true`.")
        {
            IsRequired = false
        };
        command.AddOption(automaticallyFindSoftwareDevelopmentKitOption);

        var includeDirectoriesOption = new Option<IEnumerable<string?>?>(
            new[] { "--includeDirectories", "-s" },
            "Search directories for `#include` usages to use when parsing C code.")
        {
            IsRequired = false
        };
        command.AddOption(includeDirectoriesOption);

        var ignoredFilesOption = new Option<IEnumerable<string?>?>(
            new[] { "--ignoredFiles", "-g" },
            "Header files to ignore by file name with extension (exclude directory path).")
        {
            IsRequired = false
        };
        command.AddOption(ignoredFilesOption);

        var opaqueTypesOption = new Option<IEnumerable<string?>?>(
            new[] { "--opaqueTypes", "-p" },
            "Types by name that will be forced to be opaque.")
        {
            IsRequired = false
        };
        command.AddOption(opaqueTypesOption);

        var definesOption = new Option<IEnumerable<string>?>(
            new[] { "--defines", "-d" },
            "Object-like macros to use when parsing C code.")
        {
            IsRequired = false
        };
        command.AddOption(definesOption);

        var bitOption = new Option<int>(
            new[] { "--bitness", "-b" },
            "The bitness to parse the C code as. Default is the current architecture of host operating system. E.g. the default for x64 Windows is `64`. Possible values are `32` where pointers are 4 bytes, or `64` where pointers are 8 bytes.")
        {
            IsRequired = false
        };
        command.AddOption(bitOption);

        var clangArgsOption = new Option<IEnumerable<string>?>(
            new[] { "--clangArgs", "-a" },
            "Additional Clang arguments to use when parsing C code.")
        {
            IsRequired = false
        };
        command.AddOption(clangArgsOption);

        var whitelistFunctionsOption = new Option<string?>(
            new[] { "--whitelistFunctionsFile", "-w" },
            "The file path to a text file containing a set of function names delimited by new line. These functions will strictly only be considered for bindgen; this has implications for transitive types. Each function name may start with some text followed by a `!` character before the name of the function; this allows to re-use the same file for input to DirectPInvoke with NativeAOT.")
        {
            IsRequired = false
        };
        command.AddOption(whitelistFunctionsOption);

        command.Handler = CommandHandler.Create<
            string,
            string,
            bool?,
            IEnumerable<string?>?,
            IEnumerable<string?>?,
            IEnumerable<string?>?,
            IEnumerable<string?>?,
            int?,
            IEnumerable<string?>?,
            string?
        >(AbstractSyntaxTreeC);
        return command;
    }

    private static Command CommandBindgenCSharp()
    {
        var command = new Command("cs", "Generate C# bindings from a C abstract syntax tree `.json` file.");

        var inputFileOption = new Option<string>(
            new[] { "--inputFile", "-i" },
            "File path of the input abstract syntax tree `.json` file.")
        {
            IsRequired = true
        };
        command.AddOption(inputFileOption);

        var outputFileOption = new Option<string>(
            new[] { "--outputFile", "-o" },
            "File path of the output C# `.cs` file.")
        {
            IsRequired = true
        };
        command.AddOption(outputFileOption);

        var typeAliasesOption = new Option<IEnumerable<string?>?>(
            new[] { "--typeAliases", "-a" },
            "Types by name that will be remapped.")
        {
            IsRequired = false
        };
        command.AddOption(typeAliasesOption);

        var ignoredNamesFileOption = new Option<string>(
            new[] { "--ignoredNamesFile", "-g" },
            "File path of the text file with new-line separated names (types, functions, macros, etc) that will be ignored; types are ignored after remapping type names.")
        {
            IsRequired = false
        };
        command.AddOption(ignoredNamesFileOption);

        var libraryNameOption = new Option<string?>(
            new[] { "--libraryName", "-l" },
            "The name of the dynamic link library (without the file extension) used for P/Invoke with C#.")
        {
            IsRequired = false
        };
        command.AddOption(libraryNameOption);

        var classNameOption = new Option<string?>(
            new[] { "--className", "-c" },
            "The name of the C# static class.")
        {
            IsRequired = false
        };
        command.AddOption(classNameOption);

        var injectNamespacesOption = new Option<IEnumerable<string?>>(
            new[] { "--injectNamespaces", "-n" },
            "Additional namespaces to inject near the top of C# file as using statements.")
        {
            IsRequired = false
        };
        command.AddOption(injectNamespacesOption);

        var wrapNamespaceOption = new Option<string?>(
            new[] { "--wrapNamespace", "-w" },
            "The namespace to be used for C# static class. If not specified the C# static class does not have a namespace to which it is in the global namespace.")
        {
            IsRequired = false
        };
        command.AddOption(wrapNamespaceOption);

        command.Handler = CommandHandler.Create<
            string,
            string,
            IEnumerable<string?>?,
            string,
            string?,
            string?,
            IEnumerable<string?>?,
            string?
        >(BindgenCSharp);
        return command;
    }

    // NOTE: parameter name must match full name of command line option
    private static void AbstractSyntaxTreeC(
        string inputFile,
        string outputFile,
        bool? automaticallyFindSoftwareDevelopmentKit,
        IEnumerable<string?>? includeDirectories,
        IEnumerable<string?>? ignoredFiles,
        IEnumerable<string?>? opaqueTypes,
        IEnumerable<string?>? defines,
        int? bitness,
        IEnumerable<string?>? clangArgs,
        string? whitelistFunctionsFile)
    {
        var request = new UseCases.CExtractAbstractSyntaxTree.CExtractAbstractSyntaxTreeRequest(
            inputFile,
            outputFile,
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            ignoredFiles,
            opaqueTypes,
            defines,
            bitness,
            clangArgs,
            whitelistFunctionsFile);
        var useCase = new UseCases.CExtractAbstractSyntaxTree.CExtractAbstractSyntaxTreeUseCase();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }
    }

    private static void BindgenCSharp(
        string inputFile,
        string outputFile,
        IEnumerable<string?>? typeAliases,
        string ignoredNamesFile,
        string? libraryName,
        string? className,
        IEnumerable<string?>? injectNamespaces,
        string? wrapNamespace)
    {
        var libraryName2 = string.IsNullOrEmpty(libraryName) ? string.Empty : libraryName;
        var className2 = string.IsNullOrEmpty(className) ? string.Empty : className;

        var request = new CSharpBindgenRequest(
            inputFile,
            outputFile,
            typeAliases,
            ignoredNamesFile,
            libraryName2,
            className2,
            injectNamespaces,
            wrapNamespace);
        var useCase = new CSharpBindgenUseCase();
        var response = useCase.Execute(request);
        if (response.Status == UseCaseOutputStatus.Failure)
        {
            Environment.Exit(1);
        }
    }
}
