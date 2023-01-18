#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

INSTALL_DIR="$DIR/../../lib"

mkdir -p "$INSTALL_DIR"

cp "/Library/Developer/CommandLineTools/usr/lib/libclang.dylib" "$INSTALL_DIR/libclang.dylib"