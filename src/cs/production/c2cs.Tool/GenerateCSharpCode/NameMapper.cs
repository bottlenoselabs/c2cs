// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using c2ffi.Data;
using c2ffi.Data.Nodes;

namespace C2CS.GenerateCSharpCode;

public sealed class NameMapper
{
    private readonly Dictionary<string, string> _cSharpNamesByCNames = [];

    private static readonly char[] IdentifierSeparatorCharacters = ['_', '.', '@'];

#pragma warning disable IDE0060
    public NameMapper(CodeGeneratorContext context)
#pragma warning restore IDE0060
    {
        // config specified
        foreach (var (source, target) in context.Input.MappedNames)
        {
            _cSharpNamesByCNames[source] = target;
        }

        // C types -> C# Interop.Runtime types
        _ = _cSharpNamesByCNames.TryAdd("char*", "CString");
        _ = _cSharpNamesByCNames.TryAdd("wchar_t*", "CStringWide");
        _ = _cSharpNamesByCNames.TryAdd("char", "CChar");
        _ = _cSharpNamesByCNames.TryAdd("bool", "CBool");
        _ = _cSharpNamesByCNames.TryAdd("_Bool", "CBool");

        // C types -> C# native CLR types
        _ = _cSharpNamesByCNames.TryAdd("int8_t", "sbyte");
        _ = _cSharpNamesByCNames.TryAdd("uint8_t", "byte");
        _ = _cSharpNamesByCNames.TryAdd("int16_t", "short");
        _ = _cSharpNamesByCNames.TryAdd("uint16_t", "ushort");
        _ = _cSharpNamesByCNames.TryAdd("int32_t", "int");
        _ = _cSharpNamesByCNames.TryAdd("uint32_t", "uint");
        _ = _cSharpNamesByCNames.TryAdd("int64_t", "long");
        _ = _cSharpNamesByCNames.TryAdd("uint64_t", "ulong");
        _ = _cSharpNamesByCNames.TryAdd("intptr_t", "IntPtr");
        _ = _cSharpNamesByCNames.TryAdd("uintptr_t", "UIntPtr");

        // C types -> C# opaque pointers
        _ = _cSharpNamesByCNames.TryAdd("FILE*", "IntPtr");
        _ = _cSharpNamesByCNames.TryAdd("DIR*", "IntPtr");
        _ = _cSharpNamesByCNames.TryAdd("va_list", "IntPtr");
    }

    public string GetIdentifierCSharp(string nameC)
    {
        return nameC switch
        {
            "abstract"
                or "as"
                or "base"
                or "bool"
                or "break"
                or "byte"
                or "case"
                or "catch"
                or "char"
                or "checked"
                or "class"
                or "const"
                or "continue"
                or "decimal"
                or "default"
                or "delegate"
                or "do"
                or "double"
                or "else"
                or "enum"
                or "event"
                or "explicit"
                or "extern"
                or "false"
                or "finally"
                or "fixed"
                or "float"
                or "for"
                or "foreach"
                or "goto"
                or "if"
                or "implicit"
                or "in"
                or "int"
                or "interface"
                or "internal"
                or "is"
                or "lock"
                or "long"
                or "namespace"
                or "new"
                or "null"
                or "object"
                or "operator"
                or "out"
                or "override"
                or "params"
                or "private"
                or "protected"
                or "public"
                or "readonly"
                or "record"
                or "ref"
                or "return"
                or "sbyte"
                or "sealed"
                or "short"
                or "sizeof"
                or "stackalloc"
                or "static"
                or "string"
                or "struct"
                or "switch"
                or "this"
                or "throw"
                or "true"
                or "try"
                or "typeof"
                or "uint"
                or "ulong"
                or "unchecked"
                or "unsafe"
                or "ushort"
                or "using"
                or "virtual"
                or "void"
                or "volatile"
                or "while" => $"@{nameC}",
            _ => nameC
        };
    }

    public string GetNodeNameCSharp(CNode nodeC)
    {
        var nameC = SanitizeNameC(nodeC.Name);
        if (_cSharpNamesByCNames.TryGetValue(nameC, out var nameCSharp))
        {
            return nameCSharp;
        }

#pragma warning disable IDE0045
        if (nodeC is CFunctionPointer functionPointerC)
#pragma warning restore IDE0045
        {
            nameCSharp = GetFunctionPointerNameCSharp(functionPointerC);
        }
        else
        {
            nameCSharp = nameC;
        }

        _cSharpNamesByCNames.Add(nameC, nameCSharp);
        return nameCSharp;
    }

    public string GetTypeNameCSharp(CType type)
    {
        var nameC = type.Name;

        // Try to map the name without sanitizing first in the case of config specified
        if (_cSharpNamesByCNames.TryGetValue(nameC, out var typeNameCSharp))
        {
            return typeNameCSharp;
        }

        nameC = SanitizeNameC(type.Name);
        if (_cSharpNamesByCNames.TryGetValue(nameC, out typeNameCSharp))
        {
            return typeNameCSharp;
        }

        if (type.NodeKind is CNodeKind.Pointer or CNodeKind.Array)
        {
            typeNameCSharp = GetTypeNameCSharpPointerOrArray(nameC, type.InnerType);
        }
        else if (type.NodeKind is CNodeKind.FunctionPointer)
        {
            throw new NotImplementedException();
        }
        else
        {
            var forceUnsigned = type.NodeKind == CNodeKind.EnumValue;
            typeNameCSharp = GetTypeNameCSharp(nameC, type.SizeOf ?? 0, forceUnsigned);
        }

        _cSharpNamesByCNames.Add(nameC, typeNameCSharp);

        return typeNameCSharp;
    }

