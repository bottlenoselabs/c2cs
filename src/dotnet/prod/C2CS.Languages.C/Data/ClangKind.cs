// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

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
        RecordNested,
        RecordField,
        Enum,
        EnumValue,
        OpaqueDataType,
        ForwardDataType,
        SystemDataType,
    }
}
