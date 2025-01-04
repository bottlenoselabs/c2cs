#pragma once

#include <stdint.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
    #if defined(__clang__)
        #define MY_C_LIBRARY_API_DECL __declspec(dllexport) __attribute__ ((visibility("default")))
    #else
        #define MY_C_LIBRARY_API_DECL __declspec(dllexport)
    #endif
#else
    #define MY_C_LIBRARY_API_DECL extern __attribute__ ((visibility("default")))
#endif

#define HW_STRING_POINTER "Hello world using UTF-8 string literal from the C library's data segment!"

typedef enum hw_my_enum_week_day {
    HW_MY_ENUM_WEEK_DAY_UNKNOWN,
    HW_MY_ENUM_WEEK_DAY_MONDAY,
    HW_MY_ENUM_WEEK_DAY_TUESDAY,
    HW_MY_ENUM_WEEK_DAY_WEDNESDAY,
    HW_MY_ENUM_WEEK_DAY_THURSDAY,
    HW_MY_ENUM_WEEK_DAY_FRIDAY,
    _HW_MY_ENUM_WEEK_DAY_FORCE_U32 = 0x7FFFFFFF
} hw_my_enum_week_day;

typedef void (*hw_callback)(const char* s);

MY_C_LIBRARY_API_DECL void hw_hello_world(void);
MY_C_LIBRARY_API_DECL void hw_invoke_callback1(hw_callback f, const char* s);
MY_C_LIBRARY_API_DECL void hw_invoke_callback2(void f(const char*), const char* s);
MY_C_LIBRARY_API_DECL void hw_pass_string(const char* s);
MY_C_LIBRARY_API_DECL void hw_pass_integers_by_value(uint16_t a, int32_t b, uint64_t c);
MY_C_LIBRARY_API_DECL void hw_pass_integers_by_reference(const uint16_t* a, const int32_t* b, const uint64_t* c);
MY_C_LIBRARY_API_DECL void hw_pass_enum(const hw_my_enum_week_day e);
