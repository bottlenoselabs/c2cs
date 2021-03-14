// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ClangSharp.Interop;

namespace C2CS.Bindgen.ExploreCCode
{
    public class ExploreCCodeModule
    {
        private readonly ClangExtensions.VisitChildCursorAction _visitCursor;
        private readonly HashSet<CXCursor> _visitedCursors = new();
        private readonly HashSet<CXType> _visitedTypes = new();
        private readonly List<CXCursor> _functions = new();
        private readonly List<ClangEnum> _enums = new();
        private readonly List<ClangStruct> _records = new();
        private readonly List<CXCursor> _opaqueTypes = new();
        private readonly List<ClangForwardType> _forwardTypes = new();
        private readonly List<ClangFunctionPointer> _functionPointers = new();
        private readonly List<CXCursor> _systemTypes = new();
        private readonly Dictionary<CXCursor, string> _namesByCursor = new();
        private readonly Dictionary<CXCursor, List<CXCursor>> _functionParametersByFunction = new();

        public ExploreCCodeModule()
        {
            _visitCursor = VisitCursor;
        }

        public GenericCodeAbstractSyntaxTree ExtractClangAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            ExploreAbstractSyntaxTree(translationUnit);
            return CollectExtractedData();
        }

        private void ExploreAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            var externalFunctions = GetExternFunctions(translationUnit);
            foreach (var function in externalFunctions)
            {
                VisitCursor(function, translationUnit.Cursor);
            }
        }

        private GenericCodeAbstractSyntaxTree CollectExtractedData()
        {
            var namesByCursor = _namesByCursor.ToImmutableDictionary();
            var functionParametersByFunction = _functionParametersByFunction.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableArray());

            var mapper = new ClangCodeToGenericCodeMapperModule(
                namesByCursor,
                functionParametersByFunction);

            var result = mapper.MapSyntaxTree(
                _functions.ToImmutableArray(),
                _records.ToImmutableArray(),
                _enums.ToImmutableArray(),
                _opaqueTypes.ToImmutableArray(),
                _forwardTypes.ToImmutableArray(),
                _functionPointers.ToImmutableArray(),
                _systemTypes.ToImmutableArray(),
                _namesByCursor);

            return result;
        }

        private static ImmutableArray<CXCursor> GetExternFunctions(CXTranslationUnit translationUnit)
        {
            var externFunctions = new List<CXCursor>();
            translationUnit.Cursor.VisitChildren((child, _) =>
            {
                if (child.Kind != CXCursorKind.CXCursor_FunctionDecl ||
                    child.Linkage != CXLinkageKind.CXLinkage_External)
                {
                    return;
                }

                if (child.IsInSystem())
                {
                    return;
                }

                externFunctions.Add(child);
            });

            return externFunctions.ToImmutableArray();
        }

        private bool CanVisitCursor(CXCursor cursor)
        {
            if (_visitedCursors.Contains(cursor))
            {
                return false;
            }

            _visitedCursors.Add(cursor);
            _namesByCursor.Add(cursor, cursor.Spelling.CString);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitCursor(CXCursor cursor, CXCursor parent)
        {
            if (!CanVisitCursor(cursor))
            {
                return;
            }

            if (cursor.IsInSystem())
            {
                VisitSystemType(cursor);
            }
            else if (cursor.IsDeclaration)
            {
                VisitDeclaration(cursor, parent);
            }
            else if (cursor.IsReference)
            {
                VisitCursor(cursor.Type.Declaration, parent);
            }
            else if (cursor.IsAttribute)
            {
                // Ignore
            }
            else
            {
                var up = UnexpectedScenario();
                throw up;
            }
        }

        private bool CanVisitType(CXType type)
        {
            if (_visitedTypes.Contains(type))
            {
                return false;
            }

            _visitedTypes.Add(type);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitType(CXType type, CXCursor parent, CXCursor grandParent)
        {
            if (!CanVisitType(type))
            {
                return;
            }

            switch (type.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Attributed:
                    VisitType(type.ModifiedType, parent, grandParent);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    VisitType(type.NamedType, parent, grandParent);
                    break;
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_LValueReference:
                case CX_TypeClass.CX_TypeClass_RValueReference:
                    VisitType(type.PointeeType, parent, grandParent);
                    break;
                case CX_TypeClass.CX_TypeClass_Typedef:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_IncompleteArray:
                    VisitType(type.ElementType, parent, grandParent);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                    VisitType(type.ElementType, parent, grandParent);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    // Ignored
                    break;
                case CX_TypeClass.CX_TypeClass_FunctionProto:
                    // Assume parent is closest match of function proto
                    VisitFunctionProto(string.Empty, type, parent, grandParent);
                    break;
                default:
                    var up = UnsupportedType(type);
                    throw up;
            }
        }

        private void VisitDeclaration(CXCursor declaration, CXCursor parent)
        {
            switch (declaration.DeclKind)
            {
                case CX_DeclKind.CX_DeclKind_Enum:
                    VisitEnum(declaration.Spelling.CString, declaration, declaration.EnumDecl_IntegerType, declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Record:
                    VisitRecord(declaration.Spelling.CString, declaration, declaration.Type, declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Typedef:
                    VisitTypedef(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Function:
                    VisitFunction(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_ParmVar:
                    VisitFunctionParameter(parent, declaration);
                    break;
                default:
                    var up = UnsupportedDeclaration(declaration);
                    throw up;
            }
        }

        private void VisitFunction(CXCursor function)
        {
            _functionParametersByFunction.Add(function, new List<CXCursor>());

            VisitType(function.ResultType, function, function.SemanticParent);
            function.VisitChildren(_visitCursor);

            _functions.Add(function);
        }

        private void VisitFunctionParameter(CXCursor function, CXCursor functionParameter)
        {
            VisitType(functionParameter.Type, functionParameter, function);

            var functionParameters = _functionParametersByFunction[function];
            functionParameters.Add(functionParameter);
        }

        private void VisitEnum(string name, CXCursor cursor, CXType type, CXCursor underlyingCursor)
        {
            var enumValues = new List<ClangEnumValue>();

            underlyingCursor.VisitChildren(VisitEnumValues);

            var clangEnum = new ClangEnum(
                name,
                cursor,
                type,
                enumValues.ToImmutableArray());
            _enums.Add(clangEnum);

            void VisitEnumValues(CXCursor child, CXCursor parent)
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                var clangEnumValue = new ClangEnumValue(
                    child.Spelling.CString,
                    child.EnumConstantDeclValue);
                enumValues.Add(clangEnumValue);
            }
        }

        private void VisitRecord(string name, CXCursor cursor, CXType type, CXCursor underlyingCursor)
        {
            var recordFields = new List<ClangStructField>();

            underlyingCursor.VisitChildren(VisitStructFields);

            if (!cursor.IsAnonymous)
            {
                var record = new ClangStruct(
                    name,
                    type,
                    cursor,
                    recordFields.ToImmutableArray());

                _records.Add(record);
            }

            void VisitStructFields(CXCursor child, CXCursor parent)
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                var structField = VisitRecordField(child.Spelling.CString, parent, child);
                recordFields.Add(structField);
            }
        }

        private ClangStructField VisitRecordField(string name, CXCursor record, CXCursor recordField)
        {
            VisitType(recordField.Type, recordField, record);

            var structField = new ClangStructField(name, recordField, recordField.Type);
            return structField;
        }

        private void VisitTypedef(CXCursor typedef)
        {
            var typedefName = typedef.Type.TypedefName.CString;
            var underlyingType = typedef.TypedefDeclUnderlyingType.CanonicalType;

            switch (underlyingType.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Pointer:
                    VisitTypedefPointer(typedefName, typedef, underlyingType.PointeeType);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    VisitForwardType(typedefName, typedef, underlyingType);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                    VisitRecord(typedefName, typedef, underlyingType, underlyingType.Declaration);
                    break;
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitEnum(typedefName, typedef, underlyingType.Declaration.EnumDecl_IntegerType, underlyingType.Declaration);
                    break;
                default:
                    var up = UnexpectedScenario();
                    throw up;
            }
        }

        private void VisitTypedefPointer(string typedefName, CXCursor typedef, CXType pointeeType)
        {
            if (pointeeType.kind == CXTypeKind.CXType_Void)
            {
                VisitOpaqueType(typedef);
            }
            else
            {
                switch (pointeeType.TypeClass)
                {
                    case CX_TypeClass.CX_TypeClass_Elaborated:
                    {
                        var namedType = pointeeType.NamedType;
                        if (namedType.kind == CXTypeKind.CXType_Record)
                        {
                            var recordType = namedType;
                            var childrenCount = 0;
                            recordType.Declaration.VisitChildren((_, _) => childrenCount += 1);
                            if (childrenCount == 0)
                            {
                                VisitOpaqueType(typedef);
                            }
                        }

                        break;
                    }

                    case CX_TypeClass.CX_TypeClass_FunctionProto:
                    {
                        if (CanVisitType(pointeeType))
                        {
                            VisitFunctionProto(typedefName, pointeeType, typedef, typedef.SemanticParent);
                        }

                        break;
                    }

                    default:
                        var up = UnexpectedScenario();
                        throw up;
                }
            }
        }

        private void VisitForwardType(string name, CXCursor cursor, CXType underlyingType)
        {
            var forwardType = new ClangForwardType(
                name,
                cursor,
                underlyingType);
            _forwardTypes.Add(forwardType);
        }

        private void VisitOpaqueType(CXCursor opaqueType)
        {
            _opaqueTypes.Add(opaqueType);
        }

        private void VisitSystemType(CXCursor systemType)
        {
            _systemTypes.Add(systemType);
        }

        private void VisitFunctionProto(string name, CXType type, CXCursor cursor, CXCursor parent)
        {
            var grandParent = parent.SemanticParent;
            VisitType(type.ResultType, parent, grandParent);
            type.VisitChildren(child => VisitType(child.Type, parent, grandParent));

            if (string.IsNullOrEmpty(name))
            {
                var cursorName = _namesByCursor[cursor];
                var parentName = _namesByCursor[parent];
                name = $"{parentName}_{cursorName}";
            }

            var clangFunctionPointer = new ClangFunctionPointer(
                name,
                type,
                cursor,
                parent);
            _functionPointers.Add(clangFunctionPointer);
        }

        private static Exception UnsupportedDeclaration(CXCursor declaration)
        {
            return new NotImplementedException(
                $"Not yet supported declaration kind `{declaration.DeclKind}`: '{declaration}'.");
        }

        private static Exception UnsupportedType(CXType type)
        {
            return new NotImplementedException($"Not yet supported type class `{type.TypeClass}`: '{type}'.");
        }

        private static Exception UnexpectedScenario()
        {
            return new NotImplementedException($"The header file used has unforeseen conditions. Please create an issue on GitHub with the stack trace along with the header file.");
        }
    }
}
