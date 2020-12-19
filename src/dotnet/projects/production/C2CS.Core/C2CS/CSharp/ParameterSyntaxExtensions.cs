// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace C2CS
{
    internal static class ParameterSyntaxExtensions
    {
        internal static ParameterSyntax WithAttribute(
            this ParameterSyntax parameterSyntax,
            string name)
        {
            return parameterSyntax.WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(
                                IdentifierName(name))))));
        }
    }
}
