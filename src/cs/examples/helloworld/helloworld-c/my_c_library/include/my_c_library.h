#pragma once

#include <stdint.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(__NT__)
    #define MY_C_LIBRARY_API_DECL __declspec(dllexport)
#else
    #define MY_C_LIBRARY_API_DECL extern
#endif

typedef enum my_enum_week_day {
    MY_ENUM_TYPE_UNKNOWN,
    MY_ENUM_TYPE_MONDAY,
    MY_ENUM_TYPE_TUESDAY,
    MY_ENUM_TYPE_WEDNESDAY,
    MY_ENUM_TYPE_THURSDAY,
    MY_ENUM_TYPE_FRIDAY,
    _MY_ENUM_TYPE_FORCE_U32 = 0x7FFFFFFF
} my_enum_week_day;

MY_C_LIBRARY_API_DECL void hello_world(void);
MY_C_LIBRARY_API_DECL void pass_string(const char* s);
MY_C_LIBRARY_API_DECL void pass_integers_by_value(uint16_t a, int32_t b, uint64_t c);
MY_C_LIBRARY_API_DECL void pass_integers_by_reference(const uint16_t* a, const int32_t* b, const uint64_t* c);
MY_C_LIBRARY_API_DECL void pass_enum(const my_enum_week_day e);
