#include <stdio.h>
#include "ffi_helper.h"

struct struct_anonymous_nested
{
    struct {
        struct {
            char a;
            int b;
        };
        struct {
            char c;
            int d;
        };
    };
};

FFI_API_DECL struct struct_anonymous_nested struct_anonymous_nested;
