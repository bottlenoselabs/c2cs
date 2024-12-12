// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;

namespace C2CS.GenerateCSharpCode;

public sealed class CodeGeneratorDocumentPInvokeContext(InputSanitized input)
{
    private readonly HashSet<string> _existingNames = [];

    public bool IsEnabledFunctionPointers { get; } = input.IsEnabledFunctionPointers;

    public bool IsEnabledLibraryImportAttribute { get; } = input.IsEnabledLibraryImportAttribute;

    public bool NameAlreadyExists(string name)
    {
        var alreadyExists = !_existingNames.Add(name);
        return alreadyExists;
    }
}
