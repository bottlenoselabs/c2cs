// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace C2CS
{
    [StructLayout(LayoutKind.Sequential)]
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    public struct FnPtrPointerPointer
    {
        public unsafe delegate* unmanaged<void*, void*> Pointer;
    }
}
