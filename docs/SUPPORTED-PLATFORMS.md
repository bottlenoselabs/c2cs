# Supported Platforms

- [Supported Platforms](#supported-platforms)
  - [Introduction](#introduction)
  - [Generating C# code for a platform](#generating-c-code-for-a-platform)
    - [About system C headers files](#about-system-c-headers-files)
      - [Reading C code correctly](#reading-c-code-correctly)
      - [Restrictive or closed-source system headers](#restrictive-or-closed-source-system-headers)
        - [Problem](#problem)
        - [Solution](#solution)
  - [Calling C platform code from your .NET application](#calling-c-platform-code-from-your-net-application)
    - [Dynamic loading & dynamic linking](#dynamic-loading--dynamic-linking)
      - [Dynamic loading](#dynamic-loading)
      - [Dynamic linking](#dynamic-linking)
    - [Static linking](#static-linking)
  - [Platforms](#platforms)
    - [Platform Matrix Notes](#platform-matrix-notes)
    - [Platform Matrix](#platform-matrix)

## Introduction

A platform is defined as a combination of instruction set architecture a.k.a "computer architecture" (e.g. `ARM64`, `ARM32`, `X64`, `X86`, etc) and an operating system for that computer architecture (e.g. `Windows`, `macOS`, `Linux`, `Android`, `iOS`, etc).

Note that pointers such as `void*` can have different sizes across target computer architectures. E.g., `X86` pointers are 4 bytes and `X64` (aswell as `ARM64`) pointers are 8 bytes. This means that if you need to support different word size computer architectures you will need to have seperate bindings for each, even if they are the same operating system.

That being said, 64-bit word size is pretty ubiquitous on Windows these days, at least for gaming, as you can see from [Steam hardware survey where 64-bit is 99%+](https://store.steampowered.com/hwsurvey/directx/). Additionally, you can see that the ["trend" is that 64-bit is becoming standard over time with 32-bit getting dropped](https://en.wikipedia.org/wiki/64-bit_computing#64-bit_operating_system_timeline). If you are planning on targeting modern machines, I would advise just forgeting about target platforms with 32-bit computer architectures such as `X86` and `ARM32`.

## Generating C# code for a platform

Support for generating C# code of a C library for different target platforms is dependent on three things:
1. A ["Clang target triple"](https://clang.llvm.org/docs/CrossCompilation.html) (a.k.a. "target"). Targets are identified by a string in a specific format of `arch-vendor-os-environment` and passed to Clang which informs how to read C code.
2. System C header `.h` files of the target platform . The root directory of where the files are located need to be passed to Clang to read C code correctly. The files are often distributed with a software development environment (SDE) or additional downloadable components to the SDE in a form of a software development kit (SDK).
3. A [.NET runtime identifiers (RID)](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) for building and running a .NET application for the target platform. The correct .NET runtime identifier must match to the corresponding Clang target for the target platform.

### About system C headers files

#### Reading C code correctly

The system headers for a target platform must passed to Clang to read C code correctly. Not doing so can lead to incorrect reading of C code for multiple possible reasons.

A simple example is wrong bit widths for types. `uint64_t` is defined on Linux in `/usr/include/bits/stdint-uintn.h` as the following:
```c
typedef __uint64_t uint64_t;
```
This is a type alias to `/usr/include/bits/types.h`:
```c
typedef unsigned long int __uint64_t;
```
The problem is that on Windows `unsigned long int` is 4 bytes while on Linux it's often 8 bytes. By using the system headers from Linux for cross-compilation a Windows target platform _**without specifing Windows system headers**_, the wrong type size is reported for `uint64_t` as 4 bytes. This is just a simple example, but other things can generally go wrong by not specifying the correct system headers for the target platform such as Clang complaining about missing headers.

#### Restrictive or closed-source system headers

##### Problem

While Clang is free and open-source, an annoying problem is that some target platforms' system headers are not. Even worse is that some target platforms' system headers have further restrictive terms of the agreement prohibiting redistribution for cross-compilation. What makes this situation a tricky legal matter is that C `.h` header files _**could contain source code**_. In order to stay clear of any doubt, for some target platforms, reading the C code has to be done from specific operating systems (e.g. Windows) or specific hardware (e.g. Apple) where the system headers are available on disk in a way that does not violate the terms of agreement or the license.

##### Solution

`C2CS` is structured into two programs to overcome this problem.

1. `ast`: Reading C code and extracting information of the target platform for generating C# code later in form a C abstract syntax tree `.json` file. The extracted information _**does not contain any source code**_; it is purely meta data about the bitwidth and names of functions, structs, enums, typedefs, etc.
2. `cs`: Reading one or more previously generated C abstract syntax tree `.json` files to generate C# code for the specified target platforms.
   
This allows the `ast` program of `C2CS` to run and extract the information for the target platforms with restrictive system headers. The resulting `.json` files of the C abstract syntax trees can be moved to any operating system to generate the C# code for multiple target platforms at once using the `cs` program of `C2CS`.

See [README.md#cross-parsing-with-c2cs](./README.md#cross-parsing-with-c2cs) for a walkthrough.

## Calling C platform code from your .NET application

If you are not familiar already with interoperability of C/C++ with C#, it's assumed that you have read and understood the following relatively short readings:
- [P/Invoke: Introduction](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke).
- [Marshalling: Introduction](https://docs.microsoft.com/en-us/dotnet/standard/native-interop/type-marshaling).
- [Marshalling: Default behaviour for value types in .NET](https://docs.microsoft.com/en-us/dotnet/framework/interop/default-marshaling-behavior#default-marshaling-for-value-types).

### Dynamic loading & dynamic linking

At runtime, if the C library is "dynamic" (meaning the `.dll`/`.dylib`/`.so` file is external to the executable), it gets loaded into virtual memory by operating system at runtime at first usage of an external function (this is called dynamic linking) or by user code via `dlopen` on Unix or `LoadLibrary` on Windows (this is called dynamic loading).

#### Dynamic loading

In the case of **dynamic loading**, said function has to additionally be resolved by *symbol* from user code using `dlsym` (Unix) or `GetProcAddress` (Windows) to get the pointer to the function in virtual memory (function pointer). Then to call said function requires one level of *indirection* stemming from the use of the pointer. This is suboptimal compared to dynamic linking but is the current affairs of how the `DllImport` works in just-in-time compilation environments of .NET for platform invoke (P/Invoke).

#### Dynamic linking

With the advancement of ahead-of-time compilation in Mono and .NET 5 ([NativeAOT](https://github.com/dotnet/runtimelab/tree/feature/NativeAOT)), **dynamic linking** can be achieved instead of **dynamic loading** resulting in *direct* platform invoke (P/Invoke) which has better performance by lack of the *indirection* use of the extra pointer. The feature is called ["DirectPInvoke"](https://github.com/dotnet/runtimelab/blob/feature/NativeAOT/docs/using-nativeaot/interop.md).

### Static linking

Statically linking a C library (`.lib`/`.a`) is something that is necessary for some platforms such as iOS and Xbox. This requires ahead-of-time (AOT) compilation as mentioned in dynamic linking.

The main reason for statically linking a C library is "security" but it's really about total control of what gets executed on the devices. Since a dynamic library can be loaded at startup or at runtime by user code, you could hypothetically download additional executable code from somewhere and have it load it at next startup or directly at runtime (think plugins or video game mods). For certain environments which are ["walled gardens"](https://en.wikipedia.org/wiki/Closed_platform) such as iOS or Nintendo Switch, dynamic loading and/or dynamic linking is considered a Pandora's box that must be strictly controlled to which static linking is sometimes the preferred or only alternative method for enforcing centralization. E.g. a "jailbroke" iOS device or a "homebrew" Nintendo Switch is how the [community of "hackers"](https://en.wikipedia.org/wiki/Hacker_culture) such as enthusiasts, amateur developers, or modders hack their devices for enjoyment, curisoity, or extending a product or software on said device. It's how [individuals added emojis to iOS before Apple did to which Apple adopted the changes](https://en.wikipedia.org/wiki/IOS_jailbreaking#Device_customization) (a common theme/relationship for even video game studios and modders). It's also how the [police, criminals, and governments do more nefarious activities such as surveillance, warfare, and/or politics](https://en.wikipedia.org/wiki/Pegasus_(spyware)). Dynamic linking and dynamic loading is ubiquitous beyond such walled gardens or for devices which escaped the walled gardens.

## Platforms

### Platform Matrix Notes

|Column|Notes|
|:-:|-|
|Open|The ability to read C code for the target platform from a different host platform. If a platform has an `🔓` here it means the system headers can be distributed to other platforms under a free and open-source license. If a platform has an `🔒` here it means the system header files part of the target platform have to be made manually accessible to `C2CS` to read C code for the target platform from a different host platform.|
|OS|The operating system of the target platform.|
|Arch|The computer architecture (a.k.a instruction set architecture) of the target platform|
|SDE|The software development environment (SDE) required to build native libraries for the target platform.|
|.NET RID|The .NET runtime identifier (RID) for the target platform. If a RID exists here, it is officially supported as a target platform by the .NET ecosystem.|

For simplicity, `ARM32` and some other computer architectures are not listed here because it is few and far between for the .NET ecosystem. Similarly, `X86` is mostly legacy and often only available on desktop platforms.

### Platform Matrix

|Open|OS|Arch|SDE|Clang Target|.NET RID|
|:-:|:-:|:-:|:-:|:-:|:-:|
|🔓|Windows|`ARM64`|[MinGW](https://en.wikipedia.org/wiki/MinGW)|`aarch64-pc-windows-gnu`|`win-arm64`
|🔓|Windows|`X64`|[MinGW](https://en.wikipedia.org/wiki/MinGW)|`x86_64-pc-windows-gnu`|`win-x64`
|🔓|Windows|`X86`|[MinGW](https://en.wikipedia.org/wiki/MinGW)|`i686-pc-windows-gnu`|`win-x86`
|🔒<sup>1</sup>|Windows|`ARM64`|[MSVC](https://en.wikipedia.org/wiki/Microsoft_Visual_C%2B%2B)|`aarch64-pc-windows-msvc`|`win-arm64`
|🔒<sup>1</sup>|Windows|`X64`|[MSVC](https://en.wikipedia.org/wiki/Microsoft_Visual_C%2B%2B)|`x86_64-pc-windows-msvc`|`win-x64`
|🔒<sup>1</sup>|Windows|`X86`|[MSVC](https://en.wikipedia.org/wiki/Microsoft_Visual_C%2B%2B)|`i686-pc-windows-msvc`|`win-x86`
|🔒<sup>2</sup>|macOS|`ARM64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`aarch64-apple-darwin`|`osx-arm64`
|🔒<sup>2</sup>|macOS|`X64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`x86_64-apple-darwin`|`osx-x64`
|🔒<sup>2</sup>|macOS|`X86`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`i686-apple-darwin`|`osx-x86`
|🔓|Linux (kernel)|`ARM64`|[CMake](https://en.wikipedia.org/wiki/CMake) recommended|`aarch64-unknown-linux-gnu`|`linux-arm64`
|🔓|Linux (kernel)|`X64`|[CMake](https://en.wikipedia.org/wiki/CMake) recommended|`x86_64-unknown-linux-gnu`|`linux-x64`
|🔓|Linux (kernel)|`X86`|[CMake](https://en.wikipedia.org/wiki/CMake) recommended|`i686-unknown-linux-gnu`|`linux-x86`
|🔒<sup>2</sup>|iOS|`ARM64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`aarch64-apple-ios`|`ios-arm64`
|🔒<sup>2</sup>|iOS|`X64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`x86_64-apple-ios`|`ios-x64`
|🔒<sup>2</sup>|tvOS|`ARM64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`aarch64-apple-tvos`|`tvos-arm64`
|🔒<sup>2</sup>|tvOS|`X64`|[XCode](https://en.wikipedia.org/wiki/Xcode)|`x86_64-apple-tvos`|`tvos-x64`
|🔒<sup>3</sup>|Android|`ARM64`|[Android Studio](https://en.wikipedia.org/wiki/Android_Studio)|`aarch64-linux-android`|`android-arm64`
|🔒<sup>3</sup>|Android|`X64`|[Android Studio](https://en.wikipedia.org/wiki/Android_Studio)|`x86_64-linux-android`|`android-x64`

<sup>1</sup>: Microsoft does not allow distribution of their software development kits (SDKs) for Windows due to their [Microsoft Software License Terms](https://docs.microsoft.com/en-us/legal/windows-sdk/redist). You can [download and install the SDKs here](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/). You will find the important directories on your machine at `%ProgramFiles(x86)%\Windows Kits\10\Include`.

<sup>2</sup>: Apple does not allow copy or usage of their software development kits (SDKs) on non-Apple branded hardware due to their [service level agreement](https://www.apple.com/legal/sla/docs/xcode.pdf). You can download and install XCode through the App Store to gain access to the SDKs for macOS, iOS, tvOS, watchOS, or any other Apple target platform.

<sup>3</sup>: Google does not allow copy or usage of their software development kits (SDKs) due to their [Android Software Development Kit License Agreement](https://developer.android.com/studio/terms). You can download and install Android Studio to gain access to the SDKs for Android.
