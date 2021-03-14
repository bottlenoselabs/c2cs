// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;

namespace C2CS.Bindgen.ExploreCCode
{
    public readonly struct GenericCodeLocation
    {
        public readonly string FileName;
        public readonly int FileLine;
        public readonly int FileColumn;
        public readonly DateTime DateTime;

        public GenericCodeLocation(
            string fileName,
            int fileLine,
            int fileColumn,
            DateTime dateTime)
        {
            FileName = fileName;
            FileLine = fileLine;
            FileColumn = fileColumn;
            DateTime = dateTime;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}
