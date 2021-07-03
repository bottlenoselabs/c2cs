// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public enum CKind
    {
        Unknown = 0,
        TranslationUnit,
        Primitive,
        Pointer,
        Array,
        Function,
        FunctionResult,
        FunctionPointer,
        FunctionPointerResult,
        Record,
        RecordField,
        Enum,
        EnumValue,
        OpaqueType,
        Typedef,
        Variable,
    }
}
