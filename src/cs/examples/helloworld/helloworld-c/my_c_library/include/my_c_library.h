#pragma once

#include <stdint.h>

#if defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(__NT__)
    #define MY_C_LIBRARY_API_DECL __declspec(dllexport)
#else
    #define MY_C_LIBRARY_API_DECL extern
#endif

MY_C_LIBRARY_API_DECL void hello_world(void);
MY_C_LIBRARY_API_DECL void pass_string(const char* s);
MY_C_LIBRARY_API_DECL void pass_integers_by_value(uint16_t a, int32_t b, uint64_t c);
MY_C_LIBRARY_API_DECL void pass_integers_by_reference(uint16_t* a, int32_t* b, uint64_t* c);
