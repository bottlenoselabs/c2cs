#pragma once

typedef enum EnumForceSInt64 {
    ENUM_FORCE_SINT64_DAY_UNKNOWN,
    ENUM_FORCE_SINT64_DAY_MONDAY,
    ENUM_FORCE_SINT64_DAY_TUESDAY,
    ENUM_FORCE_SINT64_DAY_WEDNESDAY,
    ENUM_FORCE_SINT64_DAY_THURSDAY,
    ENUM_FORCE_SINT64_DAY_FRIDAY,
    _ENUM_FORCE_SINT64 = 0x7FFFFFFFFFFFFFFFUL
} EnumForceSInt64;

FFI_API_DECL void EnumForceSInt64__print_EnumForceSInt64(const EnumForceSInt64 e)
{
    printf("%d\n", e); // Print used for testing
}

FFI_API_DECL EnumForceSInt64 EnumForceSInt64__return_EnumForceSInt64(const EnumForceSInt64 e)
{
    return e;
}