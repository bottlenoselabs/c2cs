// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.IO;

namespace C2CS
{
    public class BindgenWriteCSharpCode
    {
        public void WriteCSharpToDisk(string filePath, string cSharpCode)
        {
            File.WriteAllText(filePath, cSharpCode);
        }
    }
}
