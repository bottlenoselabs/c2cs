#include <stdio.h>
#include "ffi_helper.h"

enum enum_implicit {
    ENUM_IMPLICIT_VALUE0 = 0,
    ENUM_IMPLICIT_VALUE1 = 255
} enum_implicit;

FFI_API_DECL int function_implicit_enum(int value)
{
    return ENUM_IMPLICIT_VALUE1;
}
