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
				_ => throw new NotImplementedException($"Declaration is not yet supported: {declaration}")
			};

			_layoutsByCursor.Add(declaration, layout);
			return layout;
		}

		// ReSharper disable once SuggestBaseTypeForParameter
		private ClangLayout GetLayoutForEnum(CXCursor clangEnum)
		{
			var maxSize = 0;

			// ReSharper disable once LoopCanBeConvertedToQuery
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

			return new ClangLayout(clangEnum, maxSize, Math.Min(maxSize, 8));
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
					return new ClangLayout(cursor, (int)size, (int)alignment);
				case CX_TypeClass.CX_TypeClass_Enum:
					return CalculateLayout(type.Declaration);
				case CX_TypeClass.CX_TypeClass_ConstantArray:
					var elementLayout = GetLayoutForType(cursor, type.ElementType);
					var size2 = elementLayout.Size * type.ArraySize;
					return new ClangLayout(cursor, (int)size2, elementLayout.Alignment);
				case CX_TypeClass.CX_TypeClass_Record:
					return CalculateLayout(type.Declaration);
				default:
					throw new NotImplementedException($"Type is not yet supported: {type}");
			}
		}

		// ReSharper disable once SuggestBaseTypeForParameter
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
			var structSize = 0;
			if (record.kind == CXCursorKind.CXCursor_UnionDecl)
			{
				// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
				foreach (var declaration in declarations)
				{
					var layout = CalculateLayout(declaration);
					structSize = Math.Max(structSize, layout.Size);
					alignment = Math.Max(alignment, layout.Alignment);
				}
			}
			else
			{
				var fieldAddress = 0;
				var layout = CalculateLayout(declarations[0]);
				var currentPackedSize = layout.Size;
				var previousLayout = layout;
				alignment = Math.Max(alignment, layout.Alignment);

				for (var i = 1; i < declarations.Count; i++)
				{
					var declaration = declarations[i];
					layout = CalculateLayout(declaration);

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
					if (layout.Cursor.kind == CXCursorKind.CXCursor_UnionDecl)
					{
						var union = layout.Cursor;
						var unionFields = new List<CXCursor>();
						layout.Cursor.VisitChildren(child =>
						{
							if (child.kind == CXCursorKind.CXCursor_FieldDecl)
							{
								unionFields.Add(child);
							}
						});
						foreach (var field in unionFields)
						{
							var unionFieldLayout = CalculateLayout(field);
							unionFieldLayout.FieldAddress = fieldAddress;
						}
					}

					previousLayout.FieldPadding = fieldPadding;
					if (previousLayout.Cursor.kind == CXCursorKind.CXCursor_UnionDecl)
					{
						var unionFields = new List<CXCursor>();
						layout.Cursor.VisitChildren(child =>
						{
							if (child.kind == CXCursorKind.CXCursor_FieldDecl)
							{
								unionFields.Add(child);
							}
						});
						foreach (var field in unionFields)
						{
							var unionFieldLayout = CalculateLayout(field);
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

			return new ClangLayout(record, structSize, alignment);
		}

		public class ClangLayout
		{
			public readonly CXCursor Cursor;
			public readonly int Size;
			public readonly int Alignment;
			public int FieldAddress;
			public int FieldPadding;

			internal ClangLayout(CXCursor record, int size, int alignment)
			{
				Cursor = record;
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
