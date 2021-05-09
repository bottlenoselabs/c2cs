# Documentation

Here you will find documentation for `C2CS` including:
- [How to install `C2CS` using NuGet](#install-using-nuget).
- [How to build `C2CS` and the examples from source.](#building-from-source)
- [How to use `C2CS`](#using-c2cs).

## Install using NuGet

This is recommended if you only want to use `C2CS` without setting up a full developer environment to build `C2CS` from source. E.g., using `C2CS` in a GitHub workflow, DevOps build pipeline, or just locally on your computer.

> :warning: **If you are using Windows or macOS**: Not yet supported, currently only works for Linux. See issue #14

Run [`nuget-install.sh`](/nuget-install.sh) to install `C2CS` as a global NuGet tool.

## Building from source

This includes building and running the examples.

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

## Using `C2CS`

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

### Troubleshooting

For troubleshooting use the `-t` option to print the Clang abstract syntax tree as the header file(s) are parsed. If you find a problem or need help you can reach out by creating a GitHub issue.