# C2CS

C to C# library bindings code generator. In go `.h` file, out come `.cs` file.

## Documentation

For documentation on how to install, use, or build `C2CS` see the [docs/README.md](docs/README.md).

## Background: Why?

### Problem

When creating applications with C# (especially games), it's sometimes necessary to dip down into C/C++ for better raw performance and overall better portability of various different low-level APIs accross various platforms. (This is what FNA does today and what [MonoGame will be doing in the future](https://github.com/MonoGame/MonoGame/issues/7523#issuecomment-865808668).) However, the problem is that maintaining the C# bindings becomes time consuming, error-prone, and in some cases quite tricky.

If you are not familiar already with interoperability of C/C++ with C#, it's assumed that you have read and understood the following relatively short readings:
- [P/Invoke: Introduction](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke).
- [Marshalling: Introduction](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/type-marshaling).
- [Marshalling: Default behaviour for value types in .NET](https://docs.microsoft.com/en-us/dotnet/framework/interop/default-marshaling-behavior#default-marshaling-for-value-types).

### Solution

Automatically generate the bindings by compiling/parsing a C `.h` file. The C API (application programmer interface; those functions you want to call) is tranpiled to C# for the target ABI (application binary interface; a Clang target triple `arch-vendor-os-environment`, e.g. `x86_64-pc-windows-msvc`) for use via P/Invoke (platform invoke).

Includes all C extern functions which are transpiled to `static` methods respecitively in C# using `DllImport` attribute. The C types which are found through transitive property to the extern functions such as: `struct`s, `enum`s, and `const`s are also transpiled to C#. C# `struct`s are generated instead of `class`es on purpose to achieve 1-1 bit-representation of C to C# types called *blittable* types. The reason for blittable types is to achieve pass-through marshalling and active avoidance of the Garbage Collector in C# for best possible runtime performance and portability when doing interoperability with C.

This is all accomplished by using [libclang](https://clang.llvm.org/docs/Tooling.html) for parsing C and [Roslyn](https://github.com/dotnet/roslyn) for generating C#. All naming is left as found in the C header `.h` file(s).

<p align="center">
  <img width="460" src="./docs/c2cs.png">
</p>

### Limitations

1. .NET 5+ because C# 9 function pointers are used. `C2CS` is made using .NET 6 but can create bindings for at least .NET 5.

2. Pointers such as `void*` can have different sizes across target computer architectures. E.g., `x86` pointers are 4 bytes and `x64` (aswell as `arm64`) pointers are 8 bytes. Thus, if you need to have bindings for `x86`/`arm32` and `x64`/`arm64` you will need to have two seperate bindings. However, 64 bit is pretty ubiquitous on Windows these days, at least for gaming, as you can see from [Steam hardware survey where 64-bit is 99%+](https://store.steampowered.com/hwsurvey/directx/). Additionally, you can see that the ["trend" is that 64-bit is becoming standard over time with 32-bit getting dropped](https://en.wikipedia.org/wiki/64-bit_computing#64-bit_operating_system_timeline).

3. The "built-in" C integer types could have different bit width for different ABIs. E.g. `long` is at **minimum** 32 bits (4 bytes). On Windows `x64` it is reported by the Microsoft Visual C++ (MSCV) to be actually 64 bits (8 bytes). For sanity sake you should always use the integer types from `stdint.h` such as `uint8_t`, `int32_t`, `uint64_t`, etc. Otherwise you may need to have multiple bindings for each ABI because types will be of different bit sizes.

4. This solution does not work for every C library. This is due to some technical limitations where some C libraries are not "bindgen-friendly".

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

### Technical details

#### Dynamic loading & dynamic linking

At runtime, if the C library is "dynamic" (meaning the `.dll`/`.dylib`/`.so` file is external to the executable), it gets loaded into virtual memory by operating system at runtime at first usage of an external function (this is called dynamic linking) or by user code via `dlopen` on Unix or `LoadLibrary` on Windows (this is called dynamic loading).

In the case of **dynamic loading**, said function has to additionally be resolved by *symbol* from user code using `dlsym` (Unix) or `GetProcAddress` (Windows) to get the pointer to the function in virtual memory (function pointer). Then to call said function requires one level of *indirection* stemming from the use of the pointer. This is suboptimal compared to dynamic linking but is the current affairs of how the `DllImport` works in just-in-time compilation environments of .NET for platform invoke (P/Invoke).

With the advancement of ahead-of-time compilation in Mono and .NET 5 ([NativeAOT](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT)), **dynamic linking** can be achieved instead of **dynamic loading** resulting in *direct* platform invoke (P/Invoke) which has better performance by lack of the *indirection* use of the extra pointer. The feature is called ["DirectPInvoke"](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/interop.md).

#### Static linking

Statically linking a C library (`.lib`/`.a`) is something that is necessary for some platforms such as iOS and Xbox. This requires ahead-of-time (AOT) compilation as mentioned in dynamic linking.

The main reason for statically linking a C library is "security" but it's really about total control of what gets executed on the devices. Since a dynamic library can be loaded at startup or at runtime by user code, you could hypothetically download additional executable code from somewhere and have it load it at next startup or directly at runtime (think plugins or video game mods). For certain environments which are ["walled gardens"](https://en.wikipedia.org/wiki/Closed_platform) such as iOS or Nintendo Switch, dynamic loading and/or dynamic linking is considered a Pandora's box that must be strictly controlled to which static linking is sometimes the preferred or only alternative method for enforcing centralization. E.g. a "jailbroke" iOS device or a "homebrew" Nintendo Switch is how the [community of "hackers"](https://en.wikipedia.org/wiki/Hacker_culture) such as enthusiasts, amateur developers, or modders hack their devices for enjoyment, curisoity, or extending a product or software on said device. It's how [individuals added emojis to iOS before Apple did to which Apple adopted the changes](https://en.wikipedia.org/wiki/IOS_jailbreaking#Device_customization) (a common theme/relationship for even video game studios and modders). It's also how the [police, criminals, and governments do more nefarious activities such as surveillance, warfare, and/or politics](https://en.wikipedia.org/wiki/Pegasus_(spyware)). Dynamic linking and dynamic loading is ubiquitous beyond such walled gardens or for devices which escaped the walled gardens.

## Lessons learned

### Marshalling

There exist hidden costs for interoperability in C#. How to avoid them?

For C#, the Common Language Runtime (CLR) marshals data between managed and unmanaged contexts (forwards and possibly backwards). In layman's terms, marshalling is transforming the bit representation of a data structure to be correct for the target programming language. For best performance, at worse, marshalling should be minimal, and at best, marshalling should be pass-through. Pass through is the ideal situation when considering performance because both languages agree on the bit representation of data structures without any further processing. C# calls such data structures "blittable". (The sense of the word "blit" means the rapid copying of a block of memory; the word comes from the [bit-block transfer (bit-blit) data operation commonly found in computer graphics](https://en.wikipedia.org/wiki/Bit_blit).) However, to achieve blittable data structures in C#, the garbage collector (GC) is avoided. Why? Because class instances in C# are objects which the allocation of bits can't be controlled precisely by the developer; it's an "implementation detail."

### The garbage collector is a software industry hack

The software industry's attitude, especially business-developers and web-developers, to say that memory is an "implementation detail" and then ignore memory is often justified without knowing or caring for the consequences; it becomes ultimately dangerous.

A function call that changes the state of the system is a side effect. Humans are imperfect at reasoning about side effects, to reason about non-linear systems. An example of a side effect is calling `fopen` in C because it leaves a file in an open state. `malloc` in C is another example of a side effect because it leaves a block of memory allocated. Notice that side effects come in pairs. To close a file, `fclose` is called. To deallocate a block of memory, `free` is called. Other languages have their versions of such function pairs. Some languages went as far as inventing language-specific features, some of which become part of our software programs, so we humans don't have to deal with such pairs of functions. In theory, this is a great idea. And thus, for the specific case of `malloc` and `free`, we invented garbage collection to take us to the promised land of never having to deal with these specific pair of functions.

In practice, using garbage collection to manage your memory automatically turns out to be a horrible idea. This becomes evident if you ever worked on an extensive enough system with the need for real-time responsiveness. In fairness, most applications don't require real-time responsiveness, and it is a lot easier to write safe programs with a garbage collector. However, this is where I think the problem starts. The problem is that developers have become ignorant of why good memory management is essential. This "Oh, the system will take care of it, don't worry." attitude is like a disease that spreads like wild-fire in the industry. The reason is simple: it lowers the bar of experience + knowledge + time required to write safe software. The consequence is that a large number of developers have learned to use a [Golden Hammer](https://en.wikipedia.org/wiki/Law_of_the_instrument#Computer_programming). (The world of finance [also has a definition for Golden Hammer](https://www.investopedia.com/terms/g/golden-hammer.asp) which is relatable.)

Developers have learned to ignore how the hardware operates when solving problems with software, even up to the extreme point that they deny that the hardware even exists. Optimizing code for performance has become an exercise of stumbling around in the pitch-black dark until you find something of interest; it's an afterthought. Even if the developer does find something of interest, it likely opposes his/her worldview of understandable code because they have lost touch with the hardware, lost touch with reality. C# is a useful tool, but you and I have to admit that people mostly use it as Golden Hammer. Just inspect the source code that this tool generates for native bindings as proof of this fact. From my experience, a fair amount of C# developers don't spend their time with such code, don't know how to use structs properly, or even know what blittable data structures are. C# developers (including myself) may need to take a hard look in the mirror, especially if we are open to critizing developers to other programming languages or other fields of business with their own Golden Hammers such as Java, JavaScript, or Electron (:scream:).

## License

`C2CS` is licensed under the MIT License (`MIT`).

There are a few exceptions to this detailed below. See the [LICENSE](LICENSE) file for more details on this main product's license.

`C2CS` uses `libclang` which the header files are included as part of the repository under [`ext/clang`](./ext/clang). This is because `C2CS` generates bindings for `libclang` to which `C2CS` generates bindings for `libclang` and other C libraries. The C header `.h` files (no source code `.c`/`.cpp` files) for `libclang` are included for convience of a source-of-truth for re-generating the bindings. These files are licensed under the Apache License v2.0 with LLVM Exceptions; see the [ext/clang/LICENSE.txt](./ext/clang/LICENSE.txt) for more details. The packaged binaries for `libclang` are used from and maintained by https://github.com/microsoft/ClangSharp.

`C2CS` has Git submodules to various C libraries which are included as part of this repository for purposes of testing and demonstrating by examples. These Git submodules can be considered ["vendoring"](https://stackoverflow.com/questions/26217488/what-is-vendoring). The source code for these projects can be found under the `ext` folder which is short for "external". Each of these libraries have their own license and they are not used by `C2CS` directly for purposes of the tool.


