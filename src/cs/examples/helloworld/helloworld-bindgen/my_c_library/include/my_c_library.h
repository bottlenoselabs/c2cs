#pragma once

#include <stdint.h>
#include <stdbool.h>
#include <stddef.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
    #if defined(__clang__)
        #define MY_C_LIBRARY_API_DECL __declspec(dllexport) __attribute__((visibility("default")))
    #else
        #define MY_C_LIBRARY_API_DECL __declspec(dllexport)
    #endif
#else
    #define MY_C_LIBRARY_API_DECL extern __attribute__((visibility("default")))
#endif

#define HW_STRING_POINTER "Hello world using UTF-8 string literal from the C library's data segment!"

typedef void (*hw_callback)(const char *s);

typedef enum hw_week_day
{
    HW_WEEK_DAY_UNKNOWN,

    HW_WEEK_DAY_MONDAY,
    HW_WEEK_DAY_TUESDAY,
    HW_WEEK_DAY_WEDNESDAY,
    HW_WEEK_DAY_THURSDAY,
    HW_WEEK_DAY_FRIDAY,

    _HW_WEEK_DAY_FORCE_U32 = 0x7FFFFFFF
} hw_week_day;

typedef enum hw_event_kind
{
    HW_EVENT_KIND_UNKNOWN,

    HW_EVENT_KIND_STRING,
    HW_EVENT_KIND_S8,
    HW_EVENT_KIND_U8,
    HW_EVENT_KIND_S16,
    HW_EVENT_KIND_U16,
    HW_EVENT_KIND_S32,
    HW_EVENT_KIND_U32,
    HW_EVENT_KIND_S64,
    HW_EVENT_KIND_U64,
    HW_EVENT_KIND_S128,
    HW_EVENT_KIND_U128,
    HW_EVENT_KIND_S256,
    HW_EVENT_KIND_U256,
    HW_EVENT_KIND_BOOL,

    _HW_EVENT_KIND_FORCE_U32 = 0x7FFFFFFF
} hw_event_kind;

typedef struct hw_event
{
    hw_event_kind kind;
    struct
    {
        union
        {
            struct
            {
                char* string1;
                char* string2;
            };
            struct
            {
                int8_t s8;
            };
            struct
            {
                uint8_t u8;
            };
            struct
            {
                int16_t s16;
            };
            struct
            {
                uint16_t u16;
            };
            struct
            {
                int32_t s32;
            };
            struct
            {
                uint32_t u32;
            };
            struct
            {
                int64_t s64;
            };
            struct
            {
                uint64_t u64;
            };
            struct
            {
                int8_t s128[16];
            };
            struct
            {
                uint8_t u128[16];
            };
            struct
            {
                int64_t s256[4];
            };
            struct
            {
                uint64_t u256[4];
            };
            struct
            {
                size_t size;
            };
            struct
            {
                bool boolean;
            };
        };
    };
} hw_event;

MY_C_LIBRARY_API_DECL void hw_hello_world(void);
MY_C_LIBRARY_API_DECL void hw_invoke_callback1(hw_callback f, const char *s);
MY_C_LIBRARY_API_DECL void hw_invoke_callback2(void f(const char *), const char *s);
MY_C_LIBRARY_API_DECL void hw_pass_string(const char *s);
MY_C_LIBRARY_API_DECL void hw_pass_integers_by_value(uint16_t a, uint32_t b, uint64_t c);
MY_C_LIBRARY_API_DECL void hw_pass_integers_by_reference(const uint16_t *a, const uint32_t* b, const uint64_t* c);
MY_C_LIBRARY_API_DECL void hw_pass_enum_by_value(const hw_week_day e);
MY_C_LIBRARY_API_DECL void hw_pass_enum_by_reference(const hw_week_day* e);
MY_C_LIBRARY_API_DECL void hw_pass_struct_by_value(const hw_event e);
MY_C_LIBRARY_API_DECL void hw_pass_struct_by_reference(const hw_event* e);
