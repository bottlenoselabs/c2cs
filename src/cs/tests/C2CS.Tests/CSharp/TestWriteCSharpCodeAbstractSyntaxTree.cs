using System.Collections.Immutable;
using C2CS.Tests.CSharp.Data.Models;

namespace C2CS.Tests.CSharp;

public sealed class TestWriteCSharpCodeAbstractSyntaxTree
{
    public readonly ImmutableDictionary<string, CSharpTestEnum> Enums;
    public readonly ImmutableDictionary<string, CSharpTestFunction> Methods;
    public readonly ImmutableDictionary<string, CSharpTestMacroObject> MacroObjects;
    public readonly ImmutableDictionary<string, CSharpTestStruct> Structs;

    public TestWriteCSharpCodeAbstractSyntaxTree(
        ImmutableDictionary<string, CSharpTestEnum> enums,
        ImmutableDictionary<string, CSharpTestFunction> methods,
        ImmutableDictionary<string, CSharpTestMacroObject> macroObjects,
        ImmutableDictionary<string, CSharpTestStruct> structs)
    {
        Enums = enums;
        Methods = methods;
        MacroObjects = macroObjects;
        Structs = structs;
    }
}
