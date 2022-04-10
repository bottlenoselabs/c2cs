#!/bin/bash
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

DOWNLOAD_DIR="/tmp/c2cs"
INSTALL_DIR="$DIR/../lib"

mkdir -p "$DOWNLOAD_DIR"
mkdir -p "$INSTALL_DIR"

cd $DOWNLOAD_DIR
curl -OL https://www.nuget.org/api/v2/package/libclang.runtime.ubuntu.18.04-x64
tar -xf "libclang.runtime.ubuntu.18.04-x64"
cd $DIR

cp "$DOWNLOAD_DIR/runtimes/ubuntu.18.04-x64/native/libclang.so" "$INSTALL_DIR/libclang.so"
rm -rf "$DOWNLOAD_DIR"