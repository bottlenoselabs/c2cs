#!/bin/bash
DIRECTORY="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

printf "\n"

function get_operating_system() {
    local UNAME_STRING="$(uname -a)"
    case "${UNAME_STRING}" in
        *Microsoft*)    local TARGET_OS="windows";;
        *microsoft*)    local TARGET_OS="windows";;
        Linux*)         local TARGET_OS="linux";;
        Darwin*)        local TARGET_OS="macos";;
        CYGWIN*)        local TARGET_OS="linux";;
        MINGW*)         local TARGET_OS="windows";;
        *Msys)          local TARGET_OS="windows";;
        *)              local TARGET_OS="UNKNOWN:${UNAME_STRING}"
    esac
    echo "$TARGET_OS"
    return 0
}

printf "\nBUILD..\n\n"
rm -rf ./cmake-build-release
cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release
cmake --build cmake-build-release --config Release
rm -rf ./cmake-build-release
printf "\nBUILD FINISHED...\n"

printf "\nSHOW FUNCTIONS...\n\n"

OS="$(get_operating_system)"
if [[ "$OS" == "macos" ]]; then
    nm -g ./bin/*.dylib
fi

printf "\nSHOW FUNCTIONS FINISHED...\n"