// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS;

namespace macOS.MessageBox;

[Bindgen(
    HeaderInputFile = "header.h",
    LibraryName = "/System/Library/Frameworks/CoreFoundation.framework/Versions/A/CoreFoundation",
    AddAsSource = false,
    IsEnabledSystemDeclarations = true)]
[BindgenTargetPlatform(Name = "aarch64-apple-darwin", Frameworks = new[] { "CoreFoundation" })]
[BindgenTargetPlatform(Name = "x86_64-apple-darwin", Frameworks = new[] { "CoreFoundation" })]
[BindgenFunction(Name = "CFUserNotificationDisplayAlert")]
[BindgenFunction(Name = "CFStringCreateWithCString")]
[BindgenFunction(Name = "CFRelease")]
public partial class CoreFoundation
{
}
