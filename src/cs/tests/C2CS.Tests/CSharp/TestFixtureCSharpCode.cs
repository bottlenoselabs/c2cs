// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Tests.CSharp.Data.Models;
using C2CS.Tests.Foundation.CMake;
using C2CS.Tests.Foundation.Roslyn;
using Xunit;

namespace C2CS.Tests.CSharp;

public class TestFixtureCSharpCode
{
    private readonly TestCSharpCodeAbstractSyntaxTree _abstractSyntaxTree;
    private readonly CCodeBuildResult _cCodeBuildResult;
    private readonly CSharpCodeBuildResult _cSharpCodeBuildResult;

    public TestFixtureCSharpCode(
        TestCSharpCodeAbstractSyntaxTree abstractSyntaxTree,
        CSharpCodeBuildResult cSharpCodeBuildResult,
        CCodeBuildResult cCodeBuildResult)
    {
        _abstractSyntaxTree = abstractSyntaxTree;
        _cSharpCodeBuildResult = cSharpCodeBuildResult;
        _cCodeBuildResult = cCodeBuildResult;
    }

    public void AssertCompiles()
    {
        Assert.True(_cCodeBuildResult.IsSuccess, $"C library did not compile successfully for purposes of P/Invoke from C#.");

        var emitResult = _cSharpCodeBuildResult.EmitResult;

        foreach (var diagnostic in emitResult.Diagnostics)
        {
            var isWarningOrError = diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Warning &&
                                   diagnostic.Severity != Microsoft.CodeAnalysis.DiagnosticSeverity.Error;
            Assert.True(isWarningOrError, $"C# code compilation diagnostic: {diagnostic}.");
        }

        Assert.True(emitResult.Success, "C# code did not compile successfully.");
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _abstractSyntaxTree.Enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");

        var enumType = _cSharpCodeBuildResult.ClassType.GetNestedType(name);
        Assert.True(enumType != null, $"The enum type `{name}` does not exist.");
        var enumValues = enumType!.GetEnumValues();

        var enumPrintMethodName = $"{name}__print_{name}";
        var enumPrintMethod = _cSharpCodeBuildResult.ClassType.GetMethod(enumPrintMethodName);
        Assert.True(enumPrintMethod != null, $"The enum method `{enumPrintMethodName}` does not exist.");
        foreach (var enumValue in enumValues)
        {
            var enumPrintMethodResult = enumPrintMethod!.Invoke(null, new[] { enumValue });
            Assert.True(enumPrintMethodResult == null, $"Unexpected result from enum print method `{enumPrintMethodName}`");
        }

        var enumReturnMethodName = $"{name}__return_{name}";
        var enumReturnMethod = _cSharpCodeBuildResult.ClassType.GetMethod(enumReturnMethodName);
        Assert.True(enumReturnMethod != null, $"The enum method `{enumReturnMethodName}` does not exist.");

        foreach (var enumValue in enumValues)
        {
            var enumReturnMethodResult = enumReturnMethod!.Invoke(null, new[] { enumValue });
            Assert.True(enumReturnMethodResult!.Equals(enumValue), $"Unexpected result from enum return method `{enumReturnMethodName}`");
        }

        return value!;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _abstractSyntaxTree.Methods.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CSharpTestStruct GetStruct(string name)
    {
        var exists = _abstractSyntaxTree.Structs.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        return value!;
    }

    public CSharpTestMacroObject GetMacroObject(string name)
    {
        var exists = _abstractSyntaxTree.MacroObjects.TryGetValue(name, out var value);
        Assert.True(exists, $"The macro object `{name}` does not exist.");
        return value!;
    }
}
