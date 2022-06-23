# C2CS

C to C# library bindings code generator. In go `.h` file, out come `.cs` file.

## Documentation

For documentation on supported platforms, limitations, how to install `C2CS`, how to use `C2CS`, how to build `C2CS`, etc, see the [docs/README.md](docs/README.md).

## C# bindings of C libraries using C2CS


| C library  | C# bindings |
|:----------:|:-----------:|
|[flecs](https://github.com/SanderMertens/flecs)|https://github.com/flecs-hub/flecs-cs|
|[sokol](https://github.com/floooh/sokol)|https://github.com/bottlenoselabs/sokol-cs|
|[SDL](https://github.com/libsdl-org/SDL)|https://github.com/bottlenoselabs/SDL-cs|
|[FAudio](https://github.com/FNA-XNA/FAudio)|https://github.com/bottlenoselabs/FAudio-cs|

## Background: Why?

### Problem

When creating applications with C# (especially games), it's sometimes necessary to dip down into C/C++ for better raw performance and overall better portability of various different low-level APIs accross various platforms. (This is what FNA does today and what [MonoGame will be doing in the future](https://github.com/MonoGame/MonoGame/issues/7523#issuecomment-865808668).) However, the problem is that maintaining the C# bindings becomes time consuming, error-prone, and in some cases quite tricky.

### Solution

Automate the generation the C# bindings by compiling/parsing a C `.h` file. The C API (application programmer interface; those functions you want to call) is tranpiled to C# for the target platform (a Clang target triple `arch-vendor-os-environment`) for use via P/Invoke (platform invoke).

All C extern functions which are transpiled to `static` methods in C# using `DllImport` attribute. Includes the C types which are found through transitive property to the extern functions such as: `struct`s, `enum`s, and `const`s. C# `struct`s are generated instead of `class`es on purpose to achieve 1-1 bit-representation of C to C# types called *blittable* types. For more details why blittable types matter see [docs/LESSONS-LEARNED.md#marshalling](./docs/LESSONS-LEARNED.md#marshalling).

This is all accomplished by using [libclang](https://clang.llvm.org/docs/Tooling.html) for parsing C and [Roslyn](https://github.com/dotnet/roslyn) for generating C#. All naming is left as found in the C header `.h` file(s).

<p align="center">
  <img width="460" src="./docs/c2cs.png">
</p>

For more details on why `C2CS` is structured into `ast` and `cs` see [docs/SUPPORTED-PLATFORMS.md#restrictive-or-closed-source-system-headers](./docs/SUPPORTED-PLATFORMS.md#restrictive-or-closed-source-system-headers).

### Limitations

1. .NET 5+ because C# 9 function pointers are used. `C2CS` is made using .NET 6 but can create bindings for at least .NET 5.

2. This solution does not work for every C library. This is due to some technical limitations where some C libraries are not "bindgen-friendly".

#### What does it mean for a C library to be bindgen-friendly?

Everything in the [**external linkage**](https://stackoverflow.com/questions/1358400/what-is-external-linkage-and-internal-linkage) of the C API is subject to the following list. Note that the internal guts of the C library is irrelevant and to which this list does not apply. In this sense it is then possible to use C++ internally but then only expose a C interface for interoperability with C#. This list may be updated as more things are discovered/standardized.

|Supported|Description|
|:-:|-|
|:white_check_mark:|Function externs.|
|:x:|Variable externs.<sup>1</sup>|
|:white_check_mark:|Function prototypes. (a.k.a., function pointers.)|
|:white_check_mark:|Enums<sup>2</sup>.|
|:white_check_mark:|Structs.<sup>3</sup>|
|:white_check_mark:|Unions.<sup>4</sup>|
|:white_check_mark:|Opaque types.<sup>5</sup>|
|:white_check_mark:|Typedefs. (a.k.a, type aliases.)|
|:o:|Function-like macros.<sup>6</sup>|
|:white_check_mark:|Object-like macros.<sup>7</sup>|
|:x:|C++.|
|:x:|Objective-C.|
|:o:|Implicit types.<sup>8</sup>|
|:x:|`va_list`<sup>9</sup>|
|:white_check_mark:|`wchar_t`<sup>10</sup>|

<sup>1</sup>: `dlsym` on Unix and `GetProcAddress` on Windows allow getting the address of a variable exported for shared libraries (`.dll`/`.dylib`/`.so`). However, there is no way to do the same for statically linked libraries. There is also no alternative for `DllImport` in C# for extern variables. The recommended way to expose variable externs to C# from C is to instead create "getter" and/or "setter" function externs. Thus, variable externs are not supported for simplicity.

<sup>2</sup>: Enums are forced to be signed type in C#. This is allow for better convergence accross platforms such as Windows, macOS, and Linux because enums can be signed or unsigned depending on the toolchain/platform.

<sup>3</sup>: For structs (and unions within structs), distinguishing between public/private fields is not possible automatically. If the record is transtive to a function extern then it will be transpiled as if all the fields were public. In some cases this may not be appropriate to which there is the following options. Either, (1) use proper information hiding with C headers so the private fields are not in transtive property to a public function extern, or (2) use pointers to access the struct and manually specify the struct as an opaque type for input to `C2CS`. Option 2 is the approach taken for generating bindings for https://github.com/libuv/libuv because `libuv` makes use of mixing public/private struct fields and struct inheritance.

<sup>4</sup>: C# allows for unions using explicit layout of struct fields. Anonymous unions are transpiled to a struct which is nested inside the parent struct.

<sup>5</sup>: For opaque types, if the C header file has direct knowledge of the actual implementation, then they will be by default transpiled as if they were not opaque types. To overcome this, the opaque types in question will need to be manually specified for input to `C2CS`. This a common scenario for single file header libraries such as https://github.com/nothings/stb.

<sup>6</sup>: Function-like macros are only possible if the parameters' types can be inferred 100% of the time during preprocessor; otherwise, not possible. **Not yet implemented**.

<sup>7</sup>: Object-like macros have limited support. They are transpiled to constants in C# which the value type is determined by evaluating the value of the macro as an C# expression.

<sup>8</sup>: Types must be explicitly transtive to a function extern so they can be found. The only exception is enums that are part of external linkage to which such enums are transpiled. This is a commonly found in some C libraries such as https://github.com/libsdl-org/SDL where functions take integers as part of their API but are actually expecting an enum.

<sup>9</sup>: For support with `va_list` see https://github.com/lithiumtoast/c2cs/issues/15.

<sup>10</sup>: `wchar_t*` is mapped to `CStringWide` which is an opaque pointer, that is fine. What is not fine is that `wchar_t` itself is a problem for cross-platform because by default it is 2 bytes on Windows and 4 bytes on Linux (it also be 1 byte on some embedded systems or otherwise different on various hardware). It can be forced to be the same across hardware using the `-fshort-wchar` compiler flag, but this has consequences. Some hardware vendors enforce that all linked objects must use the same `wchar_t` size, including libraries. It is then not possible or at very least unstable to link an object file compiled with `-fshort-wchar`, with another object file that is compiled without `-fshort-wchar` such as standard libraries. The approach taken for `C2CS` is to use either 1, 2 or 4 for the C# mapped type `CCharWide` that is blittable to `wchar_t`. The exact size of bytes depend on the target operating system by default but can be overriden by specifying the property `SIZEOF_WCHAR_T` to a value of `1`, `2`, or `4` in your C# project. For more information on how this works, please see [How to use `C2CS.Runtime` - Custom C# project properties for `C2CS.Runtime`](docs/README.md#custom-c-project-properties-for-c2csruntime). Note however that if any of your public header structs use `wchar_t` then the resulting struct may be different sizes across platforms. In such a case it is no different than pointers limitation mentioned earlier in terms of a solution.

#### What do I do if I want to generate bindings for a non bindgen-friendly C library?

Options:

1. Change the library so that the **external linkage** becomes bindgen-friendly. E.g. removing C++, removing macros, etc.
2. Use either ClangSharp bindgen tool ()https://github.com/InfectedLibraries/Biohazrd as a framework to generate bindings; requires more setup and is not as straightforward, but it works for C++.

### Other similar projects

Mentioned here for completeness. I do believe you should be aware of other approaches to this problem and see if they make more sense to you.

- https://github.com/dotnet/runtimelab/tree/feature/DllImportGenerator
- https://github.com/microsoft/ClangSharp
- https://github.com/SharpGenTools/SharpGenTools
- https://github.com/xoofx/CppAst.NET
- https://github.com/rds1983/Sichem

## License

`C2CS` is licensed under the MIT License (`MIT`).

There are a few exceptions to this detailed below. See the [LICENSE](LICENSE) file for more details on this main product's license.

`C2CS` uses `libclang` which the header files are included as part of the repository under [`ext/clang`](./ext/clang). This is because `C2CS` generates bindings for `libclang` to which `C2CS` generates bindings for `libclang` and other C libraries. The C header `.h` files (no source code `.c`/`.cpp` files) for `libclang` are included for convience of a source-of-truth for re-generating the bindings. These files are licensed under the Apache License v2.0 with LLVM Exceptions; see the [ext/clang/LICENSE.txt](./ext/clang/LICENSE.txt) for more details.

