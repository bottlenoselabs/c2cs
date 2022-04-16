# Platform Headers

System headers (a.k.a platform headers) are vendored so that `C2CS` has access to read C code for *almost* any target from a host using [Clang cross-compilation](https://clang.llvm.org/docs/CrossCompilation.html). These files are *not* used as source code files directly to generate any binary, executable, or library. Rather, these header files are only used to extract the name and bitwidth of functions, enums, structs, typedefs, pointers, function pointers, macros, etc, for the purposes of generating bindings to C#. When building a C/C++/ObjC library, do _**not**_ use these headers; instead _**do use**_ the official headers as part of your local installed toolchain. You can of course tell `C2CS` use to the official headers from your local installed toolchain instead of these system headers if you so wish aswell.

Each directory here contains the system root (sysroot) for a [Clang target triple](https://clang.llvm.org/docs/CrossCompilation.html) minus the executables, libraries, and binaries. Each directory has it's own license. `C2CS` considers each Clang target triple a "platform" because it identifies the CPU architecture and the operating system. For .NET, the [.NET Runtime Identifier (RID)](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog) also identifies the CPU architecture and the operating system. The Clang target triple however can carry more information such as the environment. But the 

**Not all platforms can be made available here due to licensing.** Some platforms prohibit distribution of their system headers. For such target platforms you will have to tell `C2CS` use your local installed toolchain to "cross-parse". 

|Platform Description|Clang Target Triple|Open Source|.NET RID|
|-|-|-|-|
|X64 Windows using Minimalist GNU for Windows (MinGW).|[x86_64-pc-windows-gnu](./x86_64-pc-windows-gnu/)|[Link](https://packages.msys2.org/package/mingw-w64-clang-x86_64-headers)|`win-x64`
|X86 Windows using Minimalist GNU for Windows (MinGW).|[i686-pc-windows-gnu](./i686-pc-windows-gnu/)|[Link](https://packages.msys2.org/package/mingw-w64-clang-i686-headers)|`win-x86`
|ARM64 Windows using Minimalist GNU for Windows (MinGW).|[aarch64-pc-windows-gnu](./i686-pc-windows-gnu/)|[Link](https://packages.msys2.org/package/mingw-w64-clang-aarch64-headers-git)|`win-arm64`
|X64 Windows using Microsoft Visual C++ (MSVC)|x86_64-pc-windows-msvc|No<sup>1</sup>||`win-x64`
|X86 Windows using Microsoft Visual C++ (MSVC)|i686-pc-windows-msvc|No<sup>1</sup>||`win-x86`

<sup>1</sup>: Windows does not allow distribution of their software development kits (SDKs), and hence their system headers, due to their [licensing](https://docs.microsoft.com/en-us/legal/windows-sdk/redist). You can [download and install the SDKs here](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/). You will find the important directories on your machine at `%ProgramFiles(x86)%\Windows Kits\10\Include`. 