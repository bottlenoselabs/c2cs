// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;

namespace C2CS
{
    /// <summary>
    ///     Defines the native computer architectures.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public enum RuntimeArchitecture
    {
        /// <summary>
        ///     Unknown computer architecture.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Intel x86_x64 64-bit computing architecture. Commonly found in modern desktop platforms such as Windows
        ///     10 with power users such as the general gamer audience for PC. Also commonly found in some eighth
        ///     generation consoles such as Xbox One and PlayStation 4.
        /// </summary>
        X64 = 1,

        /// <summary>
        ///     Intel x86 32-bit computing architecture. Commonly found in legacy desktop platforms.
        /// </summary>
        X86 = 2,

        /// <summary>
        ///     ARM (Advanced RISC (Reduced Instruction Set Computer) Machines) 64-bit computing architecture. Commonly
        ///     found in modern mobile or some modern console platforms such as iOS, Nintendo Switch, etc. Also observed
        ///     in some modern laptops such as the M1 from Apple.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
        ARM64,

        /// <summary>
        ///     ARM (Advanced RISC (Reduced Instruction Set Computer) Machines) 32-bit computing architecture. Commonly
        ///     found in legacy mobile or legacy console platforms.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
        ARM32 = 4
    }
}
