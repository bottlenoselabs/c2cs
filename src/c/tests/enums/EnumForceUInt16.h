#pragma once

typedef enum EnumForceUInt16 {
    ENUM_FORCE_UINT16_DAY_UNKNOWN,
    ENUM_FORCE_UINT16_DAY_MONDAY,
    ENUM_FORCE_UINT16_DAY_TUESDAY,
    ENUM_FORCE_UINT16_DAY_WEDNESDAY,
    ENUM_FORCE_UINT16_DAY_THURSDAY,
    ENUM_FORCE_UINT16_DAY_FRIDAY,
    _ENUM_FORCE_UINT16 = 0xFFFF
} EnumForceUInt16;

PINVOKE_API_DECL void EnumForceUInt16__print_EnumForceUInt16(const EnumForceUInt16 e)
{
    printf("%d\n", e); // Print used for testing
}

PINVOKE_API_DECL EnumForceUInt16 EnumForceUInt16__return_EnumForceUInt16(const EnumForceUInt16 e)
{
    return e;
}