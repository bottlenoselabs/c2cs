# Limitations

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
2. Use https://github.com/InfectedLibraries/Biohazrd as a framework to generate bindings; requires more setup and is not as straightforward, but it works for C++.

### Other similar projects

Mentioned here for completeness. I do believe you should be aware of other approaches to this problem and see if they make more sense to you.

- https://github.com/dotnet/runtimelab/tree/feature/DllImportGenerator
- https://github.com/microsoft/ClangSharp
- https://github.com/SharpGenTools/SharpGenTools
- https://github.com/xoofx/CppAst.NET
- https://github.com/rds1983/Sichem