// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS;

namespace macOS.MessageBox;

[Bindgen(HeaderInputFile = "header.h", AddAsSource = false)]
[BindgenFunction(Name = "CFUserNotificationDisplayAlert")]
public partial class CoreFoundation
{
}
