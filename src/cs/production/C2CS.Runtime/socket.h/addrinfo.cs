// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace C2CS
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public unsafe struct addrinfo
    {
        public int ai_flags;
        public int ai_family;
        public int ai_socktype;
        public int ai_protocol;
        public socklen_t ai_addrlen;
        public byte* ai_canonname;
        public sockaddr* ai_addr;
        public addrinfo* ai_next;
    }
}
