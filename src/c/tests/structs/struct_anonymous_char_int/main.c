#include <stdio.h>
#include "ffi_helper.h"

struct struct_anonymous_char_int
{
    struct {
        char a;
        int b;
    };
};

FFI_API_DECL struct struct_anonymous_char_int struct_anonymous_char_int;
