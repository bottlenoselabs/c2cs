// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.GenerateCSharpCode;

public sealed class CodeGeneratorDocumentPInvokeContext
{
    private readonly HashSet<string> _existingNames = new();

    public bool IsEnabledFunctionPointers { get; }

    public bool IsEnabledLibraryImportAttribute { get; }

    public CodeGeneratorDocumentPInvokeContext(InputSanitized input)
    {
        IsEnabledFunctionPointers = input.IsEnabledFunctionPointers;
        IsEnabledLibraryImportAttribute = input.IsEnabledLibraryImportAttribute;
    }

    public bool NameAlreadyExists(string name)
    {
        var alreadyExists = !_existingNames.Add(name);
        return alreadyExists;
    }

    public string GenerateCodeAttributes(ImmutableArray<Attribute> attributes)
    {
        var stringBuilder = new StringBuilder();

        for (var i = 0; i < attributes.Length; i++)
        {
            var attribute = attributes[i];
            var attributeSyntax = Attribute(attribute);
            stringBuilder.Append('[');
            stringBuilder.Append(attributeSyntax.ToFullString());
            stringBuilder.Append(']');

            if (i != attributes.Length - 1)
            {
                stringBuilder.AppendLine();
            }
        }

        return stringBuilder.ToString();
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
