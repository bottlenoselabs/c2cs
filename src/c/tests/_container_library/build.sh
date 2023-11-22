#!/bin/bash
DIRECTORY="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
OUPUT_DIRECTORY=$DIRECTORY/bin

rm -rf $DIRECTORY/cmake-build-release
cmake -S $DIRECTORY -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY=$OUPUT_DIRECTORY -DCMAKE_RUNTIME_OUTPUT_DIRECTORY=$OUPUT_DIRECTORY -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE=$OUPUT_DIRECTORY
cmake --build cmake-build-release --config Release
rm -rf $DIRECTORY/cmake-build-release