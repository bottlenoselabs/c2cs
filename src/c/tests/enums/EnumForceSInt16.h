#pragma once

typedef enum EnumForceSInt16 {
    ENUM_FORCE_SINT16_DAY_UNKNOWN,
    ENUM_FORCE_SINT16_DAY_MONDAY,
    ENUM_FORCE_SINT16_DAY_TUESDAY,
    ENUM_FORCE_SINT16_DAY_WEDNESDAY,
    ENUM_FORCE_SINT16_DAY_THURSDAY,
    ENUM_FORCE_SINT16_DAY_FRIDAY,
    _ENUM_FORCE_SINT16 = 0x7FFF
} EnumForceSInt16;

FFI_API_DECL void EnumForceSInt16__print_EnumForceSInt16(const EnumForceSInt16 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceSInt16 EnumForceSInt16__return_EnumForceSInt16(const EnumForceSInt16 e)
{
    return e;
}