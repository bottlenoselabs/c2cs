// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Foundation.Tool;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class CSharpCodeGeneratorContext
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly ImmutableDictionary<Type, GenerateCodeHandler> _handlers;

    public CSharpCodeGeneratorOptions Options { get; }

    public CSharpCodeGeneratorContext(
        ImmutableDictionary<Type, GenerateCodeHandler> handlers,
        CSharpCodeGeneratorOptions options)
    {
        _handlers = handlers;
        Options = options;
    }

    public T ParseMemberCode<T>(string code)
        where T : MemberDeclarationSyntax
    {
        var member = SyntaxFactory.ParseMemberDeclaration(code)!;
        if (member is T syntax)
        {
            return syntax;
        }

        var up = new ToolException($"Error generating C# code for {typeof(T).Name}.");
        throw up;
    }

    public MemberDeclarationSyntax GenerateCodeMemberSyntax<T>(T node)
        where T : CSharpNode
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var handler))
        {
            throw new ToolException($"A handler '{nameof(GenerateCodeHandler)}' does not exist for the type '{type.FullName ?? type.Name}'.");
        }

        var syntax = handler.GenerateCode(this, node);
        if (syntax is not MemberDeclarationSyntax memberSyntax)
        {
            throw new ToolException($"The handler '{nameof(GenerateCodeHandler)}' did not return a '{nameof(MemberDeclarationSyntax)}' for the type '{type.FullName ?? type.Name}'.");
        }

        return memberSyntax;
    }

    public string GenerateCodeParameters(ImmutableArray<CSharpParameter> parameters, bool includeNames = true)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var attributes = GenerateCodeAttributes(parameter.Attributes);

            if (!string.IsNullOrWhiteSpace(attributes))
            {
                _stringBuilder.Append(attributes);
                _stringBuilder.Append(' ');
            }

            if (string.IsNullOrEmpty(parameter.TypeInfo.ClassName))
            {
                _stringBuilder.Append(parameter.TypeInfo.Name);
            }
            else
            {
                _stringBuilder.Append(parameter.TypeInfo.ClassName + "." + parameter.TypeInfo.Name);
            }

            if (includeNames)
            {
                _stringBuilder.Append(' ');
                _stringBuilder.Append(parameter.Name);
            }

            var isJoinedWithComma = parameters.Length > 1 && i != parameters.Length - 1;
            if (isJoinedWithComma)
            {
                _stringBuilder.Append(',');
            }
        }

        var result = _stringBuilder.ToString();
        _stringBuilder.Clear();
        return result;
    }

    public string GenerateCodeAttributes(ImmutableArray<Attribute> attributes)
    {
        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            var attributeSyntax = Attribute(attribute);
            _stringBuilder.Append('[');
            _stringBuilder.Append(attributeSyntax.ToFullString());
            _stringBuilder.Append(']');

            if (i != attributes.Length - 1)
            {
                _stringBuilder.AppendLine();
            }
        }

        var result = _stringBuilder.ToString();
        _stringBuilder.Clear();
        return result;
    }

    private AttributeSyntax Attribute(Attribute attribute)
    {
        var attributeType = attribute.GetType();
        var attributeName = attributeType.Name.Replace("Attribute", string.Empty, StringComparison.InvariantCulture);
        var attributeNameSyntax = SyntaxFactory.ParseName(attributeName);

        var builderAttributeArgumentSyntax = new List<AttributeArgumentSyntax>();

        var attributeProperties = attributeType.GetProperties();
        foreach (var propertyInfo in attributeProperties)
        {
            if (propertyInfo.Name == "TypeId")
            {
                continue;
            }

            var propertyValue = propertyInfo.GetValue(attribute);
            string expression;

            if (propertyInfo.PropertyType == typeof(string))
            {
                var propertyValueString = propertyValue as string;
                if (string.IsNullOrEmpty(propertyValueString))
                {
                    continue;
                }

                var propertyValueStringEscaped = SymbolDisplay.FormatLiteral(propertyValueString, true);
                expression = $"{propertyInfo.Name} = {propertyValueStringEscaped}";
            }
            else
            {
                throw new NotImplementedException();
            }

            var attributeArgumentSyntax = SyntaxFactory.AttributeArgument(
                SyntaxFactory.ParseExpression(expression));

            builderAttributeArgumentSyntax.Add(attributeArgumentSyntax);
        }

        var attributeArgumentListSyntax = SyntaxFactory.AttributeArgumentList(
            SyntaxFactory.SeparatedList(builderAttributeArgumentSyntax));
        var attributeSyntax = SyntaxFactory.Attribute(attributeNameSyntax, attributeArgumentListSyntax);
        return attributeSyntax;
    }
}
