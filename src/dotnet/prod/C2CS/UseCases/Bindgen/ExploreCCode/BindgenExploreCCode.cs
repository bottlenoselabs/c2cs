// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ClangSharp.Interop;

namespace C2CS
{
    public class BindgenExploreCCode
    {
        private class State
        {
            public readonly HashSet<CXCursor> VisitedCursors;
            public readonly HashSet<CXType> VisitedTypes;
            public readonly ImmutableArray<CXCursor>.Builder Functions;
            public readonly ImmutableArray<CXCursor>.Builder Enums;
            public readonly ImmutableArray<CXCursor>.Builder Records;
            public readonly ImmutableArray<CXCursor>.Builder OpaqueTypes;
            public readonly ImmutableArray<CXCursor>.Builder SystemCursors;

            public State()
            {
                VisitedCursors = new HashSet<CXCursor>();
                VisitedTypes = new HashSet<CXType>();
                Functions = ImmutableArray.CreateBuilder<CXCursor>();
                Enums = ImmutableArray.CreateBuilder<CXCursor>();
                Records = ImmutableArray.CreateBuilder<CXCursor>();
                OpaqueTypes = ImmutableArray.CreateBuilder<CXCursor>();
                SystemCursors = ImmutableArray.CreateBuilder<CXCursor>();
            }
        }

        private readonly State _state = new();

        public BindgenClangExtractedCursors ExtractClangAbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            var externalFunctions = GetExternFunctions(translationUnit);
            foreach (var function in externalFunctions)
            {
                VisitCursor(function);
            }

            var result = new BindgenClangExtractedCursors
            {
                Functions = _state.Functions.ToImmutableArray(),
                Records = _state.Records.ToImmutableArray(),
                Enums = _state.Enums.ToImmutableArray(),
                OpaqueTypes = _state.OpaqueTypes.ToImmutableArray(),
                SystemCursors = _state.SystemCursors.ToImmutableArray()
            };

            _state.VisitedCursors.Clear();
            _state.VisitedTypes.Clear();

            return result;
        }

        private ImmutableArray<CXCursor> GetExternFunctions(CXTranslationUnit translationUnit)
        {
            var externFunctions = new List<CXCursor>();
            translationUnit.Cursor.VisitChildren(child =>
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
            if (_state.VisitedCursors.Contains(cursor))
            {
                return false;
            }

            _state.VisitedCursors.Add(cursor);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitCursor(CXCursor cursor)
        {
            if (!CanVisitCursor(cursor))
            {
                return;
            }

            if (cursor.IsInSystem())
            {
                VisitSystemCursor(cursor);
            }
            else if (cursor.IsDeclaration)
            {
                VisitDeclaration(cursor);
            }
            else if (cursor.IsReference)
            {
                VisitCursor(cursor.Type.Declaration);
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
            if (_state.VisitedTypes.Contains(type))
            {
                return false;
            }

            _state.VisitedTypes.Add(type);
            return true;
        }

        [SuppressMessage("ReSharper", "TailRecursiveCall", Justification = "Easier to read.")]
        private void VisitType(CXType type)
        {
            if (!CanVisitType(type))
            {
                return;
            }

            switch (type.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Attributed:
                    VisitType(type.ModifiedType);
                    break;
                case CX_TypeClass.CX_TypeClass_Elaborated:
                    VisitType(type.NamedType);
                    break;
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_LValueReference:
                case CX_TypeClass.CX_TypeClass_RValueReference:
                    VisitType(type.PointeeType);
                    break;
                case CX_TypeClass.CX_TypeClass_Typedef:
                    VisitCursor(type.Declaration);
                    break;
                case CX_TypeClass.CX_TypeClass_IncompleteArray:
                    VisitType(type.ElementType);
                    break;
                case CX_TypeClass.CX_TypeClass_FunctionProto:
                    VisitFunctionProto(type);
                    break;
                case CX_TypeClass.CX_TypeClass_Record:
                case CX_TypeClass.CX_TypeClass_Enum:
                    VisitCursor(type.Declaration);
                    break;
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                    VisitType(type.ElementType);
                    break;
                case CX_TypeClass.CX_TypeClass_Builtin:
                    // Ignored
                    break;
                default:
                    var up = UnsupportedType(type);
                    throw up;
            }
        }

        private void VisitDeclaration(CXCursor declaration)
        {
            switch (declaration.DeclKind)
            {
                case CX_DeclKind.CX_DeclKind_Enum:
                    VisitEnum(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Record:
                    VisitRecord(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Typedef:
                    VisitTypedef(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_Function:
                    VisitFunction(declaration);
                    break;
                case CX_DeclKind.CX_DeclKind_ParmVar:
                case CX_DeclKind.CX_DeclKind_Field:
                    VisitType(declaration.Type);
                    break;
                default:
                    var up = UnsupportedDeclaration(declaration);
                    throw up;
            }
        }

        private void VisitFunction(CXCursor function)
        {
            VisitType(function.ResultType);
            function.VisitChildren(VisitCursor);
            _state.Functions.Add(function);
        }

        private void VisitEnum(CXCursor @enum)
        {
            _state.Enums.Add(@enum);
        }

        private void VisitRecord(CXCursor record)
        {
            RecordVisitChildren();

            if (record.IsAnonymous)
            {
                return;
            }

            _state.Records.Add(record);

            void RecordVisitChildren()
            {
                if (record.kind == CXCursorKind.CXCursor_TypedefDecl)
                {
                    var type = record.TypedefDeclUnderlyingType;
                    if (type.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
                    {
                        var namedType = type.NamedType;
                        var namedRecord = namedType.Declaration;
                        namedRecord.VisitChildren(VisitCursor);
                    }
                    else
                    {
                        var underlyingRecord = type.Declaration;
                        underlyingRecord.VisitChildren(VisitCursor);
                    }
                }
                else
                {
                    record.VisitChildren(VisitCursor);
                }
            }
        }

        private void VisitTypedef(CXCursor typedef)
        {
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
                    VisitType(typedef.TypedefDeclUnderlyingType);
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
                                recordType.Declaration.VisitChildren(_ => childrenCount += 1);
                                if (childrenCount == 0)
                                {
                                    VisitOpaqueType(typedef);
                                }
                            }

                            break;
                        }

                        case CX_TypeClass.CX_TypeClass_FunctionProto:
                            VisitFunctionProto(pointeeType);
                            break;
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
                    return;
                }

                switch (namedType.TypeClass)
                {
                    case CX_TypeClass.CX_TypeClass_Record:
                        VisitRecord(typedef);
                        break;
                    case CX_TypeClass.CX_TypeClass_Enum:
                        VisitEnum(typedef);
                        break;
                    default:
                        var up = UnexpectedScenario();
                        throw up;
                }
            }
        }

        private void VisitOpaqueType(CXCursor opaqueType)
        {
            _state.OpaqueTypes.Add(opaqueType);
        }

        private void VisitSystemCursor(CXCursor systemCursor)
        {
            _state.SystemCursors.Add(systemCursor);
        }

        private void VisitFunctionProto(CXType functionProto)
        {
            VisitType(functionProto.ResultType);
            functionProto.VisitChildren(child => VisitType(child.Type));
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
