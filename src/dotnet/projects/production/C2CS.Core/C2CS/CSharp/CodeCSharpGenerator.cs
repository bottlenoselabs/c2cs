// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using PointerType = ClangSharp.PointerType;

namespace C2CS
{
    internal sealed class CodeCSharpGenerator
    {
        private readonly string _libraryName;
        private readonly Dictionary<string, TypeSyntax> _typesByCNames = new();

        public CodeCSharpGenerator(string libraryName)
        {
            _libraryName = libraryName;
            AddKnownTypes();
        }

        public void AddAlias(TypedefDecl typeAlias)
        {
            var aliasTypeString = typeAlias.Spelling;
            var underlyingType = typeAlias.UnderlyingType;
            if (underlyingType is PointerType pointerType)
            {
                underlyingType = pointerType.PointeeType;
            }

            var underlyingTypeSyntax = GetTypeSyntax(underlyingType.AsString, out _, out _);
            var underlyingTypeString = underlyingTypeSyntax.ToFullString();

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (_typesByCNames.TryGetValue(underlyingTypeString, out var typeSyntax))
            {
                _typesByCNames.Add(aliasTypeString, typeSyntax);
            }
            else
            {
                _typesByCNames.Add(aliasTypeString, underlyingTypeSyntax);
            }
        }

