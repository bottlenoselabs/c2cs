// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestFunction
{
    public string Name { get; }

    public CSharpTestCallingConvention CallingConvention { get; }

    public CSharpTestType ReturnType { get; }

    public ImmutableArray<CSharpTestFunctionParameter> Parameters { get; }

    public CSharpTestFunction(SemanticModel semanticModel, MethodDeclarationSyntax syntaxNode)
    {
        Name = syntaxNode.Identifier.Text;
        CallingConvention = GetCallingConvention(syntaxNode);
        ReturnType = new CSharpTestType(semanticModel.GetTypeInfo(syntaxNode.ReturnType).Type!);
        Parameters = CreateParameters(semanticModel, syntaxNode);
        return;

        static CSharpTestCallingConvention GetCallingConvention(MethodDeclarationSyntax syntaxNode)
        {
            var libraryImportAttribute = syntaxNode.TryGetAttribute("UnmanagedCallConv");
            _ = libraryImportAttribute.Should().NotBeNull();
            var libraryImportAttributeArguments = libraryImportAttribute?.ArgumentList?.Arguments;
            _ = libraryImportAttributeArguments.Should().NotBeNull();

            var callingConvention = CSharpTestCallingConvention.Unknown;
            foreach (var argument in libraryImportAttributeArguments!.Value)
            {
                var propertyName = argument.NameEquals!.Name.Identifier.Text;
                if (propertyName == "CallConvs")
                {
                    var arrayCreationSyntax = argument.Expression as ImplicitArrayCreationExpressionSyntax;
                    foreach (var expressionSyntax in arrayCreationSyntax!.Initializer.Expressions)
                    {
                        var typeOfExpressionSyntax = (TypeOfExpressionSyntax)expressionSyntax;
                        var identifierNameSyntax = (IdentifierNameSyntax)typeOfExpressionSyntax.Type;
                        var typeString = identifierNameSyntax.Identifier.ValueText;
                        if (typeString == "CallConvCdecl")
                        {
                            callingConvention = CSharpTestCallingConvention.C;
                        }
                    }
                }
            }

            return callingConvention;
        }

        static ImmutableArray<CSharpTestFunctionParameter> CreateParameters(
            SemanticModel semanticModel,
            MethodDeclarationSyntax syntaxNode)
        {
            var builder = ImmutableArray.CreateBuilder<CSharpTestFunctionParameter>();
            foreach (var syntaxNodeParameter in syntaxNode.ParameterList.Parameters)
            {
                var parameter = new CSharpTestFunctionParameter(semanticModel, syntaxNodeParameter);
                builder.Add(parameter);
            }

            return builder.ToImmutable();
        }
    }

    public override string ToString()
    {
        return Name;
    }
}
