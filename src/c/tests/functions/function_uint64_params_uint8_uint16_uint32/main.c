#include <stdio.h>
#include <stdint.h>
#include "ffi_helper.h"

FFI_API_DECL uint64_t function_uint64_params_uint8_uint16_uint32(uint8_t a, uint16_t b, uint32_t c)
{
    return a + b + c;
}
