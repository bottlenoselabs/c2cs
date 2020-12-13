// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using ClangSharp;
using ClangSharp.Interop;
using Type = ClangSharp.Type;

namespace C2CS
{
    internal class CodeCStructLayoutCalculator
    {
        private static readonly Dictionary<Decl, DeclLayout> _layoutsByDeclaration = new Dictionary<Decl, DeclLayout>();

        public DeclLayout GetLayout(Decl declaration)
        {
            if (_layoutsByDeclaration.TryGetValue(declaration, out var layout))
            {
                return layout;
            }

            layout = declaration switch
            {
                EnumDecl @enum => GetLayoutForEnum(@enum),
                FieldDecl field => GetLayoutForField(field),
                RecordDecl record => GetLayoutForRecord(record),
                _ => throw new Exception($"Declaration is not yet supported: {declaration}")
            };

            _layoutsByDeclaration.Add(declaration, layout);
            return layout;
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private DeclLayout GetLayoutForEnum(EnumDecl @enum)
        {
            var maxSize = 0;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var decl in @enum.Decls)
            {
                var constant = (EnumConstantDecl)decl;
                var layout = GetLayoutForType(decl, constant.Type.CanonicalType);
                if (layout.Size > maxSize)
                {
                    maxSize = layout.Size;
                }
            }

            return new DeclLayout(@enum, maxSize, Math.Min(maxSize, 8));
        }

        private DeclLayout GetLayoutForField(FieldDecl field)
        {
            var typeLayout = GetLayoutForType(field, field.Type.CanonicalType);
            return new DeclLayout(field, typeLayout.Size, typeLayout.Alignment);
        }

        private DeclLayout GetLayoutForType(Decl decl, Type type)
        {
            switch (type)
            {
                case PointerType _:
                    return new DeclLayout(decl, 8, 8);
                case BuiltinType builtin:
                    var size = builtin.Handle.SizeOf;
                    return new DeclLayout(decl, (int)size, Math.Min((int)size, 8));
                case EnumType @enum:
                    return GetLayout(@enum.Decl);
                case ConstantArrayType array:
                    var elementLayout = GetLayoutForType(decl, array.ElementType);
                    var size2 = elementLayout.Size * array.Size;
                    return new DeclLayout(decl, (int)size2, elementLayout.Alignment);
                case RecordType record:
                    return GetLayout(record.Decl);
                default:
                    throw new Exception($"Type is not yet supported: {type}");
            }
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private DeclLayout GetLayoutForRecord(RecordDecl record)
        {
            var declarations = record.Definition.Decls;
            if (declarations.Count == 0)
            {
                return new DeclLayout(record, 0, 0);
            }

            var alignment = 1;
            var structSize = 0;
            if (record.CursorKind == CXCursorKind.CXCursor_UnionDecl)
            {
                // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
                foreach (var declaration in declarations)
                {
                    var layout = GetLayout(declaration);
                    structSize = Math.Max(structSize, layout.Size);
                    alignment = Math.Max(alignment, layout.Alignment);
                }
            }
            else
            {
                var fieldAddress = 0;
                var layout = GetLayout(declarations[0]);
                var currentPackedSize = layout.Size;
                var previousLayout = layout;
                alignment = Math.Max(alignment, layout.Alignment);

                for (var i = 1; i < declarations.Count; i++)
                {
                    var declaration = declarations[i];
                    layout = GetLayout(declaration);

                    alignment = Math.Max(alignment, layout.Alignment);

                    var nextPackedSize = currentPackedSize + layout.Size;
                    var nextFieldAddress = fieldAddress + previousLayout.Size;
                    var fieldPadding = 0;

                    if (nextPackedSize < layout.Alignment)
                    {
                        currentPackedSize = nextPackedSize;
                        fieldAddress = nextFieldAddress;
                    }
                    else
                    {
                        if (nextFieldAddress % layout.Alignment == 0)
                        {
                            currentPackedSize = 0;
                            fieldAddress = nextFieldAddress;
                        }
                        else
                        {
                            var nextAlignedAddressOvershoot = nextFieldAddress + layout.Alignment;
                            var nextAlignedAddress =
                                nextAlignedAddressOvershoot - (nextAlignedAddressOvershoot % layout.Alignment);
                            fieldPadding = nextAlignedAddress - nextFieldAddress;
                            currentPackedSize = 0;
                            fieldAddress = nextFieldAddress + fieldPadding;
                        }
                    }

                    layout.FieldAddress = fieldAddress;
                    if (layout.Decl.CursorKind == CXCursorKind.CXCursor_UnionDecl)
                    {
                        var union = (RecordDecl)layout.Decl;
                        foreach (var field in union.Fields)
                        {
                            var unionFieldLayout = GetLayout(field);
                            unionFieldLayout.FieldAddress = fieldAddress;
                        }
                    }

                    previousLayout.FieldPadding = fieldPadding;
                    if (previousLayout.Decl.CursorKind == CXCursorKind.CXCursor_UnionDecl)
                    {
                        var union = (RecordDecl)previousLayout.Decl;
                        foreach (var field in union.Fields)
                        {
                            var unionFieldLayout = GetLayout(field);
                            unionFieldLayout.FieldPadding = fieldPadding;
                        }
                    }

                    previousLayout = layout;
                }

                structSize = fieldAddress + previousLayout.Size;
                if (structSize % alignment != 0)
                {
                    var packedUnits = (structSize / alignment) + 1;
                    var actualStructSize = packedUnits * alignment;
                    var trailingFieldPadding = actualStructSize - structSize;
                    previousLayout.FieldPadding = trailingFieldPadding;
                    structSize = actualStructSize;
                }
            }

            return new DeclLayout(record, structSize, alignment);
        }

        public class DeclLayout
        {
            public Decl Decl { get; }

            public int Size { get; }

            public int Alignment { get; }

            public int FieldAddress { get; set; }

            public int FieldPadding { get; set; }

            internal DeclLayout(Decl decl, int size, int alignment)
            {
                Decl = decl;
                Size = size;
                Alignment = alignment;
            }

            public override string ToString()
            {
                return Decl.Spelling;
            }
        }
    }
}
