# C2CS

C to C# library bindings code generator. In go `.h` file, out come `.cs` file.

## Documentation

For documentation on supported platforms, limitations, how to install `C2CS`, how to use `C2CS`, how to build `C2CS`, etc, see the [docs/README.md](docs/README.md).

## License

`C2CS` is licensed under the MIT License (`MIT`).

There are a few exceptions to this detailed below. See the [LICENSE](LICENSE) file for more details on this main
product's license.

`C2CS` uses `libclang` which the header files are included as part of the repository under [`ext/clang`](./ext/clang).
This is because `C2CS` generates bindings for `libclang` to which `C2CS` generates bindings for `libclang` and other C
libraries. The C header `.h` files (no source code `.c`/`.cpp` files) for `libclang` are included for convience of a
source-of-truth for re-generating the bindings. These files are licensed under the Apache License v2.0 with LLVM
Exceptions; see the [ext/clang/LICENSE.txt](./ext/clang/LICENSE.txt) for more details.

