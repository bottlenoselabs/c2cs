// Provides macros, types, and functions that make P/Invoke with C# easier.
#pragma once
#include <string.h>

#if defined(__APPLE__) && __has_include("TargetConditionals.h")
    #include <TargetConditionals.h>

    #define FFI_TARGET_CPU_X64      TARGET_CPU_X86_64
    #define FFI_TARGET_CPU_X86      TARGET_CPU_X86
    #define FFI_TARGET_CPU_ARM64    TARGET_CPU_ARM64

    #define FFI_TARGET_OS_WINDOWS   0
    #define FFI_TARGET_OS_LINUX     0
    #define FFI_TARGET_OS_MACOS     TARGET_OS_OSX
    #define FFI_TARGET_OS_IOS       TARGET_OS_IOS

    #define FFI_TARGET_ENV_MSVC     0
    #define FFI_TARGET_ENV_GNU      0
#else
    #define FFI_TARGET_CPU_X64      defined(__x86_64__) || defined(_M_AMD64) || defined(_M_X64)
    #define FFI_TARGET_CPU_X86      defined(i386) || defined(__i386__) || defined(__i386) || defined(_M_IX86)
    #define FFI_TARGET_CPU_ARM64    defined(__aarch64__) || defined(_M_ARM64)

    #define FFI_TARGET_OS_WINDOWS   defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
    #define FFI_TARGET_OS_LINUX     defined(__linux__)
    #define FFI_TARGET_OS_MACOS     0
    #define FFI_TARGET_OS_IOS       0

    #define FFI_TARGET_ENV_MSVC     defined(_MSC_VER)
    #define FFI_TARGET_ENV_GNU      defined(__GNUC__)
#endif

#if FFI_TARGET_OS_WINDOWS && FFI_TARGET_ENV_GNU
    #if FFI_TARGET_CPU_X64
        #define FFI_PLATFORM_NAME "x86_64-pc-windows-gnu"
    #elif FFI_TARGET_CPU_X86
        #define FFI_PLATFORM_NAME "i686-pc-windows-gnu"
    #elif FFI_TARGET_CPU_ARM64
        #define FFI_PLATFORM_NAME "aarch64-pc-windows-gnu"
    #else
        #error "Unknown computer architecture for Windows (GNU)."
    #endif
#elif FFI_TARGET_OS_WINDOWS && FFI_TARGET_ENV_MSVC
    #if FFI_TARGET_CPU_X64
        #define FFI_PLATFORM_NAME "x86_64-pc-windows-msvc"
    #elif FFI_TARGET_CPU_X86
        #define FFI_PLATFORM_NAME "i686-pc-windows-msvc"
    #elif FFI_TARGET_CPU_ARM64
        #define FFI_PLATFORM_NAME "aarch64-pc-windows-msvc"
    #else
        #error "Unknown computer architecture for Windows (Microsoft Visual C++)."
    #endif
#elif FFI_TARGET_OS_LINUX
    #if FFI_TARGET_CPU_X64
        #define FFI_PLATFORM_NAME "x86_64-unknown-linux-gnu"
    #elif FFI_TARGET_CPU_X86
        #define FFI_PLATFORM_NAME "i686-unknown-linux-gnu"
    #elif FFI_TARGET_CPU_ARM64
        #define FFI_PLATFORM_NAME "aarch64-unknown-linux-gnu"
    #else
        #error "Unknown computer architecture for Linux."
    #endif
#elif FFI_TARGET_OS_MACOS
    #if FFI_TARGET_CPU_X64
        #define FFI_PLATFORM_NAME "x86_64-apple-darwin"
    #elif FFI_TARGET_CPU_X86
        #define FFI_PLATFORM_NAME "i686-apple-darwin"
    #elif FFI_TARGET_CPU_ARM64
        #define FFI_PLATFORM_NAME "aarch64-apple-darwin"
    #else
        #error "Unknown computer architecture for macOS."
    #endif
#elif FFI_TARGET_OS_IOS
    #if FFI_TARGET_CPU_X64
        #define FFI_PLATFORM_NAME "x86_64-apple-ios"
    #elif FFI_TARGET_CPU_X86
        #define FFI_PLATFORM_NAME "i686-apple-ios"
    #elif FFI_TARGET_CPU_ARM64
        #define FFI_PLATFORM_NAME "aarch64-apple-ios"
    #else
        #error "Unknown computer architecture for iOS."
    #endif
#else
    #define FFI_PLATFORM_NAME 0
#endif

#if FFI_TARGET_OS_WINDOWS
    #define FFI_API_DECL __declspec(dllexport)
#else
    #define FFI_API_DECL extern
#endif