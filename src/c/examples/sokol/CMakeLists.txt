cmake_minimum_required(VERSION 3.17)

macro(ADD_FRAMEWORK fwname target)
    find_library(FRAMEWORK_${fwname}
    NAMES ${fwname}
    PATHS ${CMAKE_OSX_SYSROOT}/System/Library
    PATH_SUFFIXES Frameworks
    NO_DEFAULT_PATH)
    if( ${FRAMEWORK_${fwname}} STREQUAL FRAMEWORK_${fwname}-NOTFOUND)
        MESSAGE(ERROR ": Framework ${fwname} not found")
    else()
        TARGET_LINK_LIBRARIES(${target} "${FRAMEWORK_${fwname}}/${fwname}")
        MESSAGE(STATUS "Framework ${fwname} found at ${FRAMEWORK_${fwname}}")
    endif()
endmacro(ADD_FRAMEWORK)

project(sokol)

set(CMAKE_C_STANDARD 11)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)

get_filename_component(INCLUDES_DIRRECTORY_PATH "../../../../ext/sokol" REALPATH BASE_DIR "${CMAKE_CURRENT_SOURCE_DIR}")
include_directories(${INCLUDES_DIRRECTORY_PATH})

add_definitions(-DSOKOL_NO_DEPRECATED)
if (APPLE)
    add_definitions(-DSOKOL_METAL)
endif()

if (APPLE)
    set(SOURCES "sokol.m")
else()
    set(SOURCES "sokol.c")
endif()

add_library(sokol SHARED ${SOURCES})

if (APPLE)
    add_framework(Cocoa sokol)
    add_framework(QuartzCore sokol)
    add_framework(Metal sokol)
    add_framework(MetalKit sokol)
endif()