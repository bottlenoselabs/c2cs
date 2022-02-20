// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;

namespace C2CS;

public class UseCaseException : Exception
{
    public UseCaseException()
        : this(CreateMessage(string.Empty))
    {
    }

    public UseCaseException(string message)
        : base(CreateMessage(message))
    {
    }

    public UseCaseException(string message, Exception innerException)
        : base(CreateMessage(message), innerException)
    {
    }

    private static string CreateMessage(string message)
    {
        var name = UseCaseNamespaceName();
        return string.IsNullOrEmpty(message) ? name : $"{name}: {message}";
    }

    private static string UseCaseNamespaceName(int skipFrames = 3)
    {
        while (true)
        {
            var method = new StackFrame(skipFrames, false).GetMethod()!;
            var declaringType = method.DeclaringType;
            var typeNamespace = declaringType?.Namespace!;
            if (string.IsNullOrEmpty(typeNamespace))
            {
                skipFrames++;
                continue;
            }

            var currentNamespaceName = typeof(UseCaseException).Namespace!;
            if (!typeNamespace.StartsWith(currentNamespaceName, StringComparison.InvariantCulture))
            {
                skipFrames++;
                continue;
            }

            var candidateNamespaceName = typeNamespace[currentNamespaceName.Length..].Trim('.');
            const string useCasesNamespaceName = "UseCases.";
            if (candidateNamespaceName.StartsWith(useCasesNamespaceName, StringComparison.InvariantCulture))
            {
                candidateNamespaceName = candidateNamespaceName[useCasesNamespaceName.Length..];
            }

            return candidateNamespaceName;
        }
    }
}
