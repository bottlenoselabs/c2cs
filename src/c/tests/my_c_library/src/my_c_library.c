#include "my_c_library.h"

#include <stdint.h>
#include <stdio.h>

void function_void_void(void)
{
    printf("function_void_void\n");
}

void function_void_string(const char* s)
{
    printf("function_void_string: %s\n", s);
}

void function_void_uint16_int32_uint64(uint16_t a, int32_t b, uint64_t c)
{
    uint64_t sum = a + b + c;
    printf("function_void_uint16_int32_uint64: %lu\n", sum);
}

void function_void_uint16ptr_int32ptr_uint64ptr(const uint16_t* a, const int32_t* b, const uint64_t* c)
{
    uint64_t sum = *a + *b + *c;
    printf("function_void_uint16ptr_int32ptr_uint64ptr: %lu\n", sum);
}

void function_void_enum(const enum_force_uint32 e)
{
    printf("function_void_enum: ");

    switch (e)
    {
        case ENUM_FORCE_UINT32_DAY_UNKNOWN:
            printf("UNKNOWN");
            break;
        case ENUM_FORCE_UINT32_DAY_MONDAY:
            printf("MONDAY");
            break;
        default:
            printf("???");
    }

    printf("\n");
}

void function_void_struct_union(const struct_union s)
{
    uint64_t x = s.union1.a + s.union1.b + s.union1.c + s.union1.d;
    uint64_t y = s.union2.a + s.union2.b + s.union2.c + s.union2.d;
    uint64_t sum = x + y + s.a + s.b + s.c + s.d;
}
