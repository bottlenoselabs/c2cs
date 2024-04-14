#include <stdio.h>
#include "ffi_helper.h"

typedef enum enum_week_day {
    ENUM_WEEK_DAY_UNKNOWN = -1,
    ENUM_WEEK_DAY_MONDAY = 1,
    ENUM_WEEK_DAY_TUESDAY = 2,
    ENUM_WEEK_DAY_WEDNESDAY = 3,
    ENUM_WEEK_DAY_THURSDAY = 4,
    ENUM_WEEK_DAY_FRIDAY = 5,
    _ENUM_WEEK_DAY_MAX = 6
} enum_week_day;

FFI_API_DECL enum_week_day enum_week_day;
