// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS;

public class BindgenTarget
{
    public ClassDeclarationSyntax Class { get; }

    public string ClassName { get; }

    public string WorkingDirectory { get; }

    public string ConfigurationFilePath { get; }

    public string OutputLogFilePath { get; }

    public BindgenTarget(ClassDeclarationSyntax @class, BindgenAttribute bindgenAttribute)
    {
        Class = @class;
        ClassName = @class.Identifier.ValueText;
        WorkingDirectory = GetWorkingDirectory(@class, bindgenAttribute);
        ConfigurationFilePath = GetConfigurationFileName(WorkingDirectory, ClassName, bindgenAttribute);
        OutputLogFilePath = GetOutputLogFileName(WorkingDirectory, ClassName, bindgenAttribute);
    }

    private static string GetWorkingDirectory(ClassDeclarationSyntax @class, BindgenAttribute attribute)
    {
        string result;

        if (string.IsNullOrEmpty(attribute.WorkingDirectory))
        {
            result = Path.GetDirectoryName(@class.SyntaxTree.FilePath) ?? string.Empty;
        }
        else
        {
            result = attribute.WorkingDirectory!;
        }

        if (string.IsNullOrEmpty(result))
        {
            result = Environment.CurrentDirectory;
        }

        var info = Directory.CreateDirectory(result);
        return info.Exists ? info.FullName : string.Empty;
    }

    private static string GetConfigurationFileName(
        string workingDirectory,
        string className,
        BindgenAttribute attribute)
    {
        var fileName = !string.IsNullOrEmpty(attribute.ConfigurationFileName) ?
            attribute.ConfigurationFileName : $"{className}.json";
        return Path.Combine(workingDirectory, fileName);
    }

    private static string GetOutputLogFileName(
        string workingDirectory,
        string className,
        BindgenAttribute attribute)
    {
        var fileName = !string.IsNullOrEmpty(attribute.OutputLogFileName) ?
            attribute.OutputLogFileName : $"{className}.log";
        return Path.Combine(workingDirectory, fileName);
    }
}
