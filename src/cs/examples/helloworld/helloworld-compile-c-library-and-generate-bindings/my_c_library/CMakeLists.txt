# Minimum required version of CMake to be installed in order to build this project.
cmake_minimum_required(VERSION 3.16)

# The project name followed by the language.
project(my_c_library C)

# The C standard whose features are requested to build this target. Supported values are "90" for 1990, "99" for 1990 and "11" for 2011.
# If you don't care use the latest, 11.
set(CMAKE_C_STANDARD 11)

# Change the output directories for artifacts.
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)

# Add directories so header files can be found
include_directories(my_c_library PRIVATE include)

# 1. Specify that the project is a dynamic link library (SHARED).
# 2. Specify what source code files are to be included.
add_library(my_c_library SHARED src/my_c_library.c include/my_c_library.h)
