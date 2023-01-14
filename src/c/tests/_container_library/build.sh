#!/bin/bash
DIRECTORY="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

rm -rf ./cmake-build-release
cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY=/Users/lstranks/Programming/bottlenoselabs/c2cs/bin/C2CS.Tests/Debug/net7.0/c/tests/_container_library/bin -DCMAKE_LIBRARY_OUTPUT_DIRECTORY=/Users/lstranks/Programming/bottlenoselabs/c2cs/bin/C2CS.Tests/Debug/net7.0/c/tests/_container_library/bin -DCMAKE_RUNTIME_OUTPUT_DIRECTORY=/Users/lstranks/Programming/bottlenoselabs/c2cs/bin/C2CS.Tests/Debug/net7.0/c/tests/_container_library/bin -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE=/Users/lstranks/Programming/bottlenoselabs/c2cs/bin/C2CS.Tests/Debug/net7.0/c/tests/_container_library/bin
cmake --build cmake-build-release --config Release
rm -rf ./cmake-build-release