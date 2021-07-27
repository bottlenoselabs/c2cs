# Documentation

Here you will find documentation for `C2CS` including:

- [How to use `C2CS`](#how-to-use).
- [How to build `C2CS` and the examples from source](#building-from-source).
- [Examples](#examples).

## How to use

To generate bindings for a C library there is two stages: `ast` and `cs`.

```
C2CS:
  C2CS - C to C# bindings code generator.

Usage:
  C2CS [options] [command]

Options:
  --version         Show version information
  -?, -h, --help    Show help and usage information

Commands:
  ast    Dump the abstract syntax tree of a C `.h` file to a `.json` file.
  cs     Generate C# bindings from a C abstract syntax tree `.json` file.
```

### `ast`

Dump the abstract syntax tree of a C `.h` file to a `.json` file. This essentially extracts all the information necessary for generating bindings.

```
ast:
  Dump the abstract syntax tree of a C `.h` file to a `.json` file.

Usage:
  C2CS ast [options]

Options:
  -i, --inputFile <inputFile> (REQUIRED)                                                     Path of the input `.h` header file.
  -o, --outputFile <outputFile> (REQUIRED)                                                   Path of the output abstract syntax tree `.json` file.
  -f, --automaticallyFindSoftwareDevelopmentKit <automaticallyFindSoftwareDevelopmentKit>    Find software development kit for C/C++ automatically. Default is true.
  -s, --includeDirectories <includeDirectories>                                              Search directories for `#include` usages to use when parsing C code.
  -g, --ignoredFiles <ignoredFiles>                                                          Header files to ignore.
  -p, --opaqueTypes <opaqueTypes>                                                            Types by name that will be forced to be opaque.
  -d, --defines <defines>                                                                    Object-like macros to use when parsing C code.
  -b, --bitness <bitness>                                                                    The bitness to parse the C code as. Default is the current architecture of host operating system. E.g. the default for x64 Windows is `64`. Possible values are `32` where pointers are 4 bytes, or `64` where pointers are 8 bytes.
  -a, --clangArgs <clangArgs>                                                                Additional Clang arguments to use when parsing C code.
  -?, -h, --help                                                                             Show help and usage information
```

### `cs`

Generate C# bindings from a C abstract syntax tree `.json` file. This is the stage where the C# code is generated from the abstract syntax tree.

```
cs:
  Generate C# bindings from a C abstract syntax tree `.json` file.

Usage:
  C2CS cs [options]

Options:
  -i, --inputFile <inputFile> (REQUIRED)      Path of the input abstract syntax tree `.json` file.
  -o, --outputFile <outputFile> (REQUIRED)    Path of the output C# `.cs` file.
  -a, --typeAliases <typeAliases>             Types by name that will be remapped.
  -g, --ignoredTypes <ignoredTypes>           Types by name that will be ignored; types are ignored after remapping type names.
  -l, --libraryName <libraryName>             The name of the dynamic link library (without the file extension) used for P/Invoke with C#.
  -c, --className <className>                 The name of the C# static class.
  -?, -h, --help                              Show help and usage information
```

## Examples

Here you will find examples of C libraries being demonstrated with `C2CS` as smoke tests or otherwise used directly.

### Hello world

Hello world example of calling a C function from C#. This is meant to be minimalistic to demonstrate the minimum required things to get this working.

Run the C# project [`helloworld-c`](/src/cs/examples/helloworld/helloworld-c/Program.cs) to build the shared library and generate the bindings for the [`helloworld`](/src/c/examples/helloworld/helloworld) C project.

The C# bindings will be written to [`helloworld.cs`](/src/cs/examples/helloworld/helloworld-cs/helloworld.cs).

### [libclang](./001_LIBCLANG.md)

`C2CS` uses bindings generated for libclang using `C2CS`. In this sense, the `C2CS` project eats it's own dogfood.

Run the C# project [`clang-c`](/src/dotnet/prod/libclang-c/Program.cs) to build the shared library and generate the bindings. This project is not like other examples because the generated bindings are used as part of C2CS in the next build. This means that C2CS generates bindings for libclang which then can generate bindings for libclang.

If you just want to see the bindings, you can take a look at [`libclang.cs`](/src/dotnet/prod/libclang-cs/libclang.cs).

## Building from source

This includes building and running the various examples as part of the project.

### Prerequisites

1. Install [.NET 5](https://dotnet.microsoft.com/download).
2. Install build tools for C/C++.
    - Windows:
      1. Install [Windows Subsystem for Linux v2](https://docs.microsoft.com/en-us/windows/wsl/install-win10) (WSL2).
      2. Install Ubuntu for WSL2.
      3. Install build tools for Ubuntu: ```wsl sudo apt-get update && sudo apt-get install cmake gcc clang gdb```
      4. Install build tools for Ubuntu to cross-compile to Windows: ```wsl sudo apt-get update && sudo apt-get mingw-w64```
    - macOS:
      1. Install XCode CommandLineTools (gcc, clang, etc): ```xcode-select --install```
      2. Install XCode through the App Store (necessary for SDKs).
      3. Install CMake: ```brew install cmake```
    - Linux:
      1. Install the software build tools for your distro including GCC, Clang, and CMake.
3. Clone the repository with submodules: `git clone --recurse-submodules https://github.com/lithiumtoast/c2cs.git`.

### Visual Studio / Rider / MonoDevelop

Open `./C2CS.sln`

### Command Line Interface (CLI)

`dotnet build`