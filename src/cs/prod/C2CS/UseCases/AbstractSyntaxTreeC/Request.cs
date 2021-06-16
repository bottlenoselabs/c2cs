// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class Request : UseCaseRequest
    {
        public FileInfo InputFile { get; }

        public FileInfo OutputFile { get; }

        public FileInfo ConfigurationFile { get; }

        public Request(FileInfo inputFile, FileInfo outputFile, FileInfo configurationFile)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            ConfigurationFile = configurationFile;
        }
    }
}
