#include <stdio.h>
#include "ffi_helper.h"

union union_anonymous_nested
{
    union {
        union {
            char a;
            int b;
        };
        union {
            char c;
            int d;
        };
    };
};

FFI_API_DECL union union_anonymous_nested union_anonymous_nested;
