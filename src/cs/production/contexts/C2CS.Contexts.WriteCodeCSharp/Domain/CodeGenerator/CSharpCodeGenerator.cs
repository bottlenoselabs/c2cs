// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Foundation.UseCases.Exceptions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class CSharpCodeGenerator
{
    private readonly string _className;
    private readonly string _libraryName;
    private readonly string _namespaceName;
    private readonly string _headerCodeRegion;
    private readonly string _footerCodeRegion;

    public CSharpCodeGenerator(
        string className,
        string libraryName,
        string namespaceName,
        string headerCodeRegion,
        string footerCodeRegion)
    {
        _className = className;
        _libraryName = libraryName;
        _namespaceName = namespaceName;
        _headerCodeRegion = headerCodeRegion;
        _footerCodeRegion = footerCodeRegion;
    }

    public string EmitCode(CSharpAbstractSyntaxTree abstractSyntaxTree)
    {
        var members = new List<MemberDeclarationSyntax>();

        var sharedNodes = abstractSyntaxTree.PlatformAgnosticNodes;
        EmitNodes(sharedNodes, members);

        var platformSpecificNodes = abstractSyntaxTree.PlatformSpecificNodes;
        if (!platformSpecificNodes.IsDefaultOrEmpty)
        {
            foreach (var (platform, nodes) in platformSpecificNodes)
            {
                var platformSpecificMembers = new List<MemberDeclarationSyntax>();
                EmitNodes(nodes, platformSpecificMembers);

                var platformSpecificClassName = platform.ToString().Replace("-", "_", StringComparison.InvariantCulture);
                var platformSpecificClass = ClassDeclaration(platformSpecificClassName)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddMembers(platformSpecificMembers.ToArray());

                members.Add(platformSpecificClass);
            }
        }

        var compilationUnit = CompilationUnit(
            _className,
            _libraryName,
            _namespaceName,
            _headerCodeRegion,
            _footerCodeRegion,
            members.ToArray());

        var code = compilationUnit.ToFullString().Trim();
        return code;
    }

    private void EmitNodes(CSharpNodes nodes, List<MemberDeclarationSyntax> members)
    {
        var membersApi = new List<MemberDeclarationSyntax>();
        var membersTypes = new List<MemberDeclarationSyntax>();

        FunctionExterns(membersApi, nodes.Functions);
        if (membersApi.Count > 0)
        {
            membersApi[0] = membersApi[0].AddRegionStart("API", false);
            membersApi[^1] = membersApi[^1].AddRegionEnd();
        }

        FunctionPointers(membersTypes, nodes.FunctionPointers);
        Structs(membersTypes, nodes.Structs);
        OpaqueDataTypes(membersTypes, nodes.OpaqueStructs);
        Typedefs(membersTypes, nodes.AliasStructs);
        Enums(membersTypes, nodes.Enums);
        MacroObjects(membersTypes, nodes.MacroObjects);
        EnumConstants(membersTypes, nodes.EnumConstants);

        if (membersTypes.Count > 0)
        {
            membersTypes[0] = membersTypes[0].AddRegionStart("Types", false);
            membersTypes[^1] = membersTypes[^1].AddRegionEnd();
        }

        members.AddRange(membersApi);
        members.AddRange(membersTypes);
    }

    private static CompilationUnitSyntax CompilationUnit(
        string className,
        string libraryName,
        string namespaceName,
        string headerCodeRegion,
        string footerCodeRegion,
        MemberDeclarationSyntax[] members)
    {
        var code = CompilationUnitTemplateCode(
            className, libraryName, namespaceName, headerCodeRegion, footerCodeRegion);
        var syntaxTree = ParseSyntaxTree(code);
        var compilationUnit = syntaxTree.GetCompilationUnitRoot();
        var namespaceDeclaration = (NamespaceDeclarationSyntax)compilationUnit.Members[0];
        var classDeclaration = (ClassDeclarationSyntax)namespaceDeclaration.Members[0];
        var runtimeClassDeclaration = RuntimeClass();

        var classDeclarationWithMembers = classDeclaration
            .AddMembers(members)
            .AddMembers(runtimeClassDeclaration);

        var newCompilationUnit = compilationUnit.ReplaceNode(classDeclaration, classDeclarationWithMembers);
        using var workspace = new AdhocWorkspace();
        var newCompilationUnitFormatted = (CompilationUnitSyntax)Formatter.Format(newCompilationUnit, workspace);
        return newCompilationUnitFormatted;
    }

    private static ClassDeclarationSyntax RuntimeClass()
    {
        var builderMembers = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        var assembly = typeof(CBool).Assembly;
        var assemblyName = assembly.GetName().Name!;
        var manifestResourcesNames = assembly.GetManifestResourceNames();
        foreach (var resourceName in manifestResourcesNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var streamReader = new StreamReader(stream!);
            var code = streamReader.ReadToEnd();
            var syntaxTree = ParseSyntaxTree(code);
            var compilationUnit = (CompilationUnitSyntax)syntaxTree.GetRoot();
            foreach (var member in compilationUnit.Members)
            {
                builderMembers.Add(member);
            }
        }

        var members = builderMembers.ToArray();

        const string runtimeClassName = "Runtime";
        var runtimeClass = ClassDeclaration(runtimeClassName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
            .AddMembers(members)
            .AddRegionStart(runtimeClassName, true)
            .AddRegionEnd();
        return runtimeClass;
    }

    private static string CompilationUnitTemplateCode(
        string className,
        string libraryName,
        string namespaceName,
        string headerCodeRegion,
        string footerCodeRegion)
    {
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss \"GMT\"zzz", CultureInfo.InvariantCulture);
        var version = Assembly.GetEntryAssembly()!.GetName().Version;
        var code = $@"
// <auto-generated>
//  This code was generated by the following tool on {dateTime}:
//      https://github.com/bottlenoselabs/c2cs (v{version})
//      
//  Changes to this file may cause incorrect behavior and will be lost if the code is
//      regenerated. To extend or add functionality use a partial class in a new file.
// </auto-generated>
// ReSharper disable All

#nullable enable
#pragma warning disable 1591
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using static {namespaceName}.{className}.Runtime;
{headerCodeRegion}
namespace {namespaceName}
{{
    public static unsafe partial class {className}
    {{
        private const string LibraryName = ""{libraryName}"";
    }}
}}
{footerCodeRegion}
";
        return code;
    }

    private void FunctionExterns(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpFunction> functionExterns)
    {
        foreach (var functionExtern in functionExterns)
        {
            var member = FunctionExtern(functionExtern);
            members.Add(member);
        }
    }

    private MethodDeclarationSyntax FunctionExtern(CSharpFunction function)
    {
        var callingConvention = function.CallingConvention switch
        {
            CSharpFunctionCallingConvention.Cdecl => "CallingConvention = CallingConvention.Cdecl",
            CSharpFunctionCallingConvention.StdCall => "CallingConvention = CallingConvention.StdCall",
            CSharpFunctionCallingConvention.FastCall => "CallingConvention = CallingConvention.FastCall",
            _ => string.Empty,
        };
        var dllImportParameters = string.Join(',', "LibraryName", callingConvention);

        var parameterStrings = function.Parameters.Select(
            x => $@"{x.TypeName} {x.Name}");
        var parameters = string.Join(',', parameterStrings);

        var code = $@"
{function.CodeLocationComment}
[DllImport({dllImportParameters})]
public static extern {function.ReturnType.Name} {function.Name}({parameters});
";

        var member = ParseMemberCode<MethodDeclarationSyntax>(code);
        return member;
    }

    private void FunctionPointers(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpFunctionPointer> functionPointers)
    {
        if (functionPointers.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var functionPointer in functionPointers)
        {
            var member = FunctionPointer(functionPointer);
            members.Add(member);
        }
    }

    private StructDeclarationSyntax FunctionPointer(
        CSharpFunctionPointer functionPointer, bool isNested = false)
    {
        var parameterStrings = functionPointer.Parameters
            .Select(x => $"{x.Type}")
            .Append($"{functionPointer.ReturnType.Name}");
        var parameters = string.Join(',', parameterStrings);
        var functionPointerName = functionPointer.Name;

        var code = $@"
{functionPointer.CodeLocationComment}
[StructLayout(LayoutKind.Sequential)]
public struct {functionPointerName}
{{
	public delegate* unmanaged <{parameters}> Pointer;
}}
";

        if (isNested)
        {
            code = code.Trim();
        }

        var member = ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }

    private void Structs(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpStruct> structs)
    {
        if (structs.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var @struct in structs)
        {
            var member = Struct(@struct);
            members.Add(member);
        }
    }

    private StructDeclarationSyntax Struct(CSharpStruct @struct, bool isNested = false)
    {
        var memberSyntaxes = StructMembers(
            @struct.Name, @struct.Fields, @struct.NestedStructs);
        var memberStrings = memberSyntaxes.Select(x => x.ToFullString());
        var members = string.Join("\n\n", memberStrings);

        var code = $@"
{@struct.CodeLocationComment}
[StructLayout(LayoutKind.Explicit, Size = {@struct.SizeOf}, Pack = {@struct.AlignOf})]
public struct {@struct.Name}
{{
	{members}
}}
";

        if (isNested)
        {
            code = code.Trim();
        }

        var member = ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }

    private MemberDeclarationSyntax[] StructMembers(
        string structName,
        ImmutableArray<CSharpStructField> fields,
        ImmutableArray<CSharpStruct> nestedStructs)
    {
        var builder = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();

        StructFields(structName, fields, builder);

        foreach (var nestedStruct in nestedStructs)
        {
            var syntax = Struct(nestedStruct, true);
            builder.Add(syntax);
        }

        var structMembers = builder.ToArray();
        return structMembers;
    }

    private void StructFields(
        string structName, ImmutableArray<CSharpStructField> fields, ImmutableArray<MemberDeclarationSyntax>.Builder builder)
    {
        foreach (var field in fields)
        {
            if (field.Type.IsArray)
            {
                var fieldMember = EmitStructFieldFixedBuffer(field);
                builder.Add(fieldMember);

                var methodMember = StructFieldFixedBufferProperty(
                    structName, field);
                builder.Add(methodMember);
            }
            else
            {
                var fieldMember = StructField(field);
                builder.Add(fieldMember);
            }
        }
    }

    private static FieldDeclarationSyntax StructField(CSharpStructField field)
    {
        var code = $@"
[FieldOffset({field.Offset})] // size = {field.Type.SizeOf}, padding = {field.Padding}
public {field.Type.Name} {field.Name};
".Trim();

        var member = ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }

    private static FieldDeclarationSyntax EmitStructFieldFixedBuffer(
        CSharpStructField field)
    {
        var code = $@"
[FieldOffset({field.Offset})] // size = {field.Type.SizeOf}, padding = {field.Padding}
public fixed byte {field.BackingFieldName}[{field.Type.SizeOf}]; // {field.Type.OriginalName}
".Trim();

        return ParseMemberCode<FieldDeclarationSyntax>(code);
    }

    private PropertyDeclarationSyntax StructFieldFixedBufferProperty(
        string structName,
        CSharpStructField field)
    {
        string code;

        if (field.Type.Name == "CString")
        {
            code = $@"
public string {field.Name}
{{
	get
	{{
		fixed ({structName}*@this = &this)
		{{
			var pointer = &@this->{field.BackingFieldName}[0];
            var cString = new CString(pointer);
            return Runtime.CStrings.String(cString);
		}}
	}}
}}
".Trim();
        }
        else if (field.Type.Name == "CStringWide")
        {
            code = $@"
public string {field.Name}
{{
	get
	{{
		fixed ({structName}*@this = &this)
		{{
			var pointer = &@this->{field.BackingFieldName}[0];
            var cString = new CStringWide(pointer);
            return Runtime.CStrings.StringWide(cString);
		}}
	}}
}}
".Trim();
        }
        else
        {
            var fieldTypeName = field.Type.Name;
            var elementType = fieldTypeName[..^1];
            if (elementType.EndsWith('*'))
            {
                // ReSharper disable once StringLiteralTypo
                elementType = "nint";
            }

            code = $@"
public Span<{elementType}> {field.Name}
{{
	get
	{{
		fixed ({structName}*@this = &this)
		{{
			var pointer = &@this->{field.BackingFieldName}[0];
			var span = new Span<{elementType}>(pointer, {field.Type.ArraySizeOf});
			return span;
		}}
	}}
}}
".Trim();
        }

        return ParseMemberCode<PropertyDeclarationSyntax>(code);
    }

    private static void OpaqueDataTypes(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpOpaqueStruct> opaqueDataTypes)
    {
        if (opaqueDataTypes.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var opaqueDataType in opaqueDataTypes)
        {
            var member = EmitOpaqueStruct(opaqueDataType);
            members.Add(member);
        }
    }

    private static StructDeclarationSyntax EmitOpaqueStruct(CSharpOpaqueStruct opaqueStruct)
    {
        var code = $@"
{opaqueStruct.CodeLocationComment}
[StructLayout(LayoutKind.Sequential)]
public struct {opaqueStruct.Name}
{{
}}
";

        return ParseMemberCode<StructDeclarationSyntax>(code);
    }

    private static void Typedefs(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpAliasStruct> typedefs)
    {
        if (typedefs.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var typedef in typedefs)
        {
            var member = Typedef(typedef);
            members.Add(member);
        }
    }

    private static StructDeclarationSyntax Typedef(CSharpAliasStruct aliasStruct)
    {
        var code = $@"
{aliasStruct.CodeLocationComment}
[StructLayout(LayoutKind.Explicit, Size = {aliasStruct.UnderlyingType.SizeOf}, Pack = {aliasStruct.UnderlyingType.AlignOf})]
public struct {aliasStruct.Name}
{{
	[FieldOffset(0)] // size = {aliasStruct.UnderlyingType.SizeOf}, padding = 0
    public {aliasStruct.UnderlyingType.Name} Data;

	public static implicit operator {aliasStruct.UnderlyingType.Name}({aliasStruct.Name} data) => data.Data;
	public static implicit operator {aliasStruct.Name}({aliasStruct.UnderlyingType.Name} data) => new() {{Data = data}};
}}
";

        var member = ParseMemberCode<StructDeclarationSyntax>(code);
        return member;
    }

    private static void Enums(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpEnum> enums)
    {
        if (enums.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var @enum in enums)
        {
            var member = Enum(@enum);
            members.Add(member);
        }
    }

    private static EnumDeclarationSyntax Enum(CSharpEnum @enum)
    {
        var enumName = @enum.Name;
        var enumSizeOf = @enum.SizeOf!.Value;
        var enumIntegerTypeName = enumSizeOf switch
        {
            1 => "sbyte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new NotImplementedException($"The enum size is not supported: '{enumName}' of size {@enum.SizeOf}.")
        };

        var values = EnumValues(enumIntegerTypeName, @enum.Values);
        var valuesString = values.Select(x => x.ToFullString());
        var members = string.Join(",\n", valuesString);

        var code = $@"
{@enum.CodeLocationComment}
public enum {enumName} : {enumIntegerTypeName}
    {{
        {members}
    }}
";

        var member = ParseMemberCode<EnumDeclarationSyntax>(code);
        return member;
    }

    private static EnumMemberDeclarationSyntax[] EnumValues(
        string enumIntegerTypeName, ImmutableArray<CSharpEnumValue> values)
    {
        var builder = ImmutableArray.CreateBuilder<EnumMemberDeclarationSyntax>(values.Length);

        foreach (var value in values)
        {
            var enumEqualsValue = EmitEnumEqualsValue(value.Value, enumIntegerTypeName);
            var member = EnumMemberDeclaration(value.Name)
                .WithEqualsValue(enumEqualsValue);

            builder.Add(member);
        }

        return builder.ToArray();
    }

    private static EqualsValueClauseSyntax EmitEnumEqualsValue(long value, string enumIntegerTypeName)
    {
        var literalToken = enumIntegerTypeName switch
        {
            "sbyte" => Literal((sbyte)value),
            "short" => Literal((short)value),
            "int" => Literal((int)value),
            "long" => Literal(value),
            _ => throw new NotImplementedException($"The enum integer type name is not supported: {enumIntegerTypeName}.")
        };

        return EqualsValueClause(LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken));
    }

    private static void MacroObjects(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpMacroObject> macroObjects)
    {
        if (macroObjects.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var macroObject in macroObjects)
        {
            var field = MacroObject(macroObject);
            members.Add(field);
        }
    }

    private static FieldDeclarationSyntax MacroObject(CSharpMacroObject macroObject)
    {
        string code;
        if (macroObject.IsConstant)
        {
            code = $@"
{macroObject.CodeLocationComment}
public const {macroObject.Type} {macroObject.Name} = {macroObject.Value};
";
        }
        else
        {
            code = $@"
{macroObject.CodeLocationComment}
public static {macroObject.Type} {macroObject.Name} = {macroObject.Value};
";
        }

        var member = ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }

    private void EnumConstants(
        List<MemberDeclarationSyntax> members,
        ImmutableArray<CSharpEnumConstant> enumConstants)
    {
        if (enumConstants.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var enumConstant in enumConstants)
        {
            var field = EnumConstant(enumConstant);
            members.Add(field);
        }
    }

    private static FieldDeclarationSyntax EnumConstant(CSharpEnumConstant enumConstant)
    {
        var code = $@"
{enumConstant.CodeLocationComment}
public const {enumConstant.Type} {enumConstant.Name} = {enumConstant.Value};
";

        var member = ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }

    private static T ParseMemberCode<T>(string memberCode)
        where T : MemberDeclarationSyntax
    {
        var member = ParseMemberDeclaration(memberCode)!;
        if (member is T syntax)
        {
            return syntax;
        }

        var up = new UseCaseException($"Error generating C# code for {typeof(T).Name}.");
        throw up;
    }
}