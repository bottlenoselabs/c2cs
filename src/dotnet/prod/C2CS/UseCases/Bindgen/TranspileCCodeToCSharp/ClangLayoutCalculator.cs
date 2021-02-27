// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using ClangSharp.Interop;

namespace C2CS
{
    internal class ClangLayoutCalculator
    {
        private readonly Dictionary<CXCursor, ClangLayout> _layoutsByCursor = new();

        public ClangLayout CalculateLayout(CXCursor declaration)
        {
            if (_layoutsByCursor.TryGetValue(declaration, out var layout))
            {
                return layout;
            }

            layout = declaration.DeclKind switch
            {
                CX_DeclKind.CX_DeclKind_Enum => GetLayoutForEnum(declaration),
                CX_DeclKind.CX_DeclKind_Field => GetLayoutForField(declaration),
                CX_DeclKind.CX_DeclKind_Record => GetLayoutForRecord(declaration),
                CX_DeclKind.CX_DeclKind_Typedef => GetLayoutForTypedef(declaration),
                _ => throw new NotImplementedException($"Declaration is not yet supported: {declaration}")
            };

            _layoutsByCursor.Add(declaration, layout);
            return layout;
        }

        private ClangLayout GetLayoutForTypedef(CXCursor clangTypedef)
        {
            var clangUnderlyingType = clangTypedef.TypedefDeclUnderlyingType;
            if (clangUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
            {
                var clangNamedType = clangUnderlyingType.NamedType;
                return CalculateLayout(clangNamedType.Declaration);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private ClangLayout GetLayoutForEnum(CXCursor clangEnum)
        {
            var maxSize = 0;

            clangEnum.VisitChildren(child =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return;
                }

                var layout = GetLayoutForType(child, child.Type.CanonicalType);
                if (layout.Size > maxSize)
                {
                    maxSize = layout.Size;
                }
            });

            return new ClangLayout(clangEnum, maxSize, maxSize);
        }

        private ClangLayout GetLayoutForField(CXCursor field)
        {
            var typeLayout = GetLayoutForType(field, field.Type.CanonicalType);
            return new ClangLayout(field, typeLayout.Size, typeLayout.Alignment);
        }

        private ClangLayout GetLayoutForType(CXCursor cursor, CXType type)
        {
            switch (type.TypeClass)
            {
                case CX_TypeClass.CX_TypeClass_Pointer:
                case CX_TypeClass.CX_TypeClass_Builtin:
                    var size = type.SizeOf;
                    var alignment = type.AlignOf;
                    return new ClangLayout(cursor, (int) size, (int) alignment);
                case CX_TypeClass.CX_TypeClass_Enum:
                    return CalculateLayout(type.Declaration);
                case CX_TypeClass.CX_TypeClass_ConstantArray:
                    var elementLayout = GetLayoutForType(cursor, type.ElementType);
                    var size2 = elementLayout.Size * type.ArraySize;
                    return new ClangLayout(cursor, (int) size2, elementLayout.Alignment);
                case CX_TypeClass.CX_TypeClass_Record:
                    return CalculateLayout(type.Declaration);
                default:
                    throw new NotImplementedException($"Type is not yet supported: {type}");
            }
        }

        private ClangLayout GetLayoutForRecord(CXCursor record)
        {
            var declarations = new List<CXCursor>();
            record.Definition.VisitChildren(child =>
            {
                if (child.IsDeclaration)
                {
                    declarations.Add(child);
                }
            });

            if (declarations.Count == 0)
            {
                return new ClangLayout(record, 0, 0);
            }

            var alignment = 1;
            var size = 0;
            if (record.kind == CXCursorKind.CXCursor_UnionDecl)
            {
                CalculateSizeAndAlignmentForUnion();
            }
            else
            {
                CalculateSizeAndAlignmentForStruct();
            }

            return new ClangLayout(record, size, alignment);

            void CalculateSizeAndAlignmentForUnion()
            {
                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var declaration in declarations)
                {
                    CalculateSizeAndAlignmentForUnionField(declaration);
                }
            }

            void CalculateSizeAndAlignmentForUnionField(CXCursor declaration)
            {
                var fieldLayout = CalculateLayout(declaration);
                size = Math.Max(size, fieldLayout.Size);
                alignment = Math.Max(alignment, fieldLayout.Alignment);
            }

            void CalculateSizeAndAlignmentForStruct()
            {
                var fieldLayout = CalculateLayout(declarations[0]);
                var previousMemberLayout = fieldLayout;
                alignment = Math.Max(alignment, fieldLayout.Alignment);

                var packedSize = fieldLayout.Size;
                var fieldAddress = 0;
                for (var i = 1; i < declarations.Count; i++)
                {
                    var declaration = declarations[i];
                    fieldLayout = CalculateLayout(declaration);
                    alignment = Math.Max(alignment, fieldLayout.Alignment);
                    CalculatePackForStructField(
                        fieldLayout,
                        previousMemberLayout,
                        ref fieldAddress,
                        ref packedSize);
                    previousMemberLayout = fieldLayout;
                }

                size = fieldAddress + previousMemberLayout.Size;
                if (size % alignment != 0)
                {
                    var packedUnits = (size / alignment) + 1;
                    var actualStructSize = packedUnits * alignment;
                    var trailingFieldPadding = actualStructSize - size;
                    previousMemberLayout.FieldPadding = trailingFieldPadding;
                    size = actualStructSize;
                }
            }

            void CalculatePackForStructField(
                ClangLayout fieldLayout,
                ClangLayout previousFieldLayout,
                ref int fieldAddress,
                ref int packSize)
            {
                fieldAddress += previousFieldLayout.Size;
                var previousFieldPadding = 0;

                if (packSize + fieldLayout.Size < fieldLayout.Alignment)
                {
                    packSize += fieldLayout.Size;
                }
                else
                {
                    if (fieldAddress % fieldLayout.Alignment == 0)
                    {
                        packSize = 0;
                    }
                    else
                    {
                        packSize = 0;
                        var alignedAddressOvershoot = fieldAddress + fieldLayout.Alignment;
                        var alignedAddress = alignedAddressOvershoot -
                                             (alignedAddressOvershoot % fieldLayout.Alignment);
                        previousFieldPadding = alignedAddress - fieldAddress;
                        fieldAddress += previousFieldPadding;
                    }
                }

                fieldLayout.FieldAddress = fieldAddress;
                previousFieldLayout.FieldPadding = previousFieldPadding;
            }
        }

        public class ClangLayout
        {
            public readonly CXCursor Cursor;
            public readonly int Size;
            public readonly int Alignment;
            public int FieldAddress;
            public int FieldPadding;

            internal ClangLayout(CXCursor cursor, int size, int alignment)
            {
                Cursor = cursor;
                Size = size;
                Alignment = alignment;
            }

            public override string ToString()
            {
                return Cursor.Spelling.CString;
            }
        }
    }
}
