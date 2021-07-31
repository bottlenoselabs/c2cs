// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using C2CS;

[SuppressMessage("ReSharper", "SA1300", Justification = "C style.")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
public static unsafe partial class uv
{
    public static void CheckErrorCode(string functionName, int errorCode)
    {
        var status = errorCode >= 0 ? "success" : "failure";

        if (errorCode == 0)
        {
            Console.WriteLine($"{functionName}: {status}");
        }
        else
        {
            var name = GetErrorCodeName(errorCode);
            var description = GetErrorCodeDescription(errorCode);
            Console.WriteLine($"{functionName}: {status} {name} {description}");
        }
    }

    public static string GetErrorCodeName(int errorCode)
    {
        var cString = uv_err_name(errorCode);
        var result = Runtime.String8U(cString);
        return result;
    }

    public static string GetErrorCodeDescription(int errorCode)
    {
        var buffer = stackalloc byte[512];
        var cString = uv_strerror_r(errorCode, buffer, 512);
        var result = Runtime.String8U(cString);
        return result;
    }
}
