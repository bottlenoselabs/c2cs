#include <stdio.h>
#include "ffi_helper.h"

typedef void (*function_pointer_void)();

FFI_API_DECL void function(function_pointer_void fnptr)
{
    (*fnptr)();
}
