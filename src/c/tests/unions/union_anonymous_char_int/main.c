#include <stdio.h>
#include "ffi_helper.h"

union union_anonymous_char_int
{
    union {
        char a;
        int b;
    };
};

FFI_API_DECL union union_anonymous_char_int union_anonymous_char_int;