    private string GetFunctionPointerNameCSharp(CFunctionPointer functionPointer)
    {
        var functionPointerNameCSharp = $"FnPtr_{ParameterStringsCSharp()}_{ReturnTypeNameCSharp()}"
            .Replace("__", "_", StringComparison.InvariantCulture)
            .Replace(".", string.Empty, StringComparison.InvariantCulture);
        return functionPointerNameCSharp;

        string ReturnTypeNameCSharp()
        {
            var typeC = functionPointer.ReturnType;
            var typeNameCSharp = GetTypeNameCSharp(typeC)
                .Replace("*", "Ptr", StringComparison.InvariantCulture);
            var typeNameCSharpParts = typeNameCSharp.Split(IdentifierSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);
            var typeNameCSharpPartsCapitalized = typeNameCSharpParts.Select(x =>
                char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..]);
            var result = string.Join(string.Empty, typeNameCSharpPartsCapitalized);
            return result;
        }

        string ParameterStringsCSharp()
        {
            var parameterStringsCSharp = new List<string>();
            foreach (var parameter in functionPointer.Parameters)
            {
                var typeC = parameter.Type;
                var typeNameCSharp = GetTypeNameCSharp(typeC).Replace("*", "Ptr", StringComparison.InvariantCulture);
                var typeNameCSharpParts = typeNameCSharp.Split(IdentifierSeparatorCharacters, StringSplitOptions.RemoveEmptyEntries);

                var typeNameCSharpPartsCapitalized = typeNameCSharpParts.Select(x =>
                    char.ToUpper(x[0], CultureInfo.InvariantCulture) + x[1..]);
                var typeNameParameter = string.Join(string.Empty, typeNameCSharpPartsCapitalized);
                parameterStringsCSharp.Add(typeNameParameter);
            }

            var result = string.Join('_', parameterStringsCSharp);
            return result;
        }
    }

    private string GetTypeNameCSharpPointerOrArray(string typeName, CType? innerType)
    {
        var pointerTypeName = typeName;

        // Replace [] with *
        while (true)
        {
            var x = pointerTypeName.IndexOf('[', StringComparison.InvariantCulture);

            if (x == -1)
            {
                break;
            }

            var y = pointerTypeName.IndexOf(']', x);

            pointerTypeName = pointerTypeName[..x] + "*" + pointerTypeName[(y + 1)..];
        }

        var elementTypeName = pointerTypeName[..^1];
        var pointersTypeName = pointerTypeName[elementTypeName.Length..];
        if (elementTypeName.Length == 0)
        {
            return "void" + pointersTypeName;
        }

        if (innerType == null)
        {
            return "void*";
        }

        var elementTypeNameCSharp = GetTypeNameCSharp(innerType);
        var result = elementTypeNameCSharp + pointersTypeName;
        return result;
    }

    private string GetTypeNameCSharp(
        string typeNameC,
        int? sizeOf = null,
        bool forceUnsignedInteger = false)
    {
        // if (_userTypeNameAliases.TryGetValue(typeName, out var aliasName))
        // {
        //     return aliasName;
        // }
        //
        // if (_options.SystemTypeAliases.TryGetValue(typeName, out var mappedSystemTypeName))
        // {
        //     return mappedSystemTypeName;
        // }

        switch (typeNameC)
        {
            case "unsigned char":
            case "unsigned short":
            case "unsigned short int":
            case "unsigned":
            case "unsigned int":
            case "unsigned long":
            case "unsigned long int":
            case "unsigned long long":
            case "unsigned long long int":
            case "size_t":
                return TypeNameMapUnsignedInteger(sizeOf!.Value);

            case "signed char":
            case "short":
            case "short int":
            case "signed short":
            case "signed short int":
            case "int":
            case "signed":
            case "signed int":
            case "long":
            case "long int":
            case "signed long":
            case "signed long int":
            case "long long":
            case "long long int":
            case "signed long long int":
            case "ssize_t":
                if (forceUnsignedInteger)
                {
                    return TypeNameMapUnsignedInteger(sizeOf!.Value);
                }

                return TypeNameMapSignedInteger(sizeOf!.Value);

            case "float":
            case "double":
            case "long double":
                return TypeNameMapFloatingPoint(sizeOf!.Value);

            default:
                return typeNameC;
        }
    }

    private static string TypeNameMapUnsignedInteger(int sizeOf)
    {
        return sizeOf switch
        {
            1 => "byte",
            2 => "ushort",
            4 => "uint",
            8 => "ulong",
            _ => throw new InvalidOperationException()
        };
    }

    private static string TypeNameMapSignedInteger(int sizeOf)
    {
        return sizeOf switch
        {
            1 => "sbyte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new InvalidOperationException()
        };
    }

    private string TypeNameMapFloatingPoint(int sizeOf)
    {
        return sizeOf switch
        {
            4 => "float",
            8 => "double",
            16 => "decimal",
            _ => throw new InvalidOperationException()
        };
    }

    private static string SanitizeNameC(string nameC)
    {
        var result = nameC;

        if (result.Contains("const ", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("const ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains("const", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("const", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains("enum ", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("enum ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains("struct ", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("struct ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains("union ", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("union ", string.Empty, StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains("* ", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace("* ", "*", StringComparison.InvariantCultureIgnoreCase);
        }

        if (result.Contains(" *", StringComparison.InvariantCultureIgnoreCase))
        {
            result = result.Replace(" *", "*", StringComparison.InvariantCultureIgnoreCase);
        }

        return result;
    }
}
