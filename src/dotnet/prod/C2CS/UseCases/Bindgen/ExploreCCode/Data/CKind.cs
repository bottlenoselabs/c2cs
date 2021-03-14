// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

namespace C2CS.Bindgen.ExploreCCode
{
    public enum CKind
    {
        Unknown = 0,
        FunctionExtern,
        Struct,
        Enum,
        FunctionPointer,
        OpaqueType
    }
}
