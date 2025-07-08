# Documentation

- [Documentation](#documentation)
  - [Supported platforms](#supported-platforms)
  - [Lessons Learned](#lessons-learned)
  - [Installing `C2CS`](#installing-c2cs)
    - [Latest release of `C2CS`](#latest-release-of-c2cs)
    - [Latest pre-release of `C2CS`](#latest-pre-release-of-c2cs)
  - [How to use `C2CS`](#how-to-use-c2cs)
    - [Installing and using `c2ffi`](#installing-and-using-c2ffi)
    - [Execute `c2cs`](#execute-c2cs)
  - [How to use the `Interop.Runtime`](#how-to-use-the-interopruntime)
  - [Building `C2CS` from source](#building-c2cs-from-source)
    - [Prerequisites](#prerequisites)
    - [Visual Studio / Rider](#visual-studio--rider)
    - [Command Line Interface (CLI)](#command-line-interface-cli)
  - [Debugging `C2CS` from source](#debugging-c2cs-from-source)
    - [Debugging using logging](#debugging-using-logging)
  - [Examples](#examples)
    - [Hello world](#hello-world)

## Supported platforms

See [SUPPORTED-PLATFORMS.md](./SUPPORTED-PLATFORMS.md).

## Lessons Learned

See [LESSONS-LEARNED.md](./LESSONS-LEARNED.md).

## Installing `C2CS`

`C2CS` is distributed as a NuGet tool. To get started, the .NET 9 software development kit (SDK) is required.

### Latest release of `C2CS`

```bash
dotnet tool install bottlenoselabs.c2cs.tool --global 
```

### Latest pre-release of `C2CS`

```bash
dotnet tool install bottlenoselabs.c2cs.tool --global --add-source https://www.myget.org/F/bottlenoselabs/api/v3/index.json --version "*-*"
```

- 💡 For a specific pre-release, including a specific pull-request or the latest Git commit of the `main` branch, see: https://www.myget.org/feed/bottlenoselabs/package/nuget/bottlenoselabs.C2CS.
- 💡 If you see a specific version but the `dotnet tool` command doesn't see it, try clearing your NuGet caches:
```bash
dotnet nuget locals all --clear
```

## How to use `C2CS`

To generate C# bindings for a C library you need to first install and use the `c2ffi` tool. Then setup a couple configuration files. See the [`helloworld-bindgen`](../src/cs/examples/helloworld/helloworld-bindgen) example projects for an example of these configuration files.

### Installing and using `c2ffi`

See the auxiliary project `Getting Started` section: https://github.com/bottlenoselabs/c2ffi#getting-started. 

You should extract all the platform specific FFIs you wish to have as target platforms using `c2ffi extract --config ...`. See the [`helloworld-bindgen/config-extract.json`](../src/cs/examples/helloworld/helloworld-bindgen/config-extract.json) for example configuration file for Windows, macOS, and Linux platforms.

Once all the platform FFIs are extracted to a directory, merge them together into a cross-platform FFI using `c2ffi merge --inputDirectoryPath ... --outputFilePath ...` option.

See the [`helloworld-bindgen`](../src/cs/examples/helloworld/helloworld-bindgen/Program.cs) C# program for example of using `c2ffi` from command line.

Once you have a cross-platform FFI `.json` file, you are ready to use `c2cs`.

### Execute `c2cs`

Run `c2cs --config ...` from terminal specifying a configuration file. See the [`config-generate-cs.json`](../src/cs/examples/helloworld/helloworld-bindgen/config-generate-cs.json) for an example configuration `.json` file.

## How to use the `Interop.Runtime`

The [`Interop.Runtime`](../src/cs/examples/helloworld/helloworld-app/Generated/Runtime.g.cs) C# code is by default generated to a new file as `Runtime.g.cs`. The `Interop.Runtime` namespace contains helper structs, methods, and other kind of "glue" that make interoperability with C in C# easier and more idiomatic.

See the [HelloWorld example](../src/cs/examples/helloworld/helloworld-app/Program.cs) for C# code that uses and explains how to use `Interop.Runtime`.

## Building `C2CS` from source

### Prerequisites

1. Install [.NET 9 SDK](https://dotnet.microsoft.com/download).
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
3. Clone the repository with submodules: `git clone --recurse-submodules https://github.com/bottlenoselabs/c2cs.git`.

### Visual Studio / Rider

Open `./C2CS.sln`

### Command Line Interface (CLI)

`dotnet build`

## Debugging `C2CS` from source

### Debugging using logging

By default `C2CS` has logs enabled for `Information` level. To enable logs for `Debug` level place the following `appsettings.json` file beside `C2CS` or in the current directory where `C2CS` is being run from. You can also change some other settings for logs through this file.

```json
{
    "Logging": {
        "Console": {
            "LogLevel": {
                "Default": "Warning",
                "C2CS": "Debug"
            },
            "FormatterOptions": {
                "ColorBehavior": "Enabled",
                "SingleLine": true,
                "IncludeScopes": true,
                "TimestampFormat": "yyyy-dd-MM HH:mm:ss ",
                "UseUtcTimestamp": true
            }
        }
    }
}
```

## Examples

Here you will find examples of C libraries being demonstrated with `C2CS` as smoke tests or otherwise used directly.

### Hello world

Hello world example of callings C functions from C#. This is meant to be minimalistic to demonstrate the minimum required things to get this working.

1. Run the C# project [`helloworld-bindgen`](../src/cs/examples/helloworld/helloworld-bindgen/Program.cs). This builds the example shared C library and generate the bindings for the [`my_c_library`](../src/cs/examples/helloworld/helloworld-bindgen/my_c_library) C project. The C# bindings will be written to [`my_c_library.g.cs`](../src/cs/examples/helloworld/helloworld-app/Generated/my_c_library.g.cs).
2. Run the C# project [`helloworld-app`](../src/cs/examples/helloworld/helloworld-app/Program.cs). You should see output to the console of C functions being called from C#.
