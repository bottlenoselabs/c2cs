// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public enum ClangNodeKind
    {
        Unknown = 0,
        FunctionExtern,
        FunctionExternResult,
        FunctionExternParameter,
        FunctionPointer,
        FunctionPointerResult,
        FunctionPointerParameter,
        Record,
        RecordField,
        Enum,
        EnumValue,
        OpaqueType,
        OpaquePointer,
        Typedef,
        VariableExtern,
    }
}
