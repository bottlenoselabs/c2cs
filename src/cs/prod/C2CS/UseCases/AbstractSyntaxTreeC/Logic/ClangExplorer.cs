// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using C2CS.UseCases.BindgenCSharp;
using static libclang;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class ClangExplorer
    {
        private readonly DiagnosticsSink _diagnostics;
        private readonly ImmutableHashSet<string> _ignoredFiles;
        private readonly ImmutableHashSet<string> _opaqueTypeNames;
        private readonly ImmutableArray<string> _includeDirectories;
        private readonly List<Node> _frontier = new();
        private readonly Dictionary<string, bool> _validTypeNames = new();
        private readonly Dictionary<string, CType> _typesByName = new();
        private readonly List<CType> _types = new();
        private readonly List<CVariable> _variables = new();
        private readonly List<CFunction> _functions = new();
        private readonly List<CEnum> _enums = new();
        private readonly List<CRecord> _records = new();
        private readonly List<COpaqueType> _opaqueDataTypes = new();
        private readonly List<CTypedef> _typedefs = new();
        private readonly List<CFunctionPointer> _functionPointers = new();
        private readonly HashSet<string> _systemIgnoredTypeNames = new()
        {
            "FILE",
            "DIR",
            "pid_t",
            "uid_t",
            "gid_t",
            "time_t",
            "pthread_t",
            "sockaddr",
            "addrinfo",
            "sockaddr_in",
            "sockaddr_in6",
            "socklen_t",
            "size_t",
            "ssize_t",
            "int8_t",
            "uint8_t",
            "int16_t",
            "uint16_t",
            "int32_t",
            "uint32_t",
            "int64_t",
            "uint64_t",
            "uintptr_t",
            "intptr_t",
            "va_list",
        };

        public ClangExplorer(DiagnosticsSink diagnostics, Configuration configuration)
        {
            _diagnostics = diagnostics;
            _ignoredFiles = configuration.IgnoredFiles.ToImmutableHashSet();
            _includeDirectories = configuration.IncludeDirectories;
            _opaqueTypeNames = configuration.OpaqueTypes.ToImmutableHashSet();
        }

        public CAbstractSyntaxTree AbstractSyntaxTree(CXTranslationUnit translationUnit)
        {
            ExpandTranslationUnit(translationUnit);
            Explore();

            var cursor = clang_getTranslationUnitCursor(translationUnit);
            var location = Location(cursor);

            var functions = _functions.ToImmutableArray();
            var functionPointers = _functionPointers.ToImmutableArray();
            var records = _records.ToImmutableArray();
            var enums = _enums.ToImmutableArray();
            var opaqueTypes = _opaqueDataTypes.ToImmutableArray();
            var typedefs = _typedefs.ToImmutableArray();
            var variables = _variables.ToImmutableArray();

            return new CAbstractSyntaxTree
            {
                FileName = location.Path,
                Functions = functions,
                FunctionPointers = functionPointers,
                Records = records,
                Enums = enums,
                OpaqueTypes = opaqueTypes,
                Typedefs = typedefs,
                Variables = variables,
                Types = _types.ToImmutableArray()
            };
        }

        private void ExpandTranslationUnit(CXTranslationUnit translationUnit)
        {
            var cursor = clang_getTranslationUnitCursor(translationUnit);
            var type = clang_getCursorType(cursor);
            var location = Location(cursor);
            ExpandNode(
                CKind.TranslationUnit,
                location,
                null,
                cursor,
                cursor,
                type,
                type,
                string.Empty,
                string.Empty);
        }

        private void Explore()
        {
            // The idea is that we keep track of nodes we wish to process next when we encounter them.
            //  The concept of "frontier" comes from artificial intelligence textbooks on the subject of graph
            //  traversal such as solving mazes or otherwise "exploring" the environment for a solution.
            //  Here we use it as an array which acts like stack data structure with first-in-last-out (FILO) behaviour.
            //  I.e., we add append items to the end of the array, and remove items from end of the array.
            //  Using a stack data structures with FILO leads to behaviour of depth-first-search (DFS) with post-order
            //  traversal. I.e., starting from a the root of the tree (the translation unit) we recursively go deeper
            //  and deeper into children nodes (cursors that are of interest such as an Enum, Struct, Function, etc) until
            //  we hit a leaf (some cursor that has no children cursors). As we go along we visit the last node (cursor)
            //  that was expanded. This is appropriate because we want to add nodes as we encounter them along
            //  our graph traversal journey but not immediately process them; rather we want to process the next node
            //  once we are finished processing our current node.

            while (_frontier.Count > 0)
            {
                var node = _frontier[^1];
                _frontier.RemoveAt(_frontier.Count - 1); // RemoveAt() allows us to achieve O(1), while Remove() is O(n)
                ExploreNode(node);
            }
        }

        private void ExploreNode(Node node)
        {
            switch (node.Kind)
            {
                case CKind.TranslationUnit:
                    ExploreTranslationUnit(node);
                    break;
                case CKind.Variable:
                    ExploreVariable(node.Name!, node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                    break;
                case CKind.Function:
                    ExploreFunction(node.Name!, node.Cursor, node.Type, node.Location, node.Parent!);
                    break;
                case CKind.Typedef:
                    ExploreTypedef(node, node.Parent!);
                    break;
                case CKind.OpaqueType:
                    ExploreOpaqueType(node.TypeName!, node.Location);
                    break;
                case CKind.Enum:
                    ExploreEnum(node.TypeName!, node.Cursor, node.Type, node.Location, node.Parent!);
                    break;
                case CKind.Record:
                    ExploreRecord(node, node.Parent!);
                    break;
                case CKind.FunctionPointer:
                    ExploreFunctionPointer(node.TypeName!, node.Cursor, node.Type, node.OriginalType, node.Location, node.Parent!);
                    break;
                case CKind.Pointer:
                case CKind.Primitive:
                    break;
                default:
                    var up = new ClangExplorerException($"Unknown explorer node '{node.Kind}'.");
                    throw up;
            }
        }

        private bool TypeIsIgnored(CXType type, CXCursor cursor)
        {
            var fileLocation = type.FileLocation(cursor);
            foreach (var includeDirectory in _includeDirectories)
            {
                if (!fileLocation.Path.Contains(includeDirectory))
                {
                    continue;
                }

                fileLocation.Path = fileLocation.Path.Replace(includeDirectory, string.Empty).Trim('/', '\\');
                break;
            }

            return _ignoredFiles.Contains(fileLocation.Path);
        }

        private void ExploreTranslationUnit(Node node)
        {
            var externCursors = node.Cursor.GetDescendents(IsExternCursor);

            foreach (var externCursor in externCursors)
            {
                ExpandExtern(node, externCursor);
            }

            static bool IsExternCursor(CXCursor cursor, CXCursor cursorParent)
            {
                var kind = clang_getCursorKind(cursor);
                if (kind != CXCursorKind.CXCursor_FunctionDecl && kind != CXCursorKind.CXCursor_VarDecl)
                {
                    return false;
                }

                var linkage = clang_getCursorLinkage(cursor);
                var isExternallyLinked = linkage == CXLinkageKind.CXLinkage_External;
                if (!isExternallyLinked)
                {
                    return false;
                }

                var isSystemCursor = cursor.IsSystem();
                return !isSystemCursor;
            }
        }

        private void ExpandExtern(Node parentNode, CXCursor cursor)
        {
            var kind = cursor.kind switch
            {
                CXCursorKind.CXCursor_FunctionDecl => CKind.Function,
                CXCursorKind.CXCursor_VarDecl => CKind.Variable,
                _ => CKind.Unknown
            };

            if (kind == CKind.Unknown)
            {
                var up = new ClangExplorerException($"Unexpected extern kind '{cursor.kind}'.");
                throw up;
            }

            var type = clang_getCursorType(cursor);
            var typeName = TypeName(parentNode.TypeName!, kind, type, cursor);
            var name = cursor.Name();
            var location = Location(cursor, type);

            var isIgnored = TypeIsIgnored(type, cursor);
            if (isIgnored)
            {
                return;
            }

            ExpandNode(kind, location, parentNode, cursor, cursor, type, type, name, typeName);
        }

        private void ExploreVariable(string name, string typeName, CXCursor cursor, CXType type, ClangLocation location, Node parentNode)
        {
            ExpandType(parentNode, cursor, type, type, typeName);

            var variable = new CVariable
            {
                Location = location,
                Name = name,
                Type = typeName
            };

            _variables.Add(variable);
        }

        private void ExploreFunction(string name, CXCursor cursor, CXType type, ClangLocation location, Node parentNode)
        {
            var callingConvention = CreateFunctionCallingConvention(type);
            var resultType = clang_getCursorResultType(cursor);
            var resultTypeName = TypeName(parentNode.TypeName!, CKind.FunctionResult, resultType, cursor);

            ExpandType(parentNode, cursor, resultType, resultType, resultTypeName);

            var parameters = CreateFunctionParameters(cursor, parentNode);

            var function = new CFunction
            {
                Name = name,
                Location = location,
                CallingConvention = callingConvention,
                ReturnType = resultTypeName,
                Parameters = parameters
            };

            _functions.Add(function);
        }

        private void ExploreEnum(string typeName, CXCursor cursor, CXType type, ClangLocation location, Node parentNode)
        {
            var integerType = clang_getEnumDeclIntegerType(cursor);
            var integerTypeName = TypeName(parentNode.TypeName!, CKind.Enum, integerType, cursor);

            ExpandType(parentNode, cursor, integerType, integerType, integerTypeName);

            var enumValues = CreateEnumValues(cursor);

            var @enum = new CEnum
            {
                Name = typeName,
                Location = location,
                Type = typeName,
                IntegerType = integerTypeName,
                Values = enumValues
            };

            _enums.Add(@enum);
        }

        private void ExploreRecord(Node node, Node parentNode)
        {
            var typeName = node.TypeName!;
            var cursor = node.Cursor;
            var location = node.Location;

            var fields = CreateRecordFields(typeName, cursor, parentNode);
            var nestedNodes = CreateNestedNodes(typeName, cursor, node);

            var nestedRecords = nestedNodes.Where(x => x is CRecord).Cast<CRecord>().ToImmutableArray();
            var nestedFunctionPointers = nestedNodes.Where(x => x is CFunctionPointer).Cast<CFunctionPointer>().ToImmutableArray();

            var typeCursor = clang_getTypeDeclaration(node.Type);
            var isUnion = typeCursor.kind == CXCursorKind.CXCursor_UnionDecl;

            var record = new CRecord
            {
                Location = location,
                IsUnion = isUnion,
                Type = typeName,
                Fields = fields,
                NestedRecords = nestedRecords,
                NestedFunctionPointers = nestedFunctionPointers
            };

            var isAnonymous = clang_Cursor_isAnonymous(typeCursor) > 0;
            if (isAnonymous)
            {
                // if it's the case that the record's type is anonymous then it must be a struct/union without a name
                //  which is already inside another struct; thus, it will be properly stored in that parent struct and
                //  should not be added directly here
                return;
            }

            _records.Add(record);
        }

        private void ExploreTypedef(Node node, Node parentNode)
        {
            var typeName = node.TypeName!;
            var cursor = node.Cursor;
            var type = node.Type;
            var location = node.Location;

            var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
            var (aliasKind, aliasType) = TypeKind(underlyingType);
            var aliasCursor = clang_getTypeDeclaration(aliasType);

            if (_opaqueTypeNames.Contains(typeName))
            {
                ExploreOpaqueType(typeName, location);
                return;
            }

            if (aliasKind == CKind.Enum)
            {
                ExploreEnum(typeName, aliasCursor, aliasType, location, parentNode);
                return;
            }

            if (aliasKind == CKind.Record)
            {
                ExploreRecord(node, parentNode);
                return;
            }

            if (aliasKind == CKind.FunctionPointer)
            {
                if (aliasCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
                {
                    aliasCursor = cursor;
                }

                ExploreFunctionPointer(typeName, aliasCursor, aliasType, type, location, parentNode);
                return;
            }

            var aliasTypeName = TypeName(parentNode.TypeName!, aliasKind, aliasType, aliasCursor);
            ExpandType(parentNode, aliasCursor, aliasType, aliasType, aliasTypeName);

            var typedef = new CTypedef
            {
                Name = typeName,
                Location = location,
                UnderlyingType = aliasTypeName
            };

            _typedefs.Add(typedef);
        }

        private void ExploreOpaqueType(string typeName, ClangLocation location)
        {
            var opaqueDataType = new COpaqueType
            {
                Name = typeName,
                Location = location
            };

            _opaqueDataTypes.Add(opaqueDataType);
        }

        private void ExploreFunctionPointer(string typeName, CXCursor cursor, CXType type, CXType originalType, ClangLocation location, Node parentNode)
        {
            // check if this function pointer originates from a typedef...
            //  typedefs always have a name, thus, if the function pointer originates from a typedef, the function pointer
            //  will have a name, otherwise the function pointer won't have a name
            //  if the function pointer does not have a name, we should not add it
            //  instead the function pointer will be added when mapping the nested struct
            var canVisit = originalType.kind == CXTypeKind.CXType_Typedef;
            if (!canVisit)
            {
                return;
            }

            if (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = clang_getPointeeType(type);
            }

            var functionPointer = CreateFunctionPointer(typeName, cursor, parentNode, originalType, type, location);

            _functionPointers.Add(functionPointer);
        }

        private bool TypeNameIsValid(CXType type, string typeName)
        {
            if (_validTypeNames.TryGetValue(typeName, out var value))
            {
                return value;
            }

            var isSystem = type.IsSystem();
            var isIgnored = _systemIgnoredTypeNames.Contains(typeName);
            if (isSystem && isIgnored)
            {
                _diagnostics.Add(new DiagnosticSystemTypeIgnored(type));
                value = false;
            }
            else
            {
                value = true;
            }

            _validTypeNames.Add(typeName, value);
            return value;
        }

        private bool RegisterTypeIsNew(string typeName, CXType type, CXCursor cursor)
        {
            if (typeName == string.Empty)
            {
                return true;
            }

            var alreadyVisited = _typesByName.TryGetValue(typeName, out var typeC);
            if (alreadyVisited)
            {
                // attempt to see if we have a definition for a previous opaque type, to which we should that info instead
                //  this can happen if one header file has a forward type, but another header file has the definition
                if (typeC!.Kind != CKind.OpaqueType)
                {
                    return false;
                }

                var typeKind = TypeKind(type);
                if (typeKind.Kind == CKind.OpaqueType)
                {
                    return false;
                }

                typeC = Type(typeName, cursor, type);
                _typesByName[typeName] = typeC;
                return true;
            }

            typeC = Type(typeName, cursor, type);

            _typesByName.Add(typeName, typeC);
            _types.Add(typeC);

            return true;
        }

        private void ExpandNode(
            CKind kind,
            ClangLocation location,
            Node? parent,
            CXCursor cursor,
            CXCursor originalCursor,
            CXType type,
            CXType originalType,
            string name,
            string typeName)
        {
            var node = new Node(
                kind,
                location,
                parent,
                cursor,
                originalCursor,
                type,
                originalType,
                name,
                typeName);
            _frontier.Add(node);
        }

        private static CFunctionCallingConvention CreateFunctionCallingConvention(CXType type)
        {
            var callingConvention = clang_getFunctionTypeCallingConv(type);
            var result = callingConvention switch
            {
                CXCallingConv.CXCallingConv_C => CFunctionCallingConvention.C,
                _ => throw new ClangExplorerException($"Unknown calling convention '{callingConvention}'.")
            };

            return result;
        }

        private ImmutableArray<CFunctionParameter> CreateFunctionParameters(CXCursor cursor, Node parentNode)
        {
            var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

            var parameterCursors = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var parameterCursor in parameterCursors)
            {
                var functionExternParameter = FunctionParameter(parameterCursor, parentNode);
                builder.Add(functionExternParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CFunctionParameter FunctionParameter(CXCursor cursor, Node parentNode)
        {
            var type = clang_getCursorType(cursor);
            var name = cursor.Name();

            var typeName = TypeName(parentNode.TypeName!, CKind.FunctionParameter, type, cursor);

            ExpandType(parentNode, cursor, type, type, typeName);
            var codeLocation = Location(cursor, type);

            return new CFunctionParameter
            {
                Name = name,
                Location = codeLocation,
                Type = typeName
            };
        }

        private CFunctionPointer CreateFunctionPointer(
            string typeName, CXCursor cursor, Node parentNode, CXType originalType, CXType type, ClangLocation location)
        {
            ExpandType(parentNode, cursor, type, originalType, typeName);

            var parameters = CreateFunctionPointerParameters(cursor, parentNode);

            var returnType = clang_getResultType(type);
            var returnTypeName = TypeName(parentNode.TypeName!, CKind.FunctionPointerResult, returnType, cursor);
            ExpandType(parentNode, cursor, returnType, returnType, returnTypeName);

            var parentKind = parentNode.Cursor.kind;
            var isWrapped = (parentKind == CXCursorKind.CXCursor_StructDecl ||
                             parentKind == CXCursorKind.CXCursor_TypedefDecl) &&
                            originalType.kind != CXTypeKind.CXType_Typedef;
            var name = cursor.Name();

            var functionPointer = new CFunctionPointer
            {
                Name = name,
                Location = location,
                Type = typeName,
                ReturnType = returnTypeName,
                Parameters = parameters,
                IsWrapped = isWrapped
            };

            return functionPointer;
        }

        private ImmutableArray<CFunctionPointerParameter> CreateFunctionPointerParameters(
            CXCursor cursor, Node parentNode)
        {
            var builder = ImmutableArray.CreateBuilder<CFunctionPointerParameter>();

            var parameterCursors = cursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_ParmDecl);

            foreach (var parameterCursor in parameterCursors)
            {
                var functionPointerParameter = CreateFunctionPointerParameter(parameterCursor, parentNode);
                builder.Add(functionPointerParameter);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CFunctionPointerParameter CreateFunctionPointerParameter(CXCursor cursor, Node parentNode)
        {
            var type = clang_getCursorType(cursor);
            var codeLocation = Location(cursor, type);
            var name = cursor.Name();
            var typeName = TypeName(parentNode.TypeName!, CKind.FunctionPointerParameter, type, cursor);
            ExpandType(parentNode, cursor, type, type, typeName);

            return new CFunctionPointerParameter
            {
                Name = name,
                Location = codeLocation,
                Type = typeName
            };
        }

        private ImmutableArray<CRecordField> CreateRecordFields(string recordName, CXCursor cursor, Node parentNode)
        {
            var builder = ImmutableArray.CreateBuilder<CRecordField>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);

            var type = clang_getCursorType(underlyingCursor);
            var typeCursor = clang_getTypeDeclaration(type);
            if (typeCursor.kind == CXCursorKind.CXCursor_UnionDecl ||
                typeCursor.kind == CXCursorKind.CXCursor_StructDecl)
            {
                underlyingCursor = typeCursor;
            }

            var fieldCursors = underlyingCursor.GetDescendents((child, _) =>
                child.kind == CXCursorKind.CXCursor_FieldDecl);

            foreach (var fieldCursor in fieldCursors)
            {
                var recordField = CreateRecordField(recordName, fieldCursor, parentNode);
                builder.Add(recordField);
            }

            CalculatePaddingForStructFields(cursor, builder);

            var result = builder.ToImmutable();
            return result;
        }

        private void CalculatePaddingForStructFields(
            CXCursor cursor,
            ImmutableArray<CRecordField>.Builder builder)
        {
            for (var i = 1; i < builder.Count; i++)
            {
                var recordField = builder[i];
                var fieldPrevious = builder[i - 1];
                var typeName = Value(fieldPrevious.Type);
                var type = _typesByName[typeName];
                var fieldPreviousTypeSizeOf = type!.SizeOf!.Value;
                var expectedFieldOffset = fieldPrevious.Offset + fieldPreviousTypeSizeOf;
                var hasPadding = recordField.Offset != 0 && recordField.Offset != expectedFieldOffset;
                if (!hasPadding)
                {
                    continue;
                }

                var padding = recordField.Offset - expectedFieldOffset;
                builder[i - 1].Padding = padding;
            }

            if (builder.Count >= 1)
            {
                var fieldLast = builder[^1];
                var cursorType = clang_getCursorType(cursor);
                var recordSize = (int) clang_Type_getSizeOf(cursorType);
                var typeName = Value(fieldLast.Type);
                var type = _typesByName[typeName];
                var fieldLastTypeSize = type.SizeOf!.Value;
                var expectedLastFieldOffset = recordSize - fieldLastTypeSize;
                if (fieldLast.Offset != expectedLastFieldOffset)
                {
                    var padding = expectedLastFieldOffset - fieldLast.Offset;
                    builder[^1].Padding = padding;
                }
            }
        }

        private string Value(string typeName)
        {
            if (typeName.EndsWith("*", StringComparison.InvariantCulture))
            {
                var pointeeTypeName = typeName.TrimEnd('*');
                var result2 = Value(pointeeTypeName);
                return typeName.Replace(pointeeTypeName, result2);
            }

            return typeName;
        }

        private CRecordField CreateRecordField(string recordName, CXCursor cursor, Node parentNode)
        {
            var name = cursor.Name();
            var type = clang_getCursorType(cursor);
            var codeLocation = Location(cursor, type);
            var typeName = TypeName(recordName, CKind.RecordField, type, cursor);

            ExpandType(parentNode, cursor, type, type, typeName);

            var offset = (int) (clang_Cursor_getOffsetOfField(cursor) / 8);

            return new CRecordField
            {
                Name = name,
                Location = codeLocation,
                Type = typeName,
                Offset = offset
            };
        }

        private ImmutableArray<CNode> CreateNestedNodes(string parentTypeName, CXCursor cursor, Node parentNode)
        {
            var builder = ImmutableArray.CreateBuilder<CNode>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);
            var type = clang_getCursorType(underlyingCursor);
            var typeCursor = clang_getTypeDeclaration(type);
            if (typeCursor.kind == CXCursorKind.CXCursor_UnionDecl ||
                typeCursor.kind == CXCursorKind.CXCursor_StructDecl)
            {
                underlyingCursor = typeCursor;
            }

            var nestedCursors = underlyingCursor.GetDescendents((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return false;
                }

                var type = clang_getCursorType(child);
                var typeDeclaration = clang_getTypeDeclaration(type);
                var isAnonymous = clang_Cursor_isAnonymous(typeDeclaration) > 0;
                if (isAnonymous)
                {
                    return true;
                }

                if (type.kind == CXTypeKind.CXType_Pointer)
                {
                    var pointeeType = clang_getPointeeType(type);
                    if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
                    {
                        return true;
                    }
                }

                return false;
            });

            foreach (var nestedCursor in nestedCursors)
            {
                var record = CreateNestedNode(parentTypeName, nestedCursor, parentNode);
                builder.Add(record);
            }

            if (builder.Count == 0)
            {
                return ImmutableArray<CNode>.Empty;
            }

            return builder.ToImmutable();
        }

        private CNode CreateNestedNode(string parentTypeName, CXCursor cursor, Node parentNode)
        {
            var type = clang_getCursorType(cursor);
            var isPointer = type.kind == CXTypeKind.CXType_Pointer;
            if (!isPointer)
            {
                return CreateNestedStruct(parentTypeName, cursor, type, parentNode);
            }

            var pointeeType = clang_getPointeeType(type);
            if (pointeeType.kind == CXTypeKind.CXType_FunctionProto)
            {
                var typeName = TypeName(parentTypeName, CKind.FunctionPointer, pointeeType, cursor);
                var location = Location(cursor, type);
                return CreateFunctionPointer(typeName, cursor, parentNode, type, pointeeType, location);
            }

            var up = new ClangExplorerException("Unknown mapping for nested node.");
            throw up;
        }

        private CNode CreateNestedStruct(string parentTypeName, CXCursor cursor, CXType type, Node parentNode)
        {
            var location = Location(cursor, type);
            var typeName = TypeName(parentTypeName, CKind.Record, type, cursor);

            var recordFields = CreateRecordFields(typeName, cursor, parentNode);
            var nestedNodes = CreateNestedNodes(typeName, cursor, parentNode);

            var nestedRecords = nestedNodes.Where(x => x is CRecord).Cast<CRecord>().ToImmutableArray();
            var nestedFunctionPointers = nestedNodes.Where(x => x is CFunctionPointer).Cast<CFunctionPointer>().ToImmutableArray();

            var typeCursor = clang_getTypeDeclaration(type);
            var isUnion = typeCursor.kind == CXCursorKind.CXCursor_UnionDecl;

            return new CRecord
            {
                Location = location,
                IsUnion = isUnion,
                Type = typeName,
                Fields = recordFields,
                NestedRecords = nestedRecords,
                NestedFunctionPointers = nestedFunctionPointers
            };
        }

        private ImmutableArray<CEnumValue> CreateEnumValues(CXCursor cursor)
        {
            var builder = ImmutableArray.CreateBuilder<CEnumValue>();

            var underlyingCursor = ClangUnderlyingCursor(cursor);

            var enumValuesCursors = underlyingCursor.GetDescendents((child, _) =>
            {
                if (child.kind != CXCursorKind.CXCursor_EnumConstantDecl)
                {
                    return false;
                }

                return true;
            });

            foreach (var enumValueCursor in enumValuesCursors)
            {
                var enumValue = CreateEnumValue(enumValueCursor);
                builder.Add(enumValue);
            }

            var result = builder.ToImmutable();
            return result;
        }

        private CEnumValue CreateEnumValue(CXCursor cursor, string? name = null)
        {
            var value = clang_getEnumConstantDeclValue(cursor);
            var location = Location(cursor);
            name ??= cursor.Name();

            return new CEnumValue
            {
                Location = location,
                Name = name,
                Value = value,
            };
        }

        private CType Type(string typeName, CXCursor cursor, CXType clangType)
        {
            var type = clangType;
            var declaration = clang_getTypeDeclaration(type);
            if (declaration.kind == CXCursorKind.CXCursor_NoDeclFound)
            {
                declaration = cursor;
            }

            var sizeOfValue = (int) clang_Type_getSizeOf(type);
            int? sizeOf = sizeOfValue >= 0 ? sizeOfValue : null;
            var alignOfValue = (int) clang_Type_getAlignOf(type);
            int? alignOf = alignOfValue >= 0 ? alignOfValue : null;
            var arraySizeValue = (int) clang_getArraySize(type);
            int? arraySize = arraySizeValue >= 0 ? arraySizeValue : null;
            var isSystemType = type.IsSystem();

            int? elementSize = null;
            if (type.kind == CXTypeKind.CXType_ConstantArray)
            {
                var elementType = clang_getElementType(type);
                elementSize = (int) clang_Type_getSizeOf(elementType);
            }

            ClangLocation? location = null;

            var typeKind = TypeKind(type);
            if (typeKind.Kind != CKind.Primitive && !isSystemType)
            {
                location = Location(declaration, type);
            }

            var cType = new CType
            {
                Name = typeName,
                Kind = typeKind.Kind,
                SizeOf = sizeOf,
                AlignOf = alignOf,
                ElementSize = elementSize,
                ArraySize = arraySize,
                IsSystem = isSystemType,
                Location = location
            };

            if (location != null)
            {
                var filePath = location.Value.Path;
                if (_ignoredFiles.Contains(filePath))
                {
                    var diagnostic = new DiagnosticTypeFromIgnoredFile(typeName, filePath);
                    _diagnostics.Add(diagnostic);
                }
            }

            return cType;
        }

        private static (CKind Kind, CXType Type) TypeKind(CXType type)
        {
            CXType cursorType;
            var cursor = clang_getTypeDeclaration(type);
            if (cursor.kind != CXCursorKind.CXCursor_NoDeclFound)
            {
                cursorType = clang_getCursorType(cursor);
            }
            else
            {
                cursorType = type;
            }

            if (cursorType.IsPrimitive())
            {
                return (CKind.Primitive, type);
            }

            switch (cursorType.kind)
            {
                case CXTypeKind.CXType_Enum:
                    return (CKind.Enum, cursorType);
                case CXTypeKind.CXType_Record:
                    var sizeOfRecord = clang_Type_getSizeOf(cursorType);
                    return sizeOfRecord == -2 ? (CKind.OpaqueType, cursorType) : (CKind.Record, cursorType);
                case CXTypeKind.CXType_Typedef:
                    var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                    var alias = TypeKind(underlyingType);
                    var sizeOfAlias = clang_Type_getSizeOf(alias.Type);
                    return sizeOfAlias == -2 ? (CKind.OpaqueType, cursorType) : (CKind.Typedef, cursorType);
                case CXTypeKind.CXType_FunctionProto:
                    return (CKind.FunctionPointer, cursorType);
                case CXTypeKind.CXType_Pointer:
                    var pointeeType = clang_getPointeeType(cursorType);
                    return pointeeType.kind == CXTypeKind.CXType_FunctionProto ? (CKind.FunctionPointer, pointeeType) : (CKind.Pointer, pointeeType);
                case CXTypeKind.CXType_Attributed:
                    var modifiedType = clang_Type_getModifiedType(cursorType);
                    return TypeKind(modifiedType);
                case CXTypeKind.CXType_Elaborated:
                    var namedType = clang_Type_getNamedType(cursorType);
                    return TypeKind(namedType);
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_IncompleteArray:
                    var elementType = clang_getElementType(cursorType);
                    return (CKind.Pointer, elementType);
            }

            var up = new ClangExplorerException($"Unknown type kind '{type.kind}.'");
            throw up;
        }

        private static CXCursor ClangUnderlyingCursor(CXCursor cursor)
        {
            var underlyingCursor = cursor;
            if (cursor.kind == CXCursorKind.CXCursor_TypedefDecl)
            {
                var underlyingType = clang_getTypedefDeclUnderlyingType(cursor);
                if (underlyingType.kind == CXTypeKind.CXType_Elaborated)
                {
                    var namedType = clang_Type_getNamedType(underlyingType);
                    underlyingCursor = clang_getTypeDeclaration(namedType);
                }
                else if (underlyingType.kind == CXTypeKind.CXType_Pointer)
                {
                    var pointeeType = clang_getPointeeType(underlyingType);
                    underlyingCursor = clang_getTypeDeclaration(pointeeType);
                }
                else
                {
                    underlyingCursor = clang_getTypeDeclaration(underlyingType);
                }

                if (!underlyingType.IsPrimitive())
                {
                    Debug.Assert(underlyingCursor.kind != CXCursorKind.CXCursor_NoDeclFound, "Expected declaration.");
                }
            }

            return underlyingCursor;
        }

        private void ExpandType(
            Node parentNode,
            CXCursor cursor,
            CXType type,
            CXType originalType,
            string typeName)
        {
            if (!RegisterTypeIsNew(typeName, type, cursor))
            {
                return;
            }

            var isValidTypeName = TypeNameIsValid(type, typeName);
            if (!isValidTypeName)
            {
                return;
            }

            var typeKind = TypeKind(type);
            if (typeKind.Kind == CKind.Pointer)
            {
                var pointeeKind = TypeKind(typeKind.Type);
                var pointeeCursor = clang_getTypeDeclaration(typeKind.Type);
                var pointeeTypeName = TypeName(parentNode.TypeName!, pointeeKind.Kind, pointeeKind.Type, pointeeCursor);
                ExpandType(parentNode, pointeeCursor, typeKind.Type, type, pointeeTypeName);
                return;
            }

            if (typeKind.Kind == CKind.Typedef)
            {
                ExpandTypedef(parentNode, typeKind.Type, typeName);
            }
            else
            {
                var locationCursor = clang_getTypeDeclaration(type);
                if (locationCursor.kind == CXCursorKind.CXCursor_NoDeclFound)
                {
                    locationCursor = cursor;
                }

                var location = Location(locationCursor);
                ExpandNode(typeKind.Kind, location, parentNode, cursor, cursor, typeKind.Type, originalType, string.Empty, typeName);
            }
        }

        private void ExpandTypedef(Node parentNode, CXType type, string typeName)
        {
            var typedefCursor = clang_getTypeDeclaration(type);
            var location = typedefCursor.FileLocation();
            ExpandNode(CKind.Typedef, location, parentNode, typedefCursor, typedefCursor, type, type, string.Empty, typeName);
        }

        private string TypeName(string parentTypeName, CKind kind, CXType type, CXCursor cursor)
        {
            var typeCursor = clang_getTypeDeclaration(type);
            var isAnonymous = clang_Cursor_isAnonymous(typeCursor) != 0;
            if (isAnonymous)
            {
                var anonymousCursor = clang_getTypeDeclaration(type);
                var cursorName = cursor.Name();

                switch (anonymousCursor.kind)
                {
                    case CXCursorKind.CXCursor_UnionDecl:
                        return $"{parentTypeName}_{cursorName}";
                    case CXCursorKind.CXCursor_StructDecl:
                        return $"{parentTypeName}_{cursorName}";
                    default:
                    {
                        // pretty sure this case is not possible, but it's better safe than sorry!
                        var up = new ClangExplorerException($"Unknown anonymous cursor kind '{anonymousCursor.kind}'");
                        throw up;
                    }
                }
            }

            var typeName = kind switch
            {
                CKind.Primitive => type.Name(),
                CKind.Pointer => type.Name(),
                CKind.Variable => type.Name(),
                CKind.Function => cursor.Name(),
                CKind.FunctionResult => type.Name(cursor),
                CKind.FunctionParameter => type.Name(cursor),
                CKind.FunctionPointer => type.Name(),
                CKind.FunctionPointerResult => type.Name(cursor),
                CKind.FunctionPointerParameter => type.Name(cursor),
                CKind.Typedef => type.Name(),
                CKind.Record => type.Name(),
                CKind.RecordField => type.Name(cursor),
                CKind.Enum => type.Name(),
                CKind.OpaqueType => type.Name(),
                _ => throw new ClangExplorerException($"Unexpected node kind '{kind}'.")
            };

            if (type.kind == CXTypeKind.CXType_IncompleteArray)
            {
                typeName = $"{typeName}";
            }
            else if (type.kind == CXTypeKind.CXType_ConstantArray)
            {
                var arraySize = clang_getArraySize(type);
                typeName = $"{typeName}[{arraySize}]";
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ClangExplorerException($"Type name was not found for '{kind}'.");
            }

            return kind == CKind.Pointer ? $"{typeName}*" : typeName;
        }

        private ClangLocation Location(CXCursor cursor, CXType? type = null)
        {
            ClangLocation location;
            if (type == null)
            {
                location = cursor.FileLocation();
            }
            else
            {
                location = type.Value.FileLocation(cursor);
                if (string.IsNullOrEmpty(location.Path))
                {
                    location = cursor.FileLocation();
                }
            }

            foreach (var includeDirectory in _includeDirectories)
            {
                if (location.Path.Contains(includeDirectory))
                {
                    location.Path = location.Path.Replace(includeDirectory, string.Empty).Trim('/', '\\');
                    break;
                }
            }

            return location;
        }

        public class Node
        {
            public readonly ClangLocation Location;
            public readonly CXCursor Cursor;
            public readonly CKind Kind;
            public readonly string? Name;
            public readonly string? TypeName;
            public readonly CXCursor OriginalCursor;
            public readonly CXType OriginalType;
            public readonly Node? Parent;
            public readonly CXType Type;

            public Node(
                CKind kind,
                ClangLocation location,
                Node? parent,
                CXCursor cursor,
                CXCursor originalCursor,
                CXType type,
                CXType originalType,
                string? name,
                string? typeName)
            {
                Kind = kind;

                if (string.IsNullOrEmpty(location.Path))
                {
                    if (type.IsPrimitive())
                    {
                        Location = new ClangLocation
                        {
                            Path = "System",
                            IsSystem = true
                        };
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    Location = location;
                }

                Parent = parent;
                Cursor = cursor;
                OriginalCursor = originalCursor;
                Type = type;
                OriginalType = originalType;
                Name = name;
                TypeName = typeName;
            }

            public override string ToString()
            {
                return Location.ToString();
            }
        }
    }
}
