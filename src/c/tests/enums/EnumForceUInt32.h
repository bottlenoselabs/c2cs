#pragma once

typedef enum EnumForceUInt32 {
    ENUM_FORCE_UINT32_DAY_UNKNOWN,
    ENUM_FORCE_UINT32_DAY_MONDAY,
    ENUM_FORCE_UINT32_DAY_TUESDAY,
    ENUM_FORCE_UINT32_DAY_WEDNESDAY,
    ENUM_FORCE_UINT32_DAY_THURSDAY,
    ENUM_FORCE_UINT32_DAY_FRIDAY,
    _ENUM_FORCE_UINT32 = 0xffffffffffffffffL
} EnumForceUInt32;

FFI_API_DECL void EnumForceUInt32__print_EnumForceUInt32(const EnumForceUInt32 e)
{
    printf("%lu\n", e); // Print used for testing
}

FFI_API_DECL EnumForceUInt32 EnumForceUInt32__return_EnumForceUInt32(const EnumForceUInt32 e)
{
    return e;
}