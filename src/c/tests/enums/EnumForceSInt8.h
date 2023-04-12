#pragma once

typedef enum EnumForceSInt8 {
    ENUM_FORCE_SINT8_DAY_UNKNOWN,
    ENUM_FORCE_SINT8_DAY_MONDAY,
    ENUM_FORCE_SINT8_DAY_TUESDAY,
    ENUM_FORCE_SINT8_DAY_WEDNESDAY,
    ENUM_FORCE_SINT8_DAY_THURSDAY,
    ENUM_FORCE_SINT8_DAY_FRIDAY,
    _ENUM_FORCE_SINT8 = 0x7F
} EnumForceSInt8;

FFI_API_DECL void EnumForceSInt8__print_EnumForceSInt8(const EnumForceSInt8 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceSInt8 EnumForceSInt8__return_EnumForceSInt8(const EnumForceSInt8 e)
{
    return e;
}