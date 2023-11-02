# Documentation

- [Documentation](#documentation)
  - [Supported platforms](#supported-platforms)
  - [Lessons Learned](#lessons-learned)
  - [Installing `C2CS`](#installing-c2cs)
    - [Latest release of `C2CS`](#latest-release-of-c2cs)
    - [Latest pre-release of `C2CS`](#latest-pre-release-of-c2cs)
  - [How to use `C2CS`](#how-to-use-c2cs)
    - [Installing and using `CAstFfi`](#installing-and-using-castffi)
    - [Execute `c2cs`](#execute-c2cs)
  - [How to use `C2CS.Runtime`](#how-to-use-c2csruntime)
  - [Building `C2CS` from source](#building-c2cs-from-source)
    - [Prerequisites](#prerequisites)
    - [Visual Studio / Rider / MonoDevelop](#visual-studio--rider--monodevelop)
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

`C2CS` is distributed as a NuGet tool. To get started, the .NET 7 software development kit (SDK) is required.

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

To generate C# bindings for a C library you need to install and use the `CAstFfi` tool and setup a couple configuration files. See the [`helloworld`](../src/cs/examples/helloworld/) example projects for an example.

### Installing and using `CAstFfi`

See the auxiliary project `Getting Started` section: https://github.com/bottlenoselabs/CAstFfi#getting-started. 

You should extract all the platform specific abstract syntax trees you wish to have as target platforms. See the [`helloworld-compile-c-library-and-generate-bindings`](../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/) for example configuration files for Windows, macOS, and Linux platforms.

Once all the platform abstract syntax trees are extracted to a directory, merge them together into a cross-platform abstract syntax tree using `CAstFfi merge` option.

Once you have a cross-platform abstract syntax tree, you are ready to use `c2cs`.

### Execute `c2cs`

Run `c2cs --config` from terminal specifying a configuration file. See the [`config-generate-cs.json`](/src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/config-generate-cs.json) for an example in the `helloworld-compile-c-library-and-generate-bindings` example.

## How to use `C2CS.Runtime`

The `C2CS.Runtime` C# code is directly added to the bottom of the generated bindings in a class named `Runtime` with a C# region named `C2CS.Runtime`. The `Runtime` static class contains helper structs, methods, and other kind of "glue" that make interoperability with C in C# easier and more idiomatic.

## Building `C2CS` from source

### Prerequisites

1. Install [.NET 7 SDK](https://dotnet.microsoft.com/download).
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

1. Run the C# project [`helloworld-compile-c-library-and-generate-bindings`](../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/Program.cs). This builds the example shared library and generate the bindings for the [`my_c_library`](../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/my_c_library/) C project. The C# bindings will be written to [`my_c_library.cs`](../src/cs/examples/helloworld/helloworld-app/my_c_library.cs).
2. Run the C# project [`helloworld-cs`](../src/cs/examples/helloworld/helloworld-app/Program.cs). You should see output to the console of C functions being called from C#.
