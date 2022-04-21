// Provides macros, types, and functions that make P/Invoke with C# easier.

#if defined(__APPLE__) && __has_include("TargetConditionals.h")
    #include <TargetConditionals.h>

    #define PINVOKE_TARGET_CPU_X64      TARGET_CPU_X86_64
    #define PINVOKE_TARGET_CPU_X86      TARGET_CPU_X86
    #define PINVOKE_TARGET_CPU_ARM64    TARGET_CPU_ARM64

    #define PINVOKE_TARGET_OS_WINDOWS   0
    #define PINVOKE_TARGET_OS_LINUX     0
    #define PINVOKE_TARGET_OS_MACOS     TARGET_OS_OSX
    #define PINVOKE_TARGET_OS_IOS       TARGET_OS_IOS

    #define PINVOKE_TARGET_ENV_MSVC     0
    #define PINVOKE_TARGET_ENV_GNU      0
#else
    #define PINVOKE_TARGET_CPU_X64      defined(__x86_64__) || defined(_M_AMD64) || defined(_M_X64)
    #define PINVOKE_TARGET_CPU_X86      defined(i386) || defined(__i386__) || defined(__i386) || defined(_M_IX86)
    #define PINVOKE_TARGET_CPU_ARM64    defined(__aarch64__) || defined(_M_ARM64)

    #define PINVOKE_TARGET_OS_WINDOWS   defined(WIN32) || defined(_WIN32) || defined(__WIN32__)
    #define PINVOKE_TARGET_OS_LINUX     defined(__linux__)
    #define PINVOKE_TARGET_OS_MACOS     0
    #define PINVOKE_TARGET_OS_IOS       0

    #define PINVOKE_TARGET_ENV_MSVC     defined(_MSC_VER)
    #define PINVOKE_TARGET_ENV_GNU      defined(__GNUC__)
#endif

#if PINVOKE_TARGET_OS_WINDOWS && PINVOKE_TARGET_ENV_GNU
    #if PINVOKE_TARGET_CPU_X64
        #define PINVOKE_TARGET_NAME "x86_64-pc-windows-gnu"
    #elif PINVOKE_TARGET_CPU_X86
        #define PINVOKE_TARGET_NAME "i686-pc-windows-gnu"
    #elif PINVOKE_TARGET_CPU_ARM64
        #define PINVOKE_TARGET_NAME "aarch64-pc-windows-gnu"
    #else
        #error "Unknown computer architecture for Windows (GNU)."
    #endif
#elif PINVOKE_TARGET_OS_WINDOWS && PINVOKE_TARGET_ENV_MSVC
    #if PINVOKE_TARGET_CPU_X64
        #define PINVOKE_TARGET_NAME "x86_64-pc-windows-msvc"
    #elif PINVOKE_TARGET_CPU_X86
        #define PINVOKE_TARGET_NAME "i686-pc-windows-msvc"
    #elif PINVOKE_TARGET_CPU_ARM64
        #define PINVOKE_TARGET_NAME "aarch64-pc-windows-msvc"
    #else
        #error "Unknown computer architecture for Windows (Microsoft Visual C++)."
    #endif
#elif PINVOKE_TARGET_OS_LINUX
    #if PINVOKE_TARGET_CPU_X64
        #define PINVOKE_TARGET_NAME "x86_64-unknown-linux-gnu"
    #elif PINVOKE_TARGET_CPU_X86
        #define PINVOKE_TARGET_NAME "i686-unknown-linux-gnu"
    #elif PINVOKE_TARGET_CPU_ARM64
        #define PINVOKE_TARGET_NAME "aarch64-unknown-linux-gnu"
    #else
        #error "Unknown computer architecture for Linux."
    #endif
#elif PINVOKE_TARGET_OS_MACOS
    #if PINVOKE_TARGET_CPU_X64
        #define PINVOKE_TARGET_NAME "x86_64-apple-darwin"
    #elif PINVOKE_TARGET_CPU_X86
        #define PINVOKE_TARGET_NAME "i686-apple-darwin"
    #elif PINVOKE_TARGET_CPU_ARM64
        #define PINVOKE_TARGET_NAME "aarch64-apple-darwin"
    #else
        #error "Unknown computer architecture for macOS."
    #endif
#elif PINVOKE_TARGET_OS_IOS
    #if PINVOKE_TARGET_CPU_X64
        #define PINVOKE_TARGET_NAME "x86_64-apple-ios"
    #elif PINVOKE_TARGET_CPU_X86
        #define PINVOKE_TARGET_NAME "i686-apple-ios"
    #elif PINVOKE_TARGET_CPU_ARM64
        #define PINVOKE_TARGET_NAME "aarch64-apple-ios"
    #else
        #error "Unknown computer architecture for iOS."
    #endif
#else
    #define PINVOKE_TARGET_NAME 0
#endif

#if PINVOKE_TARGET_OS_WINDOWS
    #define PINVOKE_API_DECL __declspec(dllexport)
#else
    #define PINVOKE_API_DECL extern
#endif

PINVOKE_API_DECL const char* pinvoke_get_platform_name()
{
    return PINVOKE_TARGET_NAME;
}