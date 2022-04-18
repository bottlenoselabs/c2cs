#pragma once

#include <stdint.h>
#include "pinvoke_helper.h" // /src/cs/production/C2CS/include/pinvoke_helper.h

typedef enum enum_force_uint32 {
    ENUM_FORCE_UINT32_DAY_UNKNOWN,
    ENUM_FORCE_UINT32_DAY_MONDAY,
    ENUM_FORCE_UINT32_DAY_TUESDAY,
    ENUM_FORCE_UINT32_DAY_WEDNESDAY,
    ENUM_FORCE_UINT32_DAY_THURSDAY,
    ENUM_FORCE_UINT32_DAY_FRIDAY,
    _ENUM_FORCE_UINT32 = 0x7FFFFFFF
} enum_force_uint32;

typedef struct struct_leaf_integers_forward // size: 16
{
    int8_t _8; // offset: 0
    // padding 1 byte
    int16_t _16; // offset: 2
    int32_t _32; // offset: 4
    int64_t _64; // offset: 8 
} struct_leaf_integers_forward;

typedef struct struct_leaf_integers_reverse // size: 16
{
    int64_t _64; // offset: 0
    int32_t _32; // offset: 8
    int16_t _16; // offset: 12
    int8_t _8; // offset: 14
    // padding 1 byte
} struct_leaf_integers_reverse;

typedef struct struct_union
{
    union
    {
        struct_leaf_integers_forward field1;
        struct_leaf_integers_reverse field2;
    };
} struct_union;

PINVOKE_API_DECL void function_void_void(void);
PINVOKE_API_DECL void function_void_string(const char* s);
PINVOKE_API_DECL void function_void_uint16_int32_uint64(uint16_t a, int32_t b, uint64_t c);
PINVOKE_API_DECL void function_void_uint16ptr_int32ptr_uint64ptr(const uint16_t* a, const int32_t* b, const uint64_t* c);
PINVOKE_API_DECL void function_void_enum(const enum_force_uint32 e);
PINVOKE_API_DECL void function_void_struct_union(const struct_union s);
