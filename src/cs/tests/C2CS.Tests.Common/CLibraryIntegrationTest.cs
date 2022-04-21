// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Common;

public abstract class CLibraryIntegrationTest
{
	private readonly string _libraryName;

	protected CLibraryIntegrationTest(string libraryName)
	{
		_libraryName = libraryName;
	}

	protected string ReadTestFileContents(string name)
	{
		return File.ReadAllText($"{_libraryName}/Data/{name}.json");
	}
}
