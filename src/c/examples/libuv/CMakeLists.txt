cmake_minimum_required(VERSION 3.16)

# The project name followed by the language. It is recommended that the folder for this file have the same name as the project.
project(libuv C)

# The C standard whose features are requested to build this target. Supported values are 90, 99 and 11.
# If you don't care use the latest, 11.
set(CMAKE_C_STANDARD 11)

# Change the output directories for artifacts.
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/lib)

set(LIBUV_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}/../../../../ext/libuv)

add_definitions(-DBUILD_TESTING=0)
add_subdirectory(${LIBUV_DIRECTORY} ${LIBUV_DIRECTORY}/build)