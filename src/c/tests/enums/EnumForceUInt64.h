#pragma once

typedef enum EnumForceUInt64 {
    ENUM_FORCE_UINT64_DAY_UNKNOWN,
    ENUM_FORCE_UINT64_DAY_MONDAY,
    ENUM_FORCE_UINT64_DAY_TUESDAY,
    ENUM_FORCE_UINT64_DAY_WEDNESDAY,
    ENUM_FORCE_UINT64_DAY_THURSDAY,
    ENUM_FORCE_UINT64_DAY_FRIDAY,
    _ENUM_FORCE_UINT64 = 0xffffffff
} EnumForceUInt64;

FFI_API_DECL void EnumForceUInt64__print_EnumForceUInt64(const EnumForceUInt64 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceUInt64 EnumForceUInt64__return_EnumForceUInt64(const EnumForceUInt64 e)
{
    return e;
}