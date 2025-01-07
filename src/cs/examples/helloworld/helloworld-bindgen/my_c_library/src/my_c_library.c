#include "my_c_library.h"

#include <stdio.h>

void hw_hello_world(void)
{
    printf("Hello world from C!\n");
}

void hw_invoke_callback1(hw_callback f, const char* s)
{
    if (f == 0)
    {
        return;
    }

    f(s);
}

void hw_invoke_callback2(void f(const char*), const char* s)
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

void hw_pass_integers_by_value(uint16_t a, uint32_t b, uint64_t c)
{
    uint64_t sum = a + b + c;
    printf("Sum: %llu\n", sum);
}

void hw_pass_integers_by_reference(const uint16_t* a, const uint32_t* b, const uint64_t* c)
{
    uint64_t sum = *a + *b + *c;
    printf("Sum: %llu\n", sum);
}

void _hw_print_weekday(const hw_week_day e)
{
    switch (e)
    {
        case HW_WEEK_DAY_MONDAY:
            printf("Monday :((\n");
            break;
        case HW_WEEK_DAY_TUESDAY:
            printf("Tuesday :(\n");
            break;
        case HW_WEEK_DAY_WEDNESDAY:
            printf("Wednesday :|\n");
            break;
        case HW_WEEK_DAY_THURSDAY:
            printf("Thursday :)\n");
            break;
        case HW_WEEK_DAY_FRIDAY:
            printf("Friday :))\n");
            break;
        case HW_WEEK_DAY_UNKNOWN:
        default:
            printf("Unknown week day!\n");
            break;
    }
}

void hw_pass_enum_by_value(const hw_week_day e)
{
    _hw_print_weekday(e);
}

void hw_pass_enum_by_reference(const hw_week_day* e)
{
    _hw_print_weekday(*e);
}

void _hw_print_event(const hw_event e)
{
    printf("Event kind: %d, event data:\n", e.kind);
    switch (e.kind)
    {
        case HW_EVENT_KIND_STRING:
            printf("\t%s\n", e.string1);
            printf("\t%s\n", e.string2);
            break;
        case HW_EVENT_KIND_U8:
        case HW_EVENT_KIND_S8:
        case HW_EVENT_KIND_U16:
        case HW_EVENT_KIND_S16:
        case HW_EVENT_KIND_U32:
        case HW_EVENT_KIND_S32:
        case HW_EVENT_KIND_U64:
        case HW_EVENT_KIND_S64:
        case HW_EVENT_KIND_U128:
        case HW_EVENT_KIND_S128:
        case HW_EVENT_KIND_U256:
        case HW_EVENT_KIND_S256:
            printf("\tNot implemented.\n");
            break;
        case HW_EVENT_KIND_BOOL:
            printf("\t%s\n", e.boolean ? "true" : "false");
            break;
        case HW_EVENT_KIND_UNKNOWN:
        default:
            printf("\tUnknown event!\n");
            break;
    }
}

void hw_pass_struct_by_value(const hw_event e)
{
    _hw_print_event(e);
}

void hw_pass_struct_by_reference(const hw_event* e)
{
    _hw_print_event(*e);
}