        public ClassDeclarationSyntax CreatePInvokeClass(
            string name,
            IEnumerable<FieldDeclarationSyntax> fields,
            IEnumerable<EnumDeclarationSyntax> enums,
            IEnumerable<StructDeclarationSyntax> structs,
            IEnumerable<MethodDeclarationSyntax> methods)
        {
            var members = new List<MemberDeclarationSyntax>();

            var libraryNameField = FieldDeclaration(
                    VariableDeclaration(PredefinedType(Token(SyntaxKind.StringKeyword)))
                        .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("LibraryName"))
                            .WithInitializer(
                                EqualsValueClause(LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    Literal(_libraryName)))))))
                .WithModifiers(
                    TokenList(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ConstKeyword)));

            members.Add(libraryNameField);
            members.AddRange(fields);
            members.AddRange(enums);
            members.AddRange(structs);
            members.AddRange(methods);

            return ClassDeclaration(name)
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword))
                .WithMembers(List(
                    members));
        }

        public MethodDeclarationSyntax CreateExternMethod(FunctionDecl function)
        {
            var returnType = GetTypeSyntax(function.ReturnType.AsString, out _, out _);
            var method = MethodDeclaration(returnType, function.Name)
                .WithDllImportAttribute()
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.ExternKeyword)))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            var parametersC = function.Parameters;
            var parameters = new List<ParameterSyntax>(parametersC.Count);

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var parameterC in parametersC)
            {
                var parameter = CreateMethodParameter(parameterC);
                parameters.Add(parameter);
            }

            method = method
                .AddParameterListParameters(parameters.ToArray());

            return method;
        }

        public FieldDeclarationSyntax CreateConstant(EnumConstantDecl enumConstant)
        {
            var variableDeclarationCSharp = CreateConstantVariable(enumConstant);
            var constFieldDeclarationCSharp = FieldDeclaration(variableDeclarationCSharp)
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.ConstKeyword)));
            return constFieldDeclarationCSharp;
        }

        public EnumDeclarationSyntax CreateEnum(EnumDecl enumDeclarationC)
        {
            var enumNameC = enumDeclarationC.Name;
            if (string.IsNullOrEmpty(enumNameC))
            {
                enumNameC = enumDeclarationC.TypeForDecl.AsString;
            }

            _typesByCNames.Add(enumNameC, ParseTypeName(enumNameC));

            var enumTypeKindCSharp = GetEnumSyntaxKind(enumDeclarationC);

            var enumDeclarationCSharp = EnumDeclaration(enumNameC)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SimpleBaseType(PredefinedType(Token(enumTypeKindCSharp))));

            var enumConstantDeclarationsC = enumDeclarationC.Enumerators;
            var enumMemberDeclarationsCSharp = new EnumMemberDeclarationSyntax[enumConstantDeclarationsC.Count];
            for (var i = 0; i < enumConstantDeclarationsC.Count; i++)
            {
                var enumConstantDeclarationC = enumConstantDeclarationsC[i];
                var memberName = enumConstantDeclarationC.Name.Replace($"{enumNameC}_", string.Empty);
                var value = enumConstantDeclarationC.InitVal;

                var equalsValue = CreateEqualsValue(value, enumTypeKindCSharp);
                enumMemberDeclarationsCSharp[i] = EnumMemberDeclaration(memberName)
                    .WithEqualsValue(equalsValue);
            }

            return enumDeclarationCSharp
                .AddMembers(enumMemberDeclarationsCSharp)
                .NormalizeWhitespace();
        }

        public StructDeclarationSyntax CreateStruct(
            string name,
            RecordDecl recordC,
            CodeCStructLayoutCalculator structLayoutCalculator)
        {
            var typeSyntax = ParseTypeName(name);
            if (!recordC.Handle.IsAnonymous)
            {
                _typesByCNames.Add(name, typeSyntax);
            }

            var layout = structLayoutCalculator.GetLayout(recordC);
            var @struct = StructDeclaration(name)
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithAttributeStructLayout(LayoutKind.Explicit, layout.Size, layout.Alignment);
            var structTypeSyntax = GetTypeSyntax(recordC.Name, out _, out _);

            var members = new List<MemberDeclarationSyntax>();

            foreach (var fieldC in recordC.Fields)
            {
                CreateStructHelperAddField(structTypeSyntax, fieldC, members, structLayoutCalculator);
            }

            @struct = @struct.AddMembers(members.ToArray());

            return @struct;
        }

        private MethodDeclarationSyntax CreateStructHelperCreateWrappedFieldMethod(FieldDecl fieldC, TypeSyntax structTypeSyntax)
        {
            var typeString = fieldC.Type.ToString();
            typeString = typeString.Substring(0, typeString.IndexOf('[')).Trim();
            var fieldType = GetTypeSyntax(typeString, out _, out _);
            return CreateStructFieldWrapperMethod(structTypeSyntax, fieldType, fieldC);
        }

        private void CreateStructHelperAddField(
            TypeSyntax parentStructSyntax,
            FieldDecl fieldC,
            ICollection<MemberDeclarationSyntax> members,
            CodeCStructLayoutCalculator structLayoutCalculator)
        {
            var fieldLayout = structLayoutCalculator.GetLayout(fieldC);
            string typeName;
            var type = fieldC.Type switch
            {
                ArrayType arrayType => arrayType.ElementType,
                ElaboratedType elaboratedType => elaboratedType.NamedType,
                TypedefType typedefType => typedefType.Decl.UnderlyingType,
                _ => fieldC.Type
            };

            // it's possible to have an array of an anon
            if (type is ElaboratedType elaboratedType2)
            {
                type = elaboratedType2.NamedType;
            }

            if (type is RecordType recordType)
            {
                if (recordType.Decl.Handle.IsAnonymous)
                {
                    var recordC = (RecordDecl)recordType.Decl;
                    if (recordC.IsStruct)
                    {
                        typeName = $"Anonymous_Struct_{fieldC.Name}";
                    }
                    else if (recordC.IsUnion)
                    {
                        typeName = $"Anonymous_Union_{fieldC.Name}";
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    if (recordC.IsStruct)
                    {
                        var anonStruct = CreateStruct(typeName, recordC, structLayoutCalculator);
                        members.Add(anonStruct);
                    }
                    else if (recordC.IsUnion)
                    {
                        CreateStructHelperAddUnionFields(parentStructSyntax, recordC, members, structLayoutCalculator);
                    }
                }
                else
                {
                    typeName = type.AsString;
                    if (typeName.StartsWith("struct "))
                    {
                        typeName = typeName.Substring(7);
                    }
                }
            }
            else
            {
                typeName = fieldC.Type.AsString;
            }

            if (typeName.StartsWith("volatile "))
            {
                typeName = typeName.Substring(9);
            }

            var field = CreateStructField(
                fieldC.Name,
                typeName,
                fieldLayout,
                out var needsToBeWrapped);
            members.Add(field);

            if (needsToBeWrapped)
            {
                var method = CreateStructHelperCreateWrappedFieldMethod(fieldC, parentStructSyntax);
                members.Add(method);
            }
        }

        private void CreateStructHelperAddUnionFields(
            TypeSyntax parentStructSyntax,
            RecordDecl union,
            ICollection<MemberDeclarationSyntax> members,
            CodeCStructLayoutCalculator structLayoutCalculator)
        {
            foreach (var fieldC in union.Fields)
            {
                var fieldLayout = structLayoutCalculator.GetLayout(fieldC);
                string typeName;
                if (fieldC.Type.Handle.Declaration.IsAnonymous)
                {
                    var elaboratedTypeC = (ElaboratedType)fieldC.Type;
                    var recordTypeC = (RecordType)elaboratedTypeC.NamedType;
                    var recordC = (RecordDecl)recordTypeC.Decl;
                    if (recordC.IsStruct)
                    {
                        typeName = $"Anonymous_Struct_{fieldC.Name}";
                    }
                    else if (recordC.IsUnion)
                    {
                        typeName = $"Anonymous_Union_{fieldC.Name}";
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    var struct2 = CreateStruct(typeName, recordC, structLayoutCalculator);
                    members.Add(struct2);
                }
                else
                {
                    typeName = fieldC.Type.AsString;
                }

                var field = CreateStructField(fieldC.Name, typeName, fieldLayout, out var needsToBeWrapped);
                members.Add(field);

                if (needsToBeWrapped)
                {
                    var method = CreateStructHelperCreateWrappedFieldMethod(fieldC, parentStructSyntax);
                    members.Add(method);
                }
            }
        }

        // private static IEnumerable<int> ExtractTypeArrayDimensions(string typeString)
        // {
        //     var bracketOpenIndex = typeString.IndexOf('[');
        //     while (bracketOpenIndex > 0)
        //     {
        //         var bracketClosedIndex = typeString.IndexOf(']', bracketOpenIndex);
        //         var sizeStringLength = bracketClosedIndex - (bracketOpenIndex + 1);
        //         var sizeString = typeString.AsSpan(bracketOpenIndex + 1, sizeStringLength);
        //         var size = int.Parse(sizeString);
        //         yield return size;
        //
        //         bracketOpenIndex = typeString.IndexOf('[', bracketClosedIndex);
        //     }
        // }

        // ReSharper disable once SuggestBaseTypeForParameter
        private FieldDeclarationSyntax CreateStructField(
            string name,
            string typeName,
            CodeCStructLayoutCalculator.DeclLayout fieldLayout,
            out bool needsToBeWrapped)
        {
            var type = GetTypeSyntax(typeName, out _, out var isArray);
            var typeFullString = type.ToFullString();
            needsToBeWrapped = false;

            VariableDeclaratorSyntax variable;
            if (!isArray)
            {
                variable = VariableDeclarator(Identifier(name));
            }
            else
            {
                var isValidFixedType = IsValidFixedCSharpType(typeFullString);
                if (isValidFixedType)
                {
                    var arraySize = fieldLayout.Size / fieldLayout.Alignment;
                    variable = VariableDeclarator(Identifier(name))
                        .WithArgumentList(
                            BracketedArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(arraySize))))));
                }
                else
                {
                    var typeTokenSyntaxKind = fieldLayout.Alignment switch
                    {
                        1 => SyntaxKind.ByteKeyword,
                        2 => SyntaxKind.UShortKeyword,
                        4 => SyntaxKind.UIntKeyword,
                        8 => SyntaxKind.ULongKeyword,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    type = PredefinedType(Token(typeTokenSyntaxKind));
                    variable = VariableDeclarator(Identifier($"_{name}"))
                        .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                            Argument(
                                BinaryExpression(
                                    SyntaxKind.DivideExpression,
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(fieldLayout.Size)),
                                    LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        Literal(fieldLayout.Alignment)))))));

                    needsToBeWrapped = true;
                }
            }

            var field = FieldDeclaration(
                    VariableDeclaration(type)
                        .WithVariables(SingletonSeparatedList(variable)))
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithAttributeFieldOffset(fieldLayout.FieldAddress, fieldLayout.Size, fieldLayout.FieldPadding);

            if (isArray)
            {
                field = field.AddModifiers(Token(SyntaxKind.FixedKeyword))
                    .WithSemicolonToken(Token(TriviaList(), SyntaxKind.SemicolonToken, TriviaList(
                        Comment($"/* original type is `{typeName}` */"))));
            }

            return field;
        }

        private static bool IsValidFixedCSharpType(string typeString)
        {
            return typeString switch
            {
                "bool" => true,
                "byte" => true,
                "char" => true,
                "short" => true,
                "int" => true,
                "long" => true,
                "sbyte" => true,
                "ushort" => true,
                "uint" => true,
                "ulong" => true,
                "float" => true,
                "double" => true,
                _ => false
            };
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private ParameterSyntax CreateMethodParameter(ParmVarDecl parameterC)
        {
            var parameter = Parameter(Identifier(parameterC.Name));
            var type = GetTypeSyntax(parameterC.Type.AsString, out var isReadOnly, out _);

            if (isReadOnly)
            {
                parameter = parameter
                    .WithAttribute("In");
            }

            parameter = parameter
                .WithType(type);

            return parameter;
        }

        private static SyntaxKind GetEnumSyntaxKind(EnumDecl enumDeclarationC)
        {
            var enumTypeKindC = enumDeclarationC.IntegerType.Kind;

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            return enumTypeKindC switch
            {
                CXTypeKind.CXType_Int => SyntaxKind.IntKeyword,
                CXTypeKind.CXType_UInt => SyntaxKind.UIntKeyword,
                _ => throw new Exception($@"The enum type is not yet supported: {enumTypeKindC}.")
            };
        }

        private static EqualsValueClauseSyntax CreateEqualsValue(long value, SyntaxKind typeKind)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var literalToken = typeKind switch
            {
                SyntaxKind.IntKeyword => Literal((int)value),
                SyntaxKind.UIntKeyword => Literal((uint)value),
                _ => throw new Exception($"The syntax kind is not yet supported: {typeKind}.")
            };

            return EqualsValueClause(
                LiteralExpression(SyntaxKind.NumericLiteralExpression, literalToken));
        }

        private VariableDeclarationSyntax CreateConstantVariable(EnumConstantDecl enumConstant)
        {
            var type = GetTypeSyntax(enumConstant.Type.AsString, out _, out _);

            var typeString = type.ToFullString();
            var literalSyntaxToken = typeString switch
            {
                "short" => Literal((short)enumConstant.InitVal),
                "int" => Literal((int)enumConstant.InitVal),
                _ => throw new Exception(
                    $@"The enum constant literal expression is not yet supported: {enumConstant.InitExpr.CursorKind}.")
            };

            var variable = VariableDeclaration(type)
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator(
                                Identifier(enumConstant.Name))
                            .WithInitializer(
                                EqualsValueClause(LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression, literalSyntaxToken)))));

            return variable;
        }

        private static MethodDeclarationSyntax CreateStructFieldWrapperMethod(
            TypeSyntax structType,
            TypeSyntax fieldType,
            FieldDecl fieldC)
        {
            var identifierName = fieldC.Name;
            var fieldIdentifierName = $"_{fieldC.Name}";

            var body = Block(SingletonList<StatementSyntax>(FixedStatement(
                VariableDeclaration(PointerType(structType))
                    .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("@this"))
                        .WithInitializer(EqualsValueClause(
                            PrefixUnaryExpression(SyntaxKind.AddressOfExpression, ThisExpression()))))),
                Block(
                    LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
                        .WithVariables(SingletonSeparatedList(VariableDeclarator(Identifier("pointer"))
                            .WithInitializer(EqualsValueClause(CastExpression(
                                PointerType(fieldType),
                                PrefixUnaryExpression(SyntaxKind.AddressOfExpression, ElementAccessExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.PointerMemberAccessExpression,
                                            IdentifierName("@this"),
                                            IdentifierName(fieldIdentifierName)))
                                    .WithArgumentList(BracketedArgumentList(SingletonSeparatedList(
                                        Argument(LiteralExpression(
                                            SyntaxKind.NumericLiteralExpression,
                                            Literal(0))))))))))))),
                    LocalDeclarationStatement(VariableDeclaration(IdentifierName("var"))
                        .WithVariables(SingletonSeparatedList(VariableDeclarator(
                                Identifier("pointerOffset"))
                            .WithInitializer(EqualsValueClause(
                                IdentifierName("index")))))),
                    ReturnStatement(RefExpression(PrefixUnaryExpression(
                        SyntaxKind.PointerIndirectionExpression,
                        ParenthesizedExpression(BinaryExpression(
                            SyntaxKind.AddExpression,
                            IdentifierName("pointer"),
                            IdentifierName("pointerOffset"))))))))));

            return MethodDeclaration(RefType(fieldType), identifierName)
                .WithModifiers(TokenList(
                    Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("index"))
                        .WithType(PredefinedType(Token(SyntaxKind.IntKeyword)))
                        .WithDefault(EqualsValueClause(LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(0)))))))
                .WithBody(body);
        }

        private TypeSyntax GetTypeSyntax(string typeString, out bool isReadOnly, out bool isArray)
        {
            if (typeString.Contains("["))
            {
                isArray = true;
                var bracketIndexOpening = typeString.IndexOf('[');
                typeString = typeString.Substring(0, bracketIndexOpening).Trim();
            }
            else
            {
                isArray = false;
            }

            var isPointer = false;
            if (typeString.Contains("*"))
            {
                isPointer = true;
                typeString = typeString.Replace("*", string.Empty).Trim();
            }

            if (typeString.Contains("const"))
            {
                typeString = typeString.Replace("const", string.Empty).Trim();
                isReadOnly = true;
            }
            else
            {
                isReadOnly = false;
            }

            TypeSyntax type;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (_typesByCNames.TryGetValue(typeString, out var knownType))
            {
                type = knownType;
            }
            else
            {
                type = ParseTypeName(typeString);
            }

            return isPointer ? PointerType(type) : type;
        }

        private void AddKnownTypes()
        {
            _typesByCNames.Add("void", PredefinedType(Token(SyntaxKind.VoidKeyword)));
            _typesByCNames.Add("float", PredefinedType(Token(SyntaxKind.FloatKeyword)));

            // boolean
            _typesByCNames.Add("_Bool", ParseTypeName("BlittableBoolean"));
            _typesByCNames.Add("bool", ParseTypeName("BlittableBoolean"));

            // signed 64-bit integer
            _typesByCNames.Add("long long", PredefinedType(Token(SyntaxKind.LongKeyword)));
            _typesByCNames.Add("long long int", PredefinedType(Token(SyntaxKind.LongKeyword)));
            _typesByCNames.Add("signed long long", PredefinedType(Token(SyntaxKind.LongKeyword)));
            _typesByCNames.Add("signed long long int", PredefinedType(Token(SyntaxKind.LongKeyword)));

            // unsigned 64-bit integer
            _typesByCNames.Add("unsigned long long", PredefinedType(Token(SyntaxKind.ULongKeyword)));
            _typesByCNames.Add("unsigned long long int", PredefinedType(Token(SyntaxKind.ULongKeyword)));

            // signed 32-bit integer
            _typesByCNames.Add("int", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("signed int", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("long", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("long int", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("signed long", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("signed long int", PredefinedType(Token(SyntaxKind.IntKeyword)));
            _typesByCNames.Add("int32_t", PredefinedType(Token(SyntaxKind.IntKeyword)));

            // unsigned 32-bit integer
            _typesByCNames.Add("unsigned long", PredefinedType(Token(SyntaxKind.UIntKeyword)));
            _typesByCNames.Add("unsigned long int", PredefinedType(Token(SyntaxKind.UIntKeyword)));
            _typesByCNames.Add("unsigned int", PredefinedType(Token(SyntaxKind.UIntKeyword)));
            _typesByCNames.Add("uint32_t", PredefinedType(Token(SyntaxKind.UIntKeyword)));

            // signed 16-bit integer
            _typesByCNames.Add("short", PredefinedType(Token(SyntaxKind.ShortKeyword)));
            _typesByCNames.Add("short int", PredefinedType(Token(SyntaxKind.ShortKeyword)));
            _typesByCNames.Add("signed short", PredefinedType(Token(SyntaxKind.ShortKeyword)));
            _typesByCNames.Add("signed short int", PredefinedType(Token(SyntaxKind.ShortKeyword)));
            _typesByCNames.Add("int16_t", PredefinedType(Token(SyntaxKind.ShortKeyword)));

            // unsigned 16-bit integer
            _typesByCNames.Add("unsigned short", PredefinedType(Token(SyntaxKind.UShortKeyword)));
            _typesByCNames.Add("unsigned short int", PredefinedType(Token(SyntaxKind.UShortKeyword)));
            _typesByCNames.Add("uint16_t", PredefinedType(Token(SyntaxKind.UShortKeyword)));

            // signed 8-bit integer
            _typesByCNames.Add("signed char", PredefinedType(Token(SyntaxKind.SByteKeyword)));
            _typesByCNames.Add("int8_t", PredefinedType(Token(SyntaxKind.SByteKeyword)));

            // unsigned 8-bit integer
            _typesByCNames.Add("char", PredefinedType(Token(SyntaxKind.ByteKeyword)));
            _typesByCNames.Add("unsigned char", PredefinedType(Token(SyntaxKind.ByteKeyword)));
            _typesByCNames.Add("uint8_t", PredefinedType(Token(SyntaxKind.ByteKeyword)));
        }
    }
}
