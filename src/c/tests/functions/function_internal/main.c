#include <stdio.h>
#include "ffi_helper.h"

#define UNKNOWN_CUSTOM_API_DECL
#define BAD_CUSTOM_API_DECL
#define GOOD_CUSTOM_API_DECL FFI_API_DECL

void function_internal_1()
{
}

FFI_API_DECL void function_internal_2()
{
}

UNKNOWN_CUSTOM_API_DECL void function_internal_3()
{
}

BAD_CUSTOM_API_DECL void function_internal_4()
{
}

GOOD_CUSTOM_API_DECL void function_internal_5()
{
}
