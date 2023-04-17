#pragma once

typedef enum EnumForceSInt32 {
    ENUM_FORCE_SINT32_DAY_UNKNOWN,
    ENUM_FORCE_SINT32_DAY_MONDAY,
    ENUM_FORCE_SINT32_DAY_TUESDAY,
    ENUM_FORCE_SINT32_DAY_WEDNESDAY,
    ENUM_FORCE_SINT32_DAY_THURSDAY,
    ENUM_FORCE_SINT32_DAY_FRIDAY,
    _ENUM_FORCE_SINT32 = 0x7FFFFF
} EnumForceSInt32;

FFI_API_DECL void EnumForceSInt32__print_EnumForceSInt32(const EnumForceSInt32 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceSInt32 EnumForceSInt32__return_EnumForceSInt32(const EnumForceSInt32 e)
{
    return e;
}