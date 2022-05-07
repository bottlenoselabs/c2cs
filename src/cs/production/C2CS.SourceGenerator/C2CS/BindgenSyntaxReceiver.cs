// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS;

public class BindgenSyntaxReceiver : ISyntaxReceiver
{
#pragma warning disable CA1002
    public List<BindgenTarget> Targets { get; } = new();
#pragma warning restore CA1002

    public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not ClassDeclarationSyntax @class)
        {
            return;
        }

        var isPartial = IsPartial(@class);
        if (!isPartial)
        {
            return;
        }

        foreach (var attributeList in @class.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToFullString();
                if (attributeName == "Bindgen")
                {
                    var bindgenAttribute = CreateAttribute<BindgenAttribute>(attribute);
                    var target = new BindgenTarget(@class, bindgenAttribute);
                    Targets.Add(target);
                }
            }
        }
    }

    private static bool IsPartial(MemberDeclarationSyntax member)
    {
        var isPartial = false;
        foreach (var modifier in member.Modifiers)
        {
            if (modifier.Text == "partial")
            {
                isPartial = true;
                break;
            }
        }

        return isPartial;
    }

    private static T CreateAttribute<T>(AttributeSyntax attributeSyntax)
        where T : Attribute, new()
    {
        var result = new T();

        if (attributeSyntax.ArgumentList == null)
        {
            return result;
        }

        foreach (var argument in attributeSyntax.ArgumentList.Arguments)
        {
            var propertyParsed = argument.ToFullString().Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
            var propertyName = propertyParsed[0].Trim();
            var propertyValueRaw = propertyParsed[1].Trim();
#pragma warning disable IDE0057
            var propertyValue = propertyValueRaw.Substring(1, propertyValueRaw.Length - 2);
#pragma warning restore IDE0057
            var property = result.GetType().GetRuntimeProperty(propertyName);
            if (property != null)
            {
                property.SetValue(result, propertyValue);
            }
        }

        return result;
    }
}
