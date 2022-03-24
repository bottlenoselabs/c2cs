#pragma once

#include <stdint.h>
#include "c2cs_helper.h"

typedef enum enum_force_uint32 {
    ENUM_FORCE_UINT32_DAY_UNKNOWN,
    ENUM_FORCE_UINT32_DAY_MONDAY,
    ENUM_FORCE_UINT32_DAY_TUESDAY,
    ENUM_FORCE_UINT32_DAY_WEDNESDAY,
    ENUM_FORCE_UINT32_DAY_THURSDAY,
    ENUM_FORCE_UINT32_DAY_FRIDAY,
    _ENUM_FORCE_UINT32 = 0x7FFFFFFF
} enum_force_uint32;

C2CS_API_DECL void function_void_void(void);
C2CS_API_DECL void function_void_string(const char* s);
C2CS_API_DECL void function_void_uint16_int32_uint64(uint16_t a, int32_t b, uint64_t c);
C2CS_API_DECL void function_void_uint16ptr_int32ptr_uint64ptr(const uint16_t* a, const int32_t* b, const uint64_t* c);
C2CS_API_DECL void function_void_enum(const enum_force_uint32 e);
