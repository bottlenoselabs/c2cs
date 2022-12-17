// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics;

namespace C2CS.Foundation.Executors;

public sealed class ExecutorException : Exception
{
    public ExecutorException()
    {
    }

    public ExecutorException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ExecutorException(string message)
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
        var featureNamespace = typeof(ExecutorException).Namespace! + ".Feature";

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
