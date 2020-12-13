// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;

[assembly: AssemblyCompany("Craftworkgames")]
[assembly: AssemblyProductAttribute("C2CS")]
[assembly: AssemblyTitleAttribute("C2CS")]

[assembly: AssemblyVersionAttribute("0.1.0.0")]
[assembly: AssemblyFileVersionAttribute("0.1.0.0")]
[assembly: AssemblyInformationalVersionAttribute("0.1.0")]

#if DEBUG
[assembly: AssemblyConfigurationAttribute("Debug")]
#elif RELEASE
[assembly: AssemblyConfigurationAttribute("Release")]
#endif

