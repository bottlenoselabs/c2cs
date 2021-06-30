# Documentation

Here you will find documentation for `C2CS` including:

- [Getting started with `C2CS`](#getting-started).
- [How to build `C2CS` and the examples from source](#building-from-source).
- [Examples](#examples).

## Getting started

See https://github.com/lithiumtoast/c2cs-example-helloworld for minimal example of using `C2CS`.

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
  -i, --inputFile <inputFile> (REQUIRED)                                Path of the input `.h` header file.
  -o, --outputFile <outputFile> (REQUIRED)                              Path of the output abstract syntax tree `.json` file.
  -f, --automaticallyFindSoftwareDevelopmentKit                         Find software development kit for C/C++ automatically. Default is
  <automaticallyFindSoftwareDevelopmentKit>                             true.
  -s, --includeDirectories <includeDirectories>                         Search directories for `#include` usages to use when parsing C
                                                                        code.
  -g, --ignoredFiles <ignoredFiles>                                     Header files to ignore.
  -p, --opaqueTypes <opaqueTypes>                                       Types by name that will be forced to be opaque.
  -d, --defines <defines>                                               Object-like macros to use when parsing C code.
  -a, --clangArgs <clangArgs>                                           Additional Clang arguments to use when parsing C code.
  -?, -h, --help                                                        Show help and usage information
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
  -?, -h, --help                              Show help and usage information
```

## Examples

See [examples/README.md](examples/README.md).

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

Open `./src/dotnet/C2CS.sln`

### Command Line Interface (CLI)

`dotnet build ./src/dotnet/C2CS.sln`