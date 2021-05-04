cmake_minimum_required(VERSION 3.16)

project(sokol)

set(CMAKE_C_STANDARD 11)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)

get_filename_component(INCLUDES_DIRRECTORY_PATH "../../../../ext/sokol" REALPATH BASE_DIR "${CMAKE_CURRENT_SOURCE_DIR}")
include_directories(${INCLUDES_DIRRECTORY_PATH})

add_definitions(-DSOKOL_NO_DEPRECATED)
if (WIN32)
    add_definitions(-DSOKOL_D3D11)
elseif (APPLE)
    add_definitions(-DSOKOL_METAL)
endif()

if (APPLE)
    set(SOURCES "sokol.m")
else()
    set(SOURCES "sokol.c")
endif()

add_library(sokol SHARED ${SOURCES})

if (WIN32)
    target_link_libraries(sokol -ld3d11)
elseif (APPLE)
    target_link_libraries(sokol "-framework Cocoa")
    target_link_libraries(sokol "-framework QuartzCore")
    target_link_libraries(sokol "-framework Metal")
    target_link_libraries(sokol "-framework MetalKit")
    set_target_properties(sokol PROPERTIES LINK_FLAGS "-Wl,-F/Library/Frameworks")
endif()