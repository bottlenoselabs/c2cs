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
        private readonly List<CXCursor> _enums = new();
        private readonly List<CXCursor> _records = new();
        private readonly List<CXCursor> _opaqueTypes = new();
        private readonly List<CXCursor> _forwardTypes = new();
        private readonly List<CXCursor> _functionPointers = new();
        private readonly List<CXCursor> _systemTypes = new();
        private readonly Dictionary<CXCursor, string> _namesByCursor = new();
        private readonly Dictionary<CXCursor, List<CXCursor>> _functionParametersByFunction = new();
        private readonly Dictionary<CXCursor, List<CXCursor>> _recordFieldsByRecord = new();
        private readonly Dictionary<CXCursor, List<CXCursor>> _enumValuesByEnum = new();

        public ExploreCCodeModule()
        {
            _visitCursor = VisitCursor;
        }

        public CAbstractSyntaxTree ExtractClangAbstractSyntaxTree(CXTranslationUnit translationUnit)
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

        private CAbstractSyntaxTree CollectExtractedData()
        {
            var namesByCursor = _namesByCursor.ToImmutableDictionary();
            var functionParametersByFunction = _functionParametersByFunction.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableArray());
            var recordFieldsByRecord = _recordFieldsByRecord.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableArray());
            var enumValuesByEnum =_enumValuesByEnum.ToImmutableDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToImmutableArray());

            var mapper = new ClangToCMapperModule(
                namesByCursor,
                functionParametersByFunction,
                recordFieldsByRecord,
                enumValuesByEnum);

            var cFunctions = mapper.MapFunctions(_functions.ToImmutableArray());
            var cStructs = mapper.MapStructs(_records.ToImmutableArray());
            var cEnums = mapper.MapEnums(_enums.ToImmutableArray());
            var cOpaqueTypes = mapper.MapOpaqueTypes(_opaqueTypes.ToImmutableArray());
            var cForwardTypes = mapper.MapForwardTypes(_forwardTypes.ToImmutableArray());
            var cFunctionPointers = mapper.MapFunctionPointers(_functionPointers.ToImmutableArray());
            var cSystemTypes = _systemTypes.ToImmutableArray();

            var result = new CAbstractSyntaxTree(
                cFunctions,
                cStructs,
                cEnums,
                cOpaqueTypes,
                cForwardTypes,
                cFunctionPointers,
                cSystemTypes,
                _namesByCursor.ToImmutableDictionary());

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
        private void VisitType(CXType type, CXCursor parent)
        {
            if (!CanVisitType(type))
            {
                return;
            }

            switch (type.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Attributed:
                    VisitType(type.ModifiedType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    VisitType(type.NamedType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_LValueReference:
                case CX_TypeClass.CX_TypeClass_RValueReference:
                    VisitType(type.PointeeType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Typedef:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_IncompleteArray:
                    VisitType(type.ElementType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitCursor(type.Declaration, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                    VisitType(type.ElementType, parent);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    // Ignored
                    break;
                case CX_TypeClass.CX_TypeClass_FunctionProto:
                    // Ignored
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
                    VisitEnum(declaration, declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Record:
                    VisitRecord(declaration, declaration);
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
                case CX_DeclKind.CX_DeclKind_Field:
                    VisitRecordField(parent, declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_EnumConstant:
                    VisitType(declaration.Type, parent);
                    break;
                default:
                    var up = UnsupportedDeclaration(declaration);
                    throw up;
            }
        }

        private void VisitFunction(CXCursor function)
        {
            _functionParametersByFunction.Add(function, new List<CXCursor>());

            VisitType(function.ResultType, function);
            function.VisitChildren(_visitCursor);

            _functions.Add(function);
        }

        private void VisitFunctionParameter(CXCursor function, CXCursor functionParameter)
        {
            VisitType(functionParameter.Type, functionParameter);

            var functionParameters = _functionParametersByFunction[function];
            functionParameters.Add(functionParameter);
        }

        private void VisitEnum(CXCursor @enum, CXCursor underlyingEnum)
        {
            var enumValues = new List<CXCursor>();
            _enumValuesByEnum.Add(@enum != underlyingEnum ? underlyingEnum : @enum, enumValues);
            _enums.Add(@enum);

            underlyingEnum.VisitChildren(VisitEnumValues);

            void VisitEnumValues(CXCursor child, CXCursor parent)
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                enumValues.Add(child);
                VisitCursor(child, parent);
            }
        }

        private void VisitRecord(CXCursor record, CXCursor underlyingRecord)
        {
            var recordFields = new List<CXCursor>();
            _recordFieldsByRecord.Add(record != underlyingRecord ? underlyingRecord : record, recordFields);

            underlyingRecord.VisitChildren(_visitCursor);

            if (record.IsAnonymous)
            {
                return;
            }

            _records.Add(record);
        }

        private void VisitRecordField(CXCursor record, CXCursor recordField)
        {
            VisitType(recordField.Type, record);

            var recordFields = _recordFieldsByRecord[record];
            recordFields.Add(recordField);
        }

        private void VisitTypedef(CXCursor typedef)
        {
            var typedefName = typedef.Type.TypedefName.CString;
            var underlyingType = typedef.TypedefDeclUnderlyingType;
            switch (underlyingType.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Pointer:
                    VisitTypedefPointer(underlyingType.PointeeType);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    VisitTypedefElaborated(underlyingType.NamedType);
                    break;
                default:
                    VisitForwardType(typedef);
                    break;
            }

            void VisitTypedefPointer(CXType pointeeType)
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
                            if (!CanVisitType(pointeeType))
                            {
                                break;
                            }

                            VisitFunctionProto(typedef, pointeeType, typedef.SemanticParent);
                            break;
                        }

                        default:
                            var up = UnexpectedScenario();
                            throw up;
                    }
                }
            }

            void VisitTypedefElaborated(CXType namedType)
            {
                var namedCursor = namedType.Declaration;

                if (!CanVisitType(namedType) || !CanVisitCursor(namedCursor))
                {
                    if (typedefName != namedType.Spelling.CString)
                    {
                        _namesByCursor.Remove(namedType.Declaration);
                        _namesByCursor.Add(namedType.Declaration, typedefName);

                        _namesByCursor.Remove(namedCursor);
                        _namesByCursor.Add(namedCursor, typedefName);
                    }

                    return;
                }

                switch (namedType.TypeClass)
                {
                    case CX_TypeClass.CX_TypeClass_Record:
                        VisitRecord(typedef, namedCursor);
                        break;
                    case CX_TypeClass.CX_TypeClass_Enum:
                        VisitEnum(typedef, namedCursor);
                        break;
                    default:
                        var up = UnexpectedScenario();
                        throw up;
                }
            }
        }

        private void VisitForwardType(CXCursor forwardType)
        {
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

        private void VisitFunctionProto(CXCursor functionProto, CXType functionProtoType, CXCursor parent)
        {
            VisitType(functionProtoType.ResultType, parent);
            functionProtoType.VisitChildren(child => VisitType(child.Type, parent));

            _functionPointers.Add(functionProto);
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
