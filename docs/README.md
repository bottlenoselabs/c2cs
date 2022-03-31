# Documentation

Here you will find documentation for `C2CS` including:

- [Installing `C2CS`](#installing-c2cs)
- [How to use `C2CS`](#how-to-use-c2cs).
- [How to use `C2CS.Runtime`](#how-to-use-c2csruntime).
- [How to build `C2CS` from source](#building-c2cs-from-source).
- [How to debug `C2CS` from source](#debugging-c2cs-from-source)
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

To generate bindings for a C library you need to use a configuration `.json` file which specifies the input to C2CS. See the next sub-section below for documention on each property. See the [Hello World `config.json` file](src/cs/examples/helloworld/helloworld-c/config.json) for an example with annotated comments.

```json
{
  "$schema": "https://github.com/bottlenoselabs/c2cs/schema.json",
  "directory": "path/to/my_c_library/ast",
  "ast": {
    "input_file": "path/to/my_c_library/include/my_c_library.h",
    "platforms": {
      "aarch64-pc-windows": {},
      "x86_64-pc-windows": {},
      "aarch64-apple-darwin": {},
      "x86_64-apple-darwin": {},
      "aarch64-unknown-linux-gnu": {},
      "x86_64-unknown-linux-gnu": {}
    }
  },
  "cs": {
    "output_file": "path/to/my_c_library/cs/my_c_library.cs"
  }
}
```

By default running `c2cs` via terminal will search for a `config.json` file in the current directory. If you want to use a specific `.json` file, specify the file path as the first argument: `c2cs -c myConfig.json`.

### Configuration `.json` properties

#### `inputFilePath`

Path of the input `.h` header file.

#### `outputFilePath`

Path of the output C# `.cs` file. If not specified, defaults to a file path using the current directory, a file name without extension that matches the `inputFilePath`, and a `.cs` file name extension.

#### `abstractSyntaxTreeOutputFilePath`

Path of the intermediate output abstract syntax tree `.json` file. If not specified, defaults to a random temporary file.

#### `libraryName`

The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with C#. If not specified, the library name is the same as the name of the `inputFilePath` without the directory name and without the file extension.

#### `namespaceName`

The name of the namespace to be used for the C# static class. If not specified, the namespace is the same as the `libraryName`.

#### `className`

The name of the C# static class. If not specified, the class name is the same as the `libraryName`.

#### `headerCodeRegionFilePath`

Path of the text file which to add the file's contents to the top of the C# file. Useful for comments, extra namespace using statements, or additional code that needs to be added to the generated C# file.

#### `footerCodeRegionFilePath`

Path of the text file which to add the file's contents to the bottom of the C# file. Useful for comments or additional code that needs to be added to the generated C# file.

#### `mappedTypeNames`

Pairs of strings for re-mapping type names. Each pair has source name and a target name. The source name may be found when parsing C code and get mapped to the target name when generating C# code. Does not change the type's bit layout.

#### `isEnabledFindSdk`

Determines whether the software development kit (SDK) for C/C++ is attempted to be found. Default is `true`. If `true`, the C/C++ header files for the current operating system are attempted to be found. In such a case, if the C/C++ header files can not be found, then an error is generated which halts the program. If `false`, the C/C++ header files will likely be missing causing Clang to generate parsing errors which also halts the program. In such a case, the missing C/C++ header files can be supplied to Clang using `clangArguments` such as `"-isystemPATH/TO/SYSTEM/HEADER/DIRECTORY"`.

#### `machineBitWidth`

The bit width of the computer architecture to use when parsing C code. Default is `null`. If `null`, the bit width of host operating system's computer architecture is used. E.g. the default for x64 Windows is `64`. Possible values are `null`, `32` where pointers are 4 bytes, or `64` where pointers are 8 bytes.

#### `includeDirectories`

Search directory paths to use for `#include` usages when parsing C code. If `null`, uses the directory path of `inputFilePath`.

#### `defines`

Object-like macros to use when parsing C code.

#### `excludedHeaderFiles`

C header file names to exclude. File names are relative to `includeDirectories`.

#### `ignoredTypeNames`

Type names that may be found when parsing C code that will be ignored when generating C# code. Types are ignored after mapping type names using `mappedTypeNames`.

#### `opaqueTypeNames`

Type names that may be found when parsing C code that will be interpreted as opaque types. Opaque types are often used with a pointer to hide the information about the bit layout behind the pointer.

#### `functionNamesWhitelist`

The C function names to explicitly include when parsing C code. Default is `null`. If `null`, no white list applies to which all C function names that are found are eligible for C# code generation. Note that C function names which are excluded also exclude any transitive types.

#### `clangArguments`

Additional Clang arguments to use when parsing C code.

## How to use `C2CS.Runtime`

The `C2CS.Runtime` C# code is directly added to the bottom of the generated bindings in a class named `Runtime` with a C# region named `C2CS.Runtime`. The `Runtime` static class contains helper structs, methods, and other kind of "glue" that make interoperability with C in C# easier and more idiomatic.

### Custom C# project properties for `C2CS.Runtime`

#### `SIZEOF_WCHAR_T`:

The following only applies and is of interest to you if you are using `wchar_t` directly in the public C header. Note that `wchar_t*` does not apply, it has to be directly using `wchar_t`. 

Use a value of `1`, `2`, or `4` to specify the backing byte field size of `CCharWide`. `CCharWide` in C# is intended to be blittable to `wchar_t` in C. There is no default value set for `SIZEOF_WCHAR_T` but the default size of `CCharWide` is 2. This is incorrect on some platforms like Linux.

To set it:

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
      1. Install Git Bash. (Usually installed with Git for Windows: https://git-scm.com/downloads.)
      2. Install MSCV (Microsoft Visual C++) Build Tools + some C/C++ SDK for Windows. (You can use Visual Studio Installer application to install the C/C++ workload or the components individually. You can also install it all via web or appropriate command line.)
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

## Debugging `C2CS` from source

### Debugging using logging

Structured logging is added for sanity checking of normal/expected operation. It is also extremely helpful for diagnosing or digging into a problem. In more advanced situations it is used for automated black box testing. Any log can easily be identifiable back to the code via a quick search.

An example of some logging output for console:

```
2022-31-03 01:54:24 info: [0] Configuration load: Success. Path: path/to/c2cs/bin/helloworld-c/Debug/net6.0/config.json.
2022-31-03 01:54:24 info: [2] => Extract AST C - Started
2022-31-03 01:54:24 info: [5] => Extract AST C => Install Clang macOS - Step started
2022-31-03 01:54:24 trce: [8] => Extract AST C => Install Clang macOS - Success
2022-31-03 01:54:24 info: [6] => Extract AST C => Install Clang macOS - Step finished in 0.001 seconds
2022-31-03 01:54:24 info: [5] => Extract AST C => Parse x86_64-pc-windows - Step started
2022-31-03 01:54:25 trce: [10] => Extract AST C => Parse x86_64-pc-windows - Success. Path: path/to/c2cs/src/cs/examples/helloworld/helloworld-c/my_c_library/include/my_c_library.h ; Clang arguments: --language=c --std=c11 -Wno-pragma-once-outside-header -fno-blocks --include-directory=/Users/lstranks/Programming/bottlenose/c2cs/src/cs/examples/helloworld/helloworld-c/my_c_library/include --target=x86_64-pc-windows ; Diagnostics: 0
2022-31-03 01:54:25 info: [6] => Extract AST C => Parse x86_64-pc-windows - Step finished in 0.119 seconds
2022-31-03 01:54:25 info: [5] => Extract AST C => Extract x86_64-pc-windows - Step started
2022-31-03 01:54:25 trce: [13] => Extract AST C => Extract x86_64-pc-windows - Translation unit my_c_library.h
2022-31-03 01:54:25 trce: [17] => Extract AST C => Extract x86_64-pc-windows - Enum my_enum_week_day
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type signed int
2022-31-03 01:54:25 trce: [16] => Extract AST C => Extract x86_64-pc-windows - Function hello_world
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type void
2022-31-03 01:54:25 trce: [16] => Extract AST C => Extract x86_64-pc-windows - Function pass_string
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type char*
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type char
2022-31-03 01:54:25 trce: [16] => Extract AST C => Extract x86_64-pc-windows - Function pass_integers_by_value
2022-31-03 01:54:25 trce: [16] => Extract AST C => Extract x86_64-pc-windows - Function pass_integers_by_reference
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type uint16_t*
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type int32_t*
2022-31-03 01:54:25 trce: [22] => Extract AST C => Extract x86_64-pc-windows - Type uint64_t*
2022-31-03 01:54:25 trce: [16] => Extract AST C => Extract x86_64-pc-windows - Function pass_enum
2022-31-03 01:54:25 trce: [12] => Extract AST C => Extract x86_64-pc-windows - Success
2022-31-03 01:54:25 info: [6] => Extract AST C => Extract x86_64-pc-windows - Step finished in 0.026 seconds
2022-31-03 01:54:25 info: [5] => Extract AST C => Write x86_64-pc-windows - Step started
2022-31-03 01:54:25 trce: [25] => Extract AST C => Write x86_64-pc-windows - Write abstract syntax tree C: Success. Path: path/to/c2cs/src/cs/examples/helloworld/helloworld-c/my_c_library/ast/x86_64-pc-windows.json
2022-31-03 01:54:25 info: [6] => Extract AST C => Write x86_64-pc-windows - Step finished in 0.086 seconds
...
2022-31-03 01:54:25 info: [3] => Extract AST C - Success in 0.495 seconds
```

Things to notice from left to right:

- Date
- Time
- Log level:
    -  `trce` stands for "trace". Ignorable at a glance; used for verbose information about what the program is doing exactly.
    -  `info`. Used for general higher level information about what the program is doing. Good for building automation tests from logging.
    -  `warn` stands for "warning". Suspicious; indicative of an expected but undesired outcome. Does not halt the program.
    -  `error`. Unacceptable; indicative of an unexpected result which should get fixed. Does not halt the program.
    -  `crit`. Crash; gracefully exit the program with a stack trace.
- `[number]`: Used for automated tests and otherwise quick searching. Each kind of log is easily identifiable by a unique numeric identifier. E.g., when any use case begins it is 2. When any use case end successfully it is 3.
- `=>`: The scope of the log. Helps keep track of the context of the log. Scopes can be nested with more than one `=>`. E.g. there is a scope for the use case of "Extract AST C" and a scope for "Extract x86_64-pc-windows". `x86_64-pc-windows` is the target platform to parse the C code with Clang.

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
