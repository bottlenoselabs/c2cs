#include <stdio.h>
#include "ffi_helper.h"

enum enum_uint8 {
    ENUM_UINT8_MIN = 0,
    ENUM_UINT8_MAX = 255
} enum_uint8;

FFI_API_DECL enum enum_uint8 enum_uint8;
