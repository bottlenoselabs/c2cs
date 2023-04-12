// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Foundation.Tool;

public abstract class ToolInputSanitizer<TUnsanitizedInput, TInput>
    where TUnsanitizedInput : ToolUnsanitizedInput
{
    public abstract TInput Sanitize(TUnsanitizedInput unsanitizedInput);
}
