cmake_minimum_required(VERSION 3.16)

project(_container_library C)
set(CMAKE_C_STANDARD 11)

set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_SOURCE_DIR}/bin)

include_directories(_container_library PRIVATE ../../production/pinvoke_helper/include)

add_library(_container_library SHARED ${SOURCE_FILES} main.c)
set_target_properties(_container_library PROPERTIES LINKER_LANGUAGE C)