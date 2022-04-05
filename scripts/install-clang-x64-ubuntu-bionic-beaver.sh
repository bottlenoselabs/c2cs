#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

DOWNLOAD_DIR="/tmp/c2cs"
INSTALL_DIR="$DIR/../lib"

mkdir -p "$DOWNLOAD_DIR"
mkdir -p "$INSTALL_DIR"

cd $DOWNLOAD_DIR
curl -OL https://github.com/llvm/llvm-project/releases/download/llvmorg-14.0.0/clang+llvm-14.0.0-x86_64-linux-gnu-ubuntu-18.04.tar.xz
tar -xf "clang+llvm-14.0.0-x86_64-linux-gnu-ubuntu-18.04.tar.xz"
cd $DIR

cp "$DOWNLOAD_DIR/clang+llvm-14.0.0-x86_64-linux-gnu-ubuntu-18.04/lib/libclang.so.14.0.0" "$INSTALL_DIR/libclang.so"
rm -rf "$DOWNLOAD_DIR"