#pragma once

typedef enum EnumForceUInt8 {
    ENUM_FORCE_UINT8_DAY_UNKNOWN,
    ENUM_FORCE_UINT8_DAY_MONDAY,
    ENUM_FORCE_UINT8_DAY_TUESDAY,
    ENUM_FORCE_UINT8_DAY_WEDNESDAY,
    ENUM_FORCE_UINT8_DAY_THURSDAY,
    ENUM_FORCE_UINT8_DAY_FRIDAY,
    _ENUM_FORCE_UINT8 = 0xFF
} EnumForceUInt8;

FFI_API_DECL void EnumForceUInt8__print_EnumForceUInt8(const EnumForceUInt8 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceUInt8 EnumForceUInt8__return_EnumForceUInt8(const EnumForceUInt8 e)
{
    return e;
}