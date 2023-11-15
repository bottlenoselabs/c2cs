#pragma once

#define MACRO_OBJECT_INT_VALUE 42;

FFI_API_DECL int MacroObjectInt__return_int()
{
    return MACRO_OBJECT_INT_VALUE;
}