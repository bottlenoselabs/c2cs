#pragma once

#include "pinvoke_helper.h"

// WARNING: Difference between Clang and MSCV for layout of bitfields.
//  Ensure that bitfields are tightly packed by using the smallest data type for the bits. E.g. for 1-8 bits use `uint8_t` not `uint16_t` or greater.

typedef struct struct_bitfield_one_fields_1
{
    int8_t bitfield : 8;
} struct_bitfield_one_fields_1;

typedef struct struct_bitfield_one_fields_2
{
    int32_t a;
    int8_t bitfield : 8;
} struct_bitfield_one_fields_2;

typedef struct struct_bitfield_one_fields_3
{
    int32_t a; // Offset: 0
    int8_t bitfield : 8; // Offset: 4
    int8_t b; // Offset: 5
    // Padding 2 bytes
} struct_bitfield_one_fields_3; // Size: 8

// Required to that the typedefs are included in bindgen
PINVOKE_API_DECL void function_struct_bitfield_one_fields_1(struct_bitfield_one_fields_1);
PINVOKE_API_DECL void function_struct_bitfield_one_fields_2(struct_bitfield_one_fields_2);
PINVOKE_API_DECL void function_struct_bitfield_one_fields_3(struct_bitfield_one_fields_3);

