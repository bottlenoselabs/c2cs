// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

// using static macOS.MessageBox.CoreFoundation;

namespace macOS.MessageBox;

public static class Program
{
    private static unsafe void Main()
    {
        // CFTimeInterval timeout = default;
        // CFOptionFlags flags = 1;
        // CFURLRef iconUrl = default;
        // CFURLRef secondUrl = default;
        // CFURLRef localizationUrl = default;
        // var stringTitle = CreateCoreFoundationString("Hello world!");
        // CFStringRef stringMessage = default;
        // var stringDefaultButtonText = CreateCoreFoundationString("OK");
        // CFStringRef alternateButtonTitle = default;
        // CFStringRef otherButtonTitle = default;
        // CFOptionFlags result;
        // var isCancelled = CFUserNotificationDisplayAlert(
        //     timeout,
        //     flags,
        //     iconUrl,
        //     secondUrl,
        //     localizationUrl,
        //     stringTitle,
        //     stringMessage,
        //     stringDefaultButtonText,
        //     alternateButtonTitle,
        //     otherButtonTitle,
        //     &result) == 0;
        //
        // // Clean up
        // CFRelease(stringTitle.Data);
        // CFRelease(stringDefaultButtonText.Data);
        //
        // if (isCancelled)
        // {
        //     Console.WriteLine("Message box default button.");
        // }
        // else
        // {
        //     Console.WriteLine("Message box other button: " + result.Data);
        // }
    }

    // private static CFStringRef CreateCoreFoundationString(string value)
    // {
    //     CFStringEncoding encoding;
    //     encoding.Data = (uint)value.Length;
    //     var result = CFStringCreateWithCString(default, value, encoding);
    //     return result;
    // }
}
