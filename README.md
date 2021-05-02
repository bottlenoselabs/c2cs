# C2CS

C to C# bindings code generator. In go `.h` file, out come `.cs`.

## Background: Why?

### Problem

When creating applications with C# (especially games), it's sometimes necessary to dip down into C/C++ for better performance. However, maintaining the bindings becomes time consuming, error-prone, and in some cases quite tricky.

### Solution

Automatically generate the bindings by compiling/parsing a C `.h` file. Essentially, the C public API for the target operating system + architecture is transpiled to C#. 

This includes all C extern functions which are transpiled to `static` extern methods in C#. Also includes transpiling all the C types to C# which are found through transitive property to the extern functions such as `struct`s, `enum`s, and `const`s. C# `struct`s are generated instead of `class`es on purpose to achieve 1-1 bit-representation of C to C# types called *blittable* types. The reason for blittable types is to achieve pass-through marshalling and active avoidance of the Garbage Collector in C# for best possible runtime performance when doing interoperability with C. 

This is all accomplished by using [libclang](https://clang.llvm.org/docs/Tooling.html) for parsing C and [Roslyn](https://github.com/dotnet/roslyn) for generating C#. All naming is left as found in the header file of the C code.

### Other solutions

#### For tricky C libraries

This project does not work for every C library. This is due to some technical limitations where some C libraries are not "bindgen-friendly".

##### Bindgen-friendly

Note that this list only applies to the **external linkage** of the C API; the internal guts of the C library is irrelevant.

- No macros.
- No C++.
- No implicit types, types must be explicit so they can be found; e.g. it is not possible to transpile an enum if it is never part of the public facing API.
- (This list may be updated as more things are discovered...)

For such tricky libraries which are not "bindgen-friendly" I recommend you take a look at: https://github.com/InfectedLibraries/Biohazrd

#### Other similar projects

Mentioned here for completeness.

- https://github.com/microsoft/ClangSharp
- https://github.com/SharpGenTools/SharpGenTools
- https://github.com/rds1983/Sichem

## Developers: Building from Source

### Prerequisites

1. Download and install [.NET 5](https://dotnet.microsoft.com/download).
2. Clone the repository with submodules: `git clone --recurse-submodules https://github.com/lithiumtoast/c2cs.git`.

### Visual Studio / Rider / MonoDevelop

Open `./src/dotnet/C2CS.sln`

### Command Line Interface (CLI)

`dotnet build ./src/dotnet/C2CS.sln`

## Using C2CS

```
C2CS:
  C2CS - C to C# bindings code generator.

Usage:
  C2CS [options]

Options:
  -i, --inputFilePath <inputFilePath> (REQUIRED)       File path of the input .h file.
  -p, --additionalInputPaths <additionalInputPaths>    Directory paths and/or file paths of additional .h files to bundle together before parsing C code.
  -o, --outputFilePath <outputFilePath> (REQUIRED)     File path of the output .cs file.
  -u, --unattended                                     Don't ask for further input.
  -c, --className <className>                          The name of the generated C# class name.
  -l, --libraryName <libraryName>                      The name of the dynamic link library (without the file extension) used for P/Invoke with C#.
  -t, --printAbstractSyntaxTree                        Print the Clang abstract syntax tree as it is discovered to standard out. Note that it does not print parts of the abstract syntax tree
                                                       which are already discovered. This option is useful for troubleshooting.
  -s, --includeDirectories <includeDirectories>        Search directories for `#include` usages to use when parsing C code.
  -d, --defines <defines>                              Object-like macros to use when parsing C code.
  -a, --clangArgs <clangArgs>                          Additional Clang arguments to use when parsing C code.
  --version                                            Show version information
  -?, -h, --help                                       Show help and usage information
```

## [Examples](docs/examples/README.md)

## Troubleshooting

For troubleshooting use the `-t` option to print the Clang abstract syntax tree as the header file(s) are parsed. If you find a problem or need help you can reach out by creating a GitHub issue.

## Lessons learned

### Marshalling

There exist hidden costs for interoperability in C#. How to avoid them?

For C#, the Common Language Runtime (CLR) marshals data between managed and unmanaged contexts (forwards and possibly backwards). In layman's terms, marshalling is transforming the bit representation of a data structure to be correct for the target programming language. For best performance, at worse, marshalling should be minimal, and at best, marshalling should be pass-through. Pass through is the ideal situation when considering performance because both languages agree on the bit representation of data structures without any further processing. C# calls such data structures "blittable" after the bit-block transfer (bit-blit) data operation commonly found in computer graphics. However, to achieve blittable data structures in C#, the garbage collector (GC) is avoided. Why? Because class instances in C# are objects which the allocation of bits can't be controlled precisely by the developer; it's an "implementation detail."

### The garbage collector is a software industry hack

The software industry's attitude, especially business-developers and web-developers, to say that memory is an "implementation detail" and then ignore memory ultimately is very dangerous.

A function call that changes the state of the system is a side effect. Humans are imperfect at reasoning about side effects, to reason about non-linear systems. An example of a side effect is calling `fopen` in C because it leaves a file in an open state. `malloc` in C is another example of a side effect because it leaves a block of memory allocated. Notice that side effects come in pairs. To close a file, `fclose` is called. To deallocate a block of memory, `free` is called. Other languages have their versions of such function pairs. Some languages went as far as inventing language-specific features, some of which become part of our software programs, so we humans don't have to deal with such pairs of functions. In theory, this is a great idea. And thus, we invented garbage collection to take us to the promised land of never having to deal with the specific pair of functions `malloc` and `free` ever again.

In practice, using garbage collection to manage your memory automatically turns out to be a horrible idea. This becomes evident if you ever worked on an extensive enough system with the need for real-time responsiveness. In fairness, most applications don't require real-time responsiveness, and it is a lot easier to write safe programs with a garbage collector. However, this is where I think the problem starts. The problem is that developers have become ignorant of why good memory management is essential. This "Oh, the system will take care of it, don't worry." attitude is like a disease that spreads like wild-fire in the industry. The reason is simple: it lowers the bar of experience + knowledge + time required to write safe software. The consequence is that a large number of developers have learned to use a [Golden Hammer](https://en.wikipedia.org/wiki/Law_of_the_instrument#Computer_programming). They have learned to ignore how the hardware operates when solving problems, even up to the extreme point that they deny that the hardware even exists. Optimizing code for performance has become an exercise of stumbling around in the pitch-black dark until you find something of interest; it's an afterthought. Even if the developer does find something of interest, it likely opposes his/her worldview of understandable code because they have lost touch with the hardware, lost touch with reality. C# is a useful tool, but you and I have to admit that people mostly use it as Golden Hammer. Just inspect the source code that this tool generates for native bindings as proof of this fact. From my experience, a fair amount of C# developers don't spend their time with such code, don't know how to use structs properly, or even what blittable data structures are. C# developers (including myself) may need to take a hard look in the mirror, especially if we are open to critizing developers of other languages who have the same problem with Java or JavaScript.

## License

C2CS is licensed under the MIT License (`MIT`). There are a few exceptions to this detailed below. See the [LICENSE](LICENSE) file for more details on this main product's license.

C2CS uses libclang which the header files are included as part of the repository under [`ext/clang`](./ext/clang). These files are licensed under the Apache License v2.0 with LLVM Exceptions; see the [ext/clang/LICENSE.txt](./ext/clang/LICENSE.txt) for more details.
