#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

DOWNLOAD_DIR="%Temp%/c2cs"
INSTALL_DIR="$DIR/../lib"

mkdir -p "$DOWNLOAD_DIR"
mkdir -p "$INSTALL_DIR"

cd $DOWNLOAD_DIR
curl -OL https://github.com/llvm/llvm-project/releases/download/llvmorg-13.0.0-rc2/LLVM-13.0.0-rc2-win64.exe
7z x "./LLVM-13.0.0-rc2-win64.exe" -o"./LLVM"
cd $DIR

cp "$DOWNLOAD_DIR/LLVM/bin/libclang.dll" "$INSTALL_DIR/lib/libclang.dll"
rm -rf "$DOWNLOAD_DIR"