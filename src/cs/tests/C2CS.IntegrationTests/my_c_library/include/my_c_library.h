#pragma once

#include <stdint.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(__NT__)
    #define MY_C_LIBRARY_API_DECL __declspec(dllexport)
#else
    #define MY_C_LIBRARY_API_DECL extern
#endif

typedef enum enum_force_uint32 {
    ENUM_FORCE_UINT32_DAY_UNKNOWN,
    ENUM_FORCE_UINT32_DAY_MONDAY,
    ENUM_FORCE_UINT32_DAY_TUESDAY,
    ENUM_FORCE_UINT32_DAY_WEDNESDAY,
    ENUM_FORCE_UINT32_DAY_THURSDAY,
    ENUM_FORCE_UINT32_DAY_FRIDAY,
    _ENUM_FORCE_UINT32 = 0x7FFFFFFF
} enum_force_uint32;

MY_C_LIBRARY_API_DECL void function_void_void(void);
MY_C_LIBRARY_API_DECL void function_void_string(const char* s);
MY_C_LIBRARY_API_DECL void function_void_uint16_int32_uint64(uint16_t a, int32_t b, uint64_t c);
MY_C_LIBRARY_API_DECL void function_void_uint16ptr_int32ptr_uint64ptr(const uint16_t* a, const int32_t* b, const uint64_t* c);
MY_C_LIBRARY_API_DECL void function_void_enum(const enum_force_uint32 e);
