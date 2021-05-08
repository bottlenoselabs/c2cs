// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public enum ClangKind
    {
        Unknown = 0,
        FunctionExtern,
        FunctionExternParameter,
        FunctionPointer,
        FunctionPointerParameter,
        Record,
        RecordField,
        Enum,
        EnumValue,
        OpaqueDataType,
        OpaquePointer,
        Typedef,
        Variable
    }
}
