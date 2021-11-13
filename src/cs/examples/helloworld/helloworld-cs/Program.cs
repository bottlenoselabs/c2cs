// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static my_c_library_namespace.my_c_library;

internal static class Program
{
    private static unsafe void Main()
    {
        hello_world();
        pass_string("Hello world from C#!");
        pass_integers_by_value(65449, -255, 24242);

        ushort a = 65449;
        var b = -255;
        ulong c = 24242;
        pass_integers_by_reference(&a, &b, &c);
    }
}
