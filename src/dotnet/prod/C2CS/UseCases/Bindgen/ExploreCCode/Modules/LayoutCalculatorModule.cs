// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

// using System;
// using System.Collections.Generic;
// using ClangSharp.Interop;
//
// namespace C2CS.Bindgen.ExploreCCode
// {
//     public class CLayoutCalculatorModule
//     {
//         private readonly Dictionary<CXType, CLayout> _layoutsByClangType = new();
//
//         public CLayout GetLayout(CXType clangType)
//         {
//             if (_layoutsByClangType.TryGetValue(clangType, out var layout))
//             {
//                 return layout;
//             }
//
//             var clangTypeClass = clangType.TypeClass;
//             layout = clangTypeClass switch
//             {
//                 CX_TypeClass.CX_TypeClass_Pointer => GetLayoutForTerminalType(clangType),
//                 CX_TypeClass.CX_TypeClass_Builtin => GetLayoutForTerminalType(clangType),
//                 CX_TypeClass.CX_TypeClass_Enum => GetLayoutForTerminalType(clangType),
//                 CX_TypeClass.CX_TypeClass_Record => GetLayoutForRecord(clangType),
//                 CX_TypeClass.CX_TypeClass_Typedef => GetLayoutForTypedef(clangType),
//                 CX_TypeClass.CX_TypeClass_ConstantArray => GetLayoutForConstArray(clangType),
//                 _ => throw new NotImplementedException()
//             };
//
//             _layoutsByClangType.Add(clangType, layout);
//             return layout;
//         }
//
//         private CLayout GetLayoutForTerminalType(CXType clangType)
//         {
//             var size = (int)clangType.SizeOf;
//             var alignment = (int)clangType.AlignOf;
//             var layout = new CLayout(size, alignment);
//             return layout;
//         }
//
//         private CLayout GetLayoutForTypedef(CXType typedef)
//         {
//             var clangUnderlyingType = typedef.Declaration.TypedefDeclUnderlyingType;
//             CLayout layout;
//
//             if (clangUnderlyingType.TypeClass == CX_TypeClass.CX_TypeClass_Elaborated)
//             {
//                 var clangNamedType = clangUnderlyingType.NamedType;
//                 layout = GetLayout(clangNamedType);
//             }
//             else
//             {
//                 layout = GetLayout(clangUnderlyingType);
//             }
//
//             return layout;
//         }
//
//         private CLayout GetLayoutForConstArray(CXType type)
//         {
//             var elementLayout = GetLayout(type.ElementType);
//             var size2 = elementLayout.Size * type.ArraySize;
//             return new CLayout((int) size2, elementLayout.Alignment);
//         }
//
//         private CLayout GetLayoutForRecord(CXType record)
//         {
//             var declarations = new List<CXCursor>();
//             record.VisitChildren(child =>
//             {
//                 if (child.IsDeclaration)
//                 {
//                     declarations.Add(child);
//                 }
//             });
//
//             if (declarations.Count == 0)
//             {
//                 return new CLayout((int)record.SizeOf, (int)record.AlignOf);
//             }
//
//             var size = 0;
//             if (record.Declaration.kind == CXCursorKind.CXCursor_UnionDecl)
//             {
//                 CalculateSizeForUnion();
//             }
//             else
//             {
//                 CalculateSizeForStruct();
//             }
//
//             var alignment = (int)record.AlignOf;
//             return new CLayout(size, alignment);
//
//             void CalculateSizeForUnion()
//             {
//                 // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
//                 foreach (var declaration in declarations)
//                 {
//                     CalculateSizeForUnionField(declaration.Type);
//                 }
//             }
//
//             void CalculateSizeForUnionField(CXCursor declaration)
//             {
//                 var fieldLayout = GetLayout(declaration);
//                 size = Math.Max(size, fieldLayout.Size);
//             }
//
//             void CalculateSizeForStruct()
//             {
//                 var fieldLayout = GetLayout(declarations[0]);
//                 var previousMemberLayout = fieldLayout;
//
//                 var packedSize = fieldLayout.Size;
//                 var fieldAddress = 0;
//                 for (var i = 1; i < declarations.Count; i++)
//                 {
//                     var declaration = declarations[i];
//                     fieldLayout = GetLayout(declaration);
//                     CalculatePackForStructField(
//                         fieldLayout,
//                         previousMemberLayout,
//                         ref fieldAddress,
//                         ref packedSize);
//                     previousMemberLayout = fieldLayout;
//                 }
//
//                 size = fieldAddress + previousMemberLayout.Size;
//                 var align = (int)record.Type.AlignOf;
//                 if (size % align != 0)
//                 {
//                     var packedUnits = (size / align) + 1;
//                     var actualStructSize = packedUnits * align;
//                     var trailingFieldPadding = actualStructSize - size;
//                     previousMemberLayout.FieldPadding = trailingFieldPadding;
//                     size = actualStructSize;
//                 }
//             }
//
//             void CalculatePackForStructField(
//                 CLayout fieldLayout,
//                 CLayout previousFieldLayout,
//                 ref int fieldAddress,
//                 ref int packSize)
//             {
//                 fieldAddress += previousFieldLayout.Size;
//                 var previousFieldPadding = 0;
//
//                 if (packSize + fieldLayout.Size < fieldLayout.Alignment)
//                 {
//                     packSize += fieldLayout.Size;
//                 }
//                 else
//                 {
//                     if (fieldAddress % fieldLayout.Alignment == 0)
//                     {
//                         packSize = 0;
//                     }
//                     else
//                     {
//                         packSize = 0;
//                         var alignedAddressOvershoot = fieldAddress + fieldLayout.Alignment;
//                         var alignedAddress = alignedAddressOvershoot -
//                                              (alignedAddressOvershoot % fieldLayout.Alignment);
//                         previousFieldPadding = alignedAddress - fieldAddress;
//                         fieldAddress += previousFieldPadding;
//                     }
//                 }
//
//                 fieldLayout.FieldAddress = fieldAddress;
//                 previousFieldLayout.FieldPadding = previousFieldPadding;
//             }
//         }
//     }
// }
