// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.BindgenCSharp
{
	// NOTE: Properties are required for System.Text.Json serialization
    [PublicAPI]
    public class Configuration
    {
		[JsonPropertyName("className")]
		public string ClassName { get; set; } = string.Empty;

		[JsonPropertyName("libraryName")]
		public string LibraryName { get; set; } = string.Empty;
    }
}
