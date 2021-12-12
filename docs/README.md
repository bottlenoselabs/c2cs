# Documentation

Here you will find documentation for `C2CS` including:

- [Installing `C2CS`](#installing-c2cs)
- [How to use `C2CS`](#how-to-use-c2cs).
- [How to use `C2CS.Runtime`](#how-to-use-c2csruntime).
- [How to build `C2CS` from source](#building-c2cs-from-source).
- [Examples](#examples).

## Installing `C2CS`

`C2CS` is distributed as a NuGet tool. To get started all you need is the .NET software development kit to access `dotnet tool`.

### Latest release

```bash
dotnet tool install bottlenoselabs.c2cs --global 
```

### Latest pre-release

```bash
dotnet tool install bottlenoselabs.c2cs --global --add-source https://www.myget.org/F/bottlenoselabs/api/v3/index.json --version "*-*"
```

- 💡 For a specific pre-release, including a specific pull-request or the latest Git commit of the `main` branch, see: https://www.myget.org/feed/bottlenoselabs/package/nuget/bottlenoselabs.C2CS.
- 💡 If you see a specific version but the `dotnet tool` command doesn't see it, try clearing your NuGet caches:
```bash
dotnet nuget locals all --clear
```

## How to use `C2CS`

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
  -i, --inputFile <inputFile> (REQUIRED)                                                   File path of the input `.h` header file.
  -o, --outputFile <outputFile> (REQUIRED)                                                 File path of the output abstract syntax tree `.json` file.
  -f, --automaticallyFindSoftwareDevelopmentKit <automaticallyFindSoftwareDevelopmentKit>  Find software development kit for C/C++ automatically. Default is `true`.
  -s, --includeDirectories <includeDirectories>                                            Search directories for `#include` usages to use when parsing C code.
  -g, --ignoredFiles <ignoredFiles>                                                        Header files to ignore by file name with extension (exclude directory path).
  -p, --opaqueTypes <opaqueTypes>                                                          Types by name that will be forced to be opaque.
  -d, --defines <defines>                                                                  Object-like macros to use when parsing C code.
  -b, --bitness <bitness>                                                                  The bitness to parse the C code as. Default is the current architecture of host operating system. E.g. 
                                                                                           the default for x64 Windows is `64`. Possible values are `32` where pointers are 4 bytes, or `64` where 
                                                                                           pointers are 8 bytes.
  -a, --clangArgs <clangArgs>                                                              Additional Clang arguments to use when parsing C code.
  -w, --whitelistFunctionsFile <whitelistFunctionsFile>                                    The file path to a text file containing a set of function names delimited by new line. These functions 
                                                                                           will strictly only be considered for bindgen; this has implications for transitive types. Each function 
                                                                                           name may start with some text followed by a `!` character before the name of the function; this allows to 
                                                                                           re-use the same file for input to DirectPInvoke with NativeAOT.
  -?, -h, --help                                                                           Show help and usage information
```

### `cs`

Generate C# bindings from a C abstract syntax tree `.json` file. This is the stage where the C# code is generated from the abstract syntax tree.

```
cs:
  Generate C# bindings from a C abstract syntax tree `.json` file.

Usage:
  C2CS cs [options]

Options:
  -i, --inputFile <inputFile> (REQUIRED)     File path of the input abstract syntax tree `.json` file.
  -o, --outputFile <outputFile> (REQUIRED)   File path of the output C# `.cs` file.
  -a, --typeAliases <typeAliases>            Types by name that will be remapped.
  -g, --ignoredNamesFile <ignoredNamesFile>  File path of the text file with new-line separated names (types, functions, macros, etc) that will be ignored; types are ignored after remapping type names.
  -l, --libraryName <libraryName>            The name of the dynamic link library (without the file extension) used for P/Invoke with C#.
  -c, --className <className>                The name of the C# static class.
  -n, --injectNamespaces <injectNamespaces>  Additional namespaces to inject near the top of C# file as using statements.
  -w, --wrapNamespace <wrapNamespace>        The namespace to be used for C# static class. If not specified the C# static class does not have a namespace to which it is in the global namespace.
  -?, -h, --help                             Show help and usage information
```

## How to use `C2CS.Runtime`

The `C2CS.Runtime` NuGet package for C# projects is required for the generated bindings because there are helper types and methods or otherwise "glue" that make interoperability with C in C# possible, easier, and more idiomatic.

Just like `C2CS`, `C2CS.Runtime` is distributed as a NuGet package. You can add it to your `.csproj` using the package id `bottlenoselabs.C2CS.Runtime`. You should always use the same version as `C2CS`. For example, in your `.csproj`:

```xml
<!-- NuGet package references -->
<ItemGroup>
  <PackageReference Include="bottlenoselabs.C2CS.Runtime" Version="x.y.z" PrivateAssets="all" />
</ItemGroup>  
```

Note that `PrivateAssets` is `all` so that the NuGet package does not flow to any projects which depend on your C# project. This is required because additional C# files are packaged not as assemblies but as raw C# `.cs` files which your C# project will compile. Having it flow to other C# projects transitively will result in compiler errors.

### Getting a pre-release package of `C2CS.Runtime` 

While `C2CS` is being developed, the packages are not deployed to nuget.org, but rather a different feed. To make the pre-release feed accessible so that you can access the pre-release NuGet package in your C# project, create or modify your `NuGet.config` file which should be beside your `.sln` or `.csproj`. (Where to place it is depending if you want to apply to just one C# project in your solution or all your C# projects in your solution).

For example, the following `NuGet.config` file has the `bottlenoselabs` added as a key with a key value pointing to the feed URI.

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
    <packageSources>
        <clear />
        <add key="nuget" value="https://api.nuget.org/v3/index.json" />
        <add key="bottlenoselabs" value="https://www.myget.org/F/bottlenoselabs/api/v3/index.json" />
    </packageSources>
</configuration>
```

### Custom C# project properties for `C2CS.Runtime`

Because `C2CS.Runtime` ships with C# `.cs` files that your C# project compiles, it is possible to configure some settings for the code in your C# project `.csproj` file.

#### `SIZEOF_WCHAR_T`:

Use a value of `1`, `2`, or `4` to specify the backing byte field size of `CCharWide`. `CCharWide` in C# is intended to be blittable to `wchar_t` in C. The default value is `2` on Windows + macOS like operating systems, and `4` on Linux like operating systems.

Example:

```xml
<PropertyGroup>
  <SIZEOF_WCHAR_T>2</SIZEOF_WCHAR_T>
</PropertyGroup>
```  

## Building `C2CS` from source

### Prerequisites

1. Install [.NET 6](https://dotnet.microsoft.com/download).
2. Install build tools for C/C++.
    - Windows:
      1. Install [Windows Subsystem for Linux v2](https://docs.microsoft.com/en-us/windows/wsl/install-win10) (WSL2).
      2. Install Ubuntu for WSL2.
      3. Install build tools for Ubuntu: ```wsl sudo apt-get update && sudo apt-get install cmake gcc clang gdb```
      4. Install build tools for Ubuntu to cross-compile to Windows: ```wsl sudo apt-get update && sudo apt-get mingw-w64```
    - macOS:
      1. Install XCode CommandLineTools (gcc, clang, etc): ```xcode-select --install```
      2. Install XCode through the App Store (necessary for SDKs).
      3. Install Brew if you have not already: https://brew.sh
      4. Install CMake: ```brew install cmake```
    - Linux:
      1. Install the software build tools for your distro including GCC, Clang, and CMake.
3. Clone the repository with submodules: `git clone --recurse-submodules https://github.com/lithiumtoast/c2cs.git`.

### Visual Studio / Rider / MonoDevelop

Open `./C2CS.sln`

### Command Line Interface (CLI)

`dotnet build`

## Examples

Here you will find examples of C libraries being demonstrated with `C2CS` as smoke tests or otherwise used directly.

### Hello world

Hello world example of callings C functions from C#. This is meant to be minimalistic to demonstrate the minimum required things to get this working.

1. Run the C# project [`helloworld-c`](/src/cs/examples/helloworld/helloworld-c/Program.cs). This builds the example shared library and generate the bindings for the [`my_c_library`](/src/cs/examples/helloworld/helloworld-c/my_c_library) C project. The C# bindings will be written to [`my_c_library.cs`](/src/cs/examples/helloworld/helloworld-cs/my_c_library.cs).
2. Run the C# project [`helloworld-cs`](/src/cs/examples/helloworld/helloworld-cs/Program.cs). You should see output to the console of C functions being called from C#.

### [libclang](./001_LIBCLANG.md)

`C2CS` uses bindings generated for libclang using `C2CS`. In this sense, the `C2CS` project eats it's own dogfood.

Run the C# project [`clang-c`](/src/dotnet/prod/libclang-c/Program.cs) to build the shared library and generate the bindings. This project is not like other examples because the generated bindings are used as part of C2CS in the next build. This means that C2CS generates bindings for libclang which then can generate bindings for libclang.

If you just want to see the bindings, you can take a look at [`libclang.cs`](/src/dotnet/prod/libclang-cs/libclang.cs).
