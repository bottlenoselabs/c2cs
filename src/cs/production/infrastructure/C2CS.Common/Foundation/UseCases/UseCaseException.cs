// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace C2CS;

public sealed class UseCaseException : Exception
{
    public UseCaseException()
    {
    }

    public UseCaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UseCaseException(string message)
        : base(CreateMessage(message))
    {
    }

    private static string CreateMessage(string message)
    {
        var featureName = FeatureName();
        if (string.IsNullOrEmpty(message))
        {
            return featureName;
        }

        return featureName + Environment.NewLine + message;
    }

    private static string FeatureName()
    {
        var skipFrames = 0;
        var featureNamespace = typeof(UseCaseException).Namespace! + ".Feature";

        while (true)
        {
            var stackFrame = new StackFrame(skipFrames, false);
            var method = stackFrame.GetMethod();
            if (method == null)
            {
                return string.Empty;
            }

            var declaringType = method.DeclaringType;
            var typeNamespace = declaringType?.Namespace!;
            if (string.IsNullOrEmpty(typeNamespace))
            {
                skipFrames++;
                continue;
            }

            if (!typeNamespace.StartsWith(featureNamespace, StringComparison.InvariantCulture))
            {
                skipFrames++;
                continue;
            }

            return typeNamespace;
        }
    }
}
