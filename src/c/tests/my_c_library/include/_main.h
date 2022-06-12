#pragma once

#include <stdint.h>
#include "pinvoke_helper.h" // /src/c/production/c2cs/include/pinvoke_helper.h
#include "parent_header.h"

typedef enum enum_force_uint32 {
    ENUM_FORCE_UINT32_DAY_UNKNOWN,
    ENUM_FORCE_UINT32_DAY_MONDAY,
    ENUM_FORCE_UINT32_DAY_TUESDAY,
    ENUM_FORCE_UINT32_DAY_WEDNESDAY,
    ENUM_FORCE_UINT32_DAY_THURSDAY,
    ENUM_FORCE_UINT32_DAY_FRIDAY,
    _ENUM_FORCE_UINT32 = 0x7FFFFFFF
} enum_force_uint32;

typedef struct struct_leaf_integers_small_to_large // size: 16
{
    int8_t struct_field_1; // offset: 0
    // padding 1 byte
    int16_t struct_field_2; // offset: 2
    int32_t struct_field_3; // offset: 4
    int64_t struct_field_4; // offset: 8 
} struct_leaf_integers_small_to_large;

typedef struct struct_leaf_integers_large_to_small // size: 16
{
    int64_t struct_field_1; // offset: 0
    int32_t struct_field_2; // offset: 8
    int16_t struct_field_3; // offset: 12
    int8_t struct_field_4; // offset: 14
    // padding 1 byte
} struct_leaf_integers_large_to_small;

typedef struct struct_union_anonymous
{
    // the members of the union are accessible directly; e.g. struct_union_anonymous.union_field_1
    union
    {
        struct_leaf_integers_small_to_large union_field_1;
        struct_leaf_integers_large_to_small union_field_2;
    };
} struct_union_anonymous;

typedef struct struct_union_anonymous_with_field_name
{
    // the members of the union are accessible via the field; e.g. struct_union_anonymous.fields.union_field_1
    union
    {
        struct_leaf_integers_small_to_large union_field_1;
        struct_leaf_integers_large_to_small union_field_2;
    } fields; // the member name
} struct_union_anonymous_with_field_name;

typedef struct struct_union_named
{
    union struct_union_named_fields // the identifier of the union, must be unique like any other struct
    {
        struct_leaf_integers_small_to_large union_field_1;
        struct_leaf_integers_large_to_small union_field_2;
    } fields; // the member name
} struct_union_named;

PINVOKE_API_DECL void function_void_void(void);
PINVOKE_API_DECL void function_void_int(int);

PINVOKE_API_DECL void function_void_intptr(int*);
PINVOKE_API_DECL void function_void_intptr_1(int* );
PINVOKE_API_DECL void function_void_intptr_2(int *);
PINVOKE_API_DECL void function_void_intptr_3(int*ptr);
PINVOKE_API_DECL void function_void_intptr_4(int* ptr);
PINVOKE_API_DECL void function_void_intptr_5(int *ptr);
PINVOKE_API_DECL void function_void_intptr_6(int * ptr);
PINVOKE_API_DECL void function_void_intptr_7(const int*);
PINVOKE_API_DECL void function_void_intptr_8(const int* );
PINVOKE_API_DECL void function_void_intptr_9(const int *);
PINVOKE_API_DECL void function_void_intptr_10(const int*ptr);
PINVOKE_API_DECL void function_void_intptr_11(const int* ptr);
PINVOKE_API_DECL void function_void_intptr_12(const int *ptr);
PINVOKE_API_DECL void function_void_intptr_13(const int * ptr);
PINVOKE_API_DECL void function_void_intptr_14(int const*);
PINVOKE_API_DECL void function_void_intptr_15(int const* );
PINVOKE_API_DECL void function_void_intptr_16(int const *);
PINVOKE_API_DECL void function_void_intptr_17(int const*ptr);
PINVOKE_API_DECL void function_void_intptr_18(int const* ptr);
PINVOKE_API_DECL void function_void_intptr_19(int const *ptr);
PINVOKE_API_DECL void function_void_intptr_20(int const * ptr);
PINVOKE_API_DECL void function_void_intptr_21(int const* const);
PINVOKE_API_DECL void function_void_intptr_22(int const*const);
PINVOKE_API_DECL void function_void_intptr_23(int const *const);
PINVOKE_API_DECL void function_void_intptr_24(int const * const);
PINVOKE_API_DECL void function_void_intptr_25(int const* const ptr);
PINVOKE_API_DECL void function_void_intptr_26(int const*const ptr);
PINVOKE_API_DECL void function_void_intptr_27(int const *const ptr);
PINVOKE_API_DECL void function_void_intptr_28(int const * const ptr);

PINVOKE_API_DECL void function_void_string(const char* s);

PINVOKE_API_DECL void function_void_uint16_int32_uint64(uint16_t a, int32_t b, uint64_t c);

PINVOKE_API_DECL void function_void_uint16ptr_int32ptr_uint64ptr(const uint16_t* a, const int32_t* b, const uint64_t* c);

PINVOKE_API_DECL void function_void_enum(const enum_force_uint32 e);

PINVOKE_API_DECL void function_void_struct_union_anonymous(const struct_union_anonymous s);

PINVOKE_API_DECL void function_void_struct_union_anonymous_with_field_name(const struct_union_anonymous_with_field_name s);

PINVOKE_API_DECL void function_void_struct_union_named(const struct_union_named s);
