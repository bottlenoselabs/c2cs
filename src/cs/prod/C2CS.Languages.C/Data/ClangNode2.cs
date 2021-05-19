// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static libclang;

namespace C2CS.Languages.C
{
    public readonly struct ClangNode2
    {
        public readonly string Identifier;
        public readonly string TypeName;
        public readonly ClangNodeKind Kind;
        public readonly CXType? Type;
        public readonly CXCursor Cursor;
        public readonly CXCursor CursorParent;

        public ClangNode2(
            string identifier,
            string typeName,
            ClangNodeKind kind,
            CXType? type,
            CXCursor cursor,
            CXCursor cursorParent)
        {
            Identifier = identifier;
            TypeName = typeName;
            Type = type;
            Cursor = cursor;
            CursorParent = cursorParent;
            Kind = kind;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Identifier))
            {
                var location = new ClangCodeLocation(Cursor);
                return @$"{Kind} {TypeName} @ {location}";
            }
            else
            {
                var location = new ClangCodeLocation(Cursor);
                return @$"{Kind} '{Identifier}': {TypeName} @ {location}";
            }
        }
    }
}
