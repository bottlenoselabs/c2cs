#include "my_c_library.h"

#include <stdint.h>
#include <stdio.h>

void hello_world(void)
{
    printf("Hello, World!\n");
}

void pass_string(const char* s)
{
    printf("%s\n", s);
}

void pass_integers_by_value(uint16_t a, int32_t b, uint64_t c)
{
    uint64_t sum = a + b + c;
    printf("%lu\n", sum);
}

void pass_integers_by_reference(const uint16_t* a, const int32_t* b, const uint64_t* c)
{
    uint64_t sum = *a + *b + *c;
    printf("%lu\n", sum);
}
