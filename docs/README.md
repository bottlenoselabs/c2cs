# Documentation

Here you will find documentation for `C2CS` including:
- [Getting started with `C2CS`](#getting-started).
- [How to build `C2CS` and the examples from source.](#building-from-source)
- [Examples](#examples)

## Getting started

> :warning: **macOS**: If you downloaded nightly build or a release, ensure that the file is an executable: `chmod +x ./C2CS`. Additionally, you may need to follow directions on [opening up an app from an unidentified developer](https://support.apple.com/guide/mac-help/open-a-mac-app-from-an-unidentified-developer-mh40616/mac).

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
  -l, --libraryName <libraryName>             The name of the dynamic link library (without the file extension) used for P/Invoke with C#.
  -?, -h, --help                              Show help and usage information
```

### Try it out!

In this minimal walkthrough we will create a C function which prints "Hello, world!" and call it from C#.

#### C header/source code

Create the following files. For this example it is assumed you will be working in the same directory whatever that directory is.

`library.h`:
```c
#pragma once

#ifdef _WIN32
__declspec(dllexport)
#endif
void hello_world(void);
```

`library.c`:
```c
#include "library.h"

#include <stdio.h>

void hello_world(void)
{
    printf("Hello, World!\n");
}
```

#### Build the C library

Next, let's build this very simple C library. This will generate the native assembly code which will be later loaded and executed from C#:

```bash
gcc -c -fPIC -o library.o library.c
```

Note that it is a convention to name your dynamic link libraries differently on different platforms. E.g.,
if `library` was the base name then on Windows the file name should be `library.dll`, on macOS `liblibrary.dylib`, and Linux `liblibrary.so`.

Windows:
```bash
gcc -shared -o library.dll library.o
```

macOS:
```bash
gcc -shared -o liblibrary.dylib library.o
```

Linux:
```bash
gcc -shared -o liblibrary.so library.o
```

Additionally it's a good idea to verify that the symbols exist in the library for the functions you want to call from C#. For this minimal example, you should see `_hello_world` or `hello_world` in the list.

macOS:
```bash
nm -g ./liblibrary.dylib

Linux:
```bash
nm -g ./liblibrary.so
```

#### Extract the abstract syntax tree

Next, run `C2CS` to generate the abstract syntax tree from the header file. This will be used later to generate the C# bindings.

```bash
C2CS ast -i ./library.h -o ./ast.json
```

You should get `.json` file like the following:
```json
{
  "fileName": "hello_world.h",
  "functions": [
    {
      "name": "hello_world",
      "returnType": "void",
      "parameters": [],
      "location": {
        "file": "hello_world.h",
        "line": 6,
        "column": 6
      }
    }
  ],
  ...
  "types": [
    {
      "name": "void",
      "kind": "primitive",
      "isSystem": true
    }
  ]
}
```

#### Generate the C# code

Use the previous extracted abstract syntax tree to generate the C# code file.

```bash
C2CS cs -i ./ast.json -o ./library.cs
```

You should get a C# file like this:
```cs
...
public static unsafe partial class library
{
    ...

    // Function @ hello_world.h:6:6
    public static void hello_world()
    {
      ...
    }
}
```

#### Execute from C#

To use the generated C# code you will need to add a dependency on the `C2CS.Runtime` C# library. This C# library provides common functionality for dealing with string conversions, loading/unloading the library function pointers, etc. You can get it as a NuGet package or build it yourself. Here will add the dependency as a NuGet package.

Create the following files:

`nuget.config`
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <config>
        <add key="globalPackagesFolder" value=".nuget\packages" />
    </config>
    <packageSources>
        <add key="nuget" value="https://api.nuget.org/v3/index.json" />
        <add key="lithiumtoast" value="https://www.myget.org/F/lithiumtoast/api/v3/index.json" />
    </packageSources>
</configuration>
```

`hello_world.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <OutputType>WinExe</OutputType>
        <TargetFramework>net5.0</TargetFramework>
        <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="C2CS.Runtime" Version="*-*" />
    </ItemGroup>
</Project>
```

`Program.cs` (Making use of top-level programs in C#9 with .NET 5)
```cs
library.hello_world();
```

Then run the following to download the NuGet dependencies. Note that with the `nuget.config` file above you should see the packages downloaded to `./nuget/packages` folder. This is common practice if you want to manually keep track of downloaded packages and delete them later; otherwise, the packages will be downloaded to your global cache.

```bash
dotnet restore ./hello_world.csproj
```

Then we can build the C# program:
```bash
dotnet build ./hello_world.csproj
```

And run it:
```bash
dotnet run ./hello_world.csproj
```

You will get an error like so:
```
Failed to load 'liblibrary.dylib'. Expected to find the library in one of the following paths
```

This because the native library built earlier needs to be in the specific folder. So the copy/move it.

macOS:
```bash
cp ./liblibrary.dylib ./bin/Debug/net5.0/liblibrary.dylib
```

Linux:
```bash
cp ./liblibrary.so ./bin/Debug/net5.0/liblibrary.so
```

Now, if you run the program you should see "Hello, world!" printed.
```bash
dotnet run ./hello_world.csproj
```
```
Hello, world!
```

Congragulations if you got this far you have successfully used `C2CS`. From here, you check out the [Examples](#examples) for using `C2CS` with various existing popular C libraries.

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

## Examples

See [examples/README.md](examples/README.md).