#include "my_c_library.h"

#include <stdint.h>
#include <stdio.h>

void hw_hello_world(void)
{
    printf("Hello world from C!\n");
}

void hw_invoke_callback(void f(const char*), const char* s)
{
    if (f == 0)
    {
        return;
    }

    f(s);
}

void hw_pass_string(const char* s)
{
    printf("%s\n", s);
}

void hw_pass_integers_by_value(uint16_t a, int32_t b, uint64_t c)
{
    uint64_t sum = a + b + c;
    printf("%lu\n", sum);
}

void hw_pass_integers_by_reference(const uint16_t* a, const int32_t* b, const uint64_t* c)
{
    uint64_t sum = *a + *b + *c;
    printf("%lu\n", sum);
}

void hw_pass_enum(const hw_my_enum_week_day e)
{
    switch (e)
    {
        case HW_MY_ENUM_WEEK_DAY_UNKNOWN:
            printf("UNKNOWN");
            break;
        case HW_MY_ENUM_WEEK_DAY_MONDAY:
            printf("MONDAY");
            break;
        default:
            printf("???");
    }
}
