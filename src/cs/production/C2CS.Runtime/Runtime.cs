// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("ReSharper", "CheckNamespace", Justification = "Wants to be builtin.")]

[assembly: SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "C style.")]
[assembly: SuppressMessage("StyleCop.Naming", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "C style.")]
[assembly: SuppressMessage("StyleCop.Naming", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "C style.")]

namespace C2CS
{
    /// <summary>
    ///     The collection of utilities for interoperability with native libraries in C#. Used by code which is generated
    ///     using the C2CS tool: https://github.com/lithiumtoast/c2cs.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public static partial class Runtime
    {
    }
}
